package com.unity.androidnotifications;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.IOException;
import java.nio.charset.StandardCharsets;

import android.app.Notification;
import android.content.Context;
import android.content.Intent;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.content.res.Resources;
import android.os.Bundle;
import android.os.Parcel;
import android.os.Parcelable;
import android.util.Base64;
import android.util.Log;

public class UnityNotificationUtilities {
    // magic stands for "Unity Mobile Notifications Notification"
    private static final byte[] UNITY_MAGIC_NUMBER = new byte[] { 'U', 'M', 'N', 'N'};
    private static final int INTENT_SERIALIZATION_VERSION = 0;

    protected static int findResourceIdInContextByName(Context context, String name) {
        if (name == null)
            return 0;

        try {
            Resources res = context.getResources();
            if (res != null) {
                int id = res.getIdentifier(name, "mipmap", context.getPackageName());//, activity.getPackageName());
                if (id == 0)
                    return res.getIdentifier(name, "drawable", context.getPackageName());//, activity.getPackageName());
                else
                    return id;
            }
            return 0;
        } catch (Resources.NotFoundException e) {
            return 0;
        }
    }

    protected static String serializeNotificationIntent(Intent intent) {
        try {
            Notification notification = intent.getParcelableExtra("unityNotification");
            if (notification == null)
                return null;
            ByteArrayOutputStream data = new ByteArrayOutputStream();
            DataOutputStream out = new DataOutputStream(data);
            out.write(UNITY_MAGIC_NUMBER);
            out.writeInt(INTENT_SERIALIZATION_VERSION);
            out.writeInt(notification.extras.getInt("id"));
            serializeString(out, notification.getChannelId());
            serializeString(out, notification.extras.getString(Notification.EXTRA_TITLE));
            serializeString(out, notification.extras.getString(Notification.EXTRA_TEXT));
            serializeString(out, notification.extras.getString("smallIcon"));
            serializeString(out, notification.extras.getString("largeIcon"));
            out.writeLong(notification.extras.getLong("fireTime", -1));
            out.writeLong(notification.extras.getLong("repeatInterval", -1));
            serializeString(out, notification.extras.getString(Notification.EXTRA_BIG_TEXT));
            Integer color = UnityNotificationManager.getNotificationColor(notification);
            out.writeBoolean(color != null);
            if (color != null)
                out.writeInt(color);
            out.writeInt(notification.number);
            out.writeBoolean(0 != (notification.flags & Notification.FLAG_AUTO_CANCEL));
            out.writeBoolean(notification.extras.getBoolean(Notification.EXTRA_SHOW_CHRONOMETER, false));
            serializeString(out, notification.getGroup()); //TODO Added in API 20
            out.writeBoolean(0 != (notification.flags & Notification.FLAG_GROUP_SUMMARY));  // TODO added in API 20
            out.writeInt(UnityNotificationManager.getNotificationGroupAlertBehavior(notification));
            serializeString(out, notification.getSortKey()); // TODO added in API 20
            boolean showWhen = notification.extras.getBoolean(Notification.EXTRA_SHOW_WHEN, false);
            out.writeBoolean(showWhen);
            if (showWhen)
                out.writeLong(notification.when);
            serializeString(out, notification.extras.getString("data"));
            byte[] extras = serializeParcelable(notification.extras);
            out.writeInt(extras == null ? 0 : extras.length);
            if (extras != null && extras.length > 0)
                out.write(extras);

            out.close();
            byte[] bytes = data.toByteArray();

            return Base64.encodeToString(bytes, 0, bytes.length, 0);
        } catch (Exception e) {
            return null;
        }
    }

    private static void serializeString(DataOutputStream out, String s) throws IOException {
        if (s == null || s.length() == 0)
            out.writeInt(0);
        else {
            byte[] bytes = s.getBytes(StandardCharsets.UTF_8);
            out.writeInt(bytes.length);
            out.write(bytes);
        }
    }

    private static byte[] serializeParcelable(Parcelable obj) {
        try {
            Parcel p = Parcel.obtain();
            Bundle b = new Bundle();
            b.putParcelable("obj", obj);
            p.writeParcelable(b, 0);
            return p.marshall();
        } catch (Exception e) {
            Log.e("Unity", "Failed to serialize Parcelable", e);
            return null;
        }
    }

    protected static Intent deserializeNotificationIntent(Context context, String src) {
        try {
            byte[] newByt = Base64.decode(src, 0);
            ByteArrayInputStream data = new ByteArrayInputStream(newByt);
            DataInputStream in = new DataInputStream(data);
            boolean magicNumberMatch = true;
            for (int i = 0; i < UNITY_MAGIC_NUMBER.length; ++i) {
                byte b = in.readByte();
                if (b != UNITY_MAGIC_NUMBER[i]) {
                    magicNumberMatch = false;
                    break;
                }
            }
            if (!magicNumberMatch)
                return null;
            int version = in.readInt();
            if (version < 0 || version > INTENT_SERIALIZATION_VERSION)
                return null;
            int id = in.readInt();
            String channelId = deserializeString(in);
            String title = deserializeString(in);
            String text = deserializeString(in);
            String smallIcon = deserializeString(in);
            String largeIcon = deserializeString(in);
            long fireTime = in.readLong();
            long repeatInterval = in.readLong();
            String bigText = deserializeString(in);
            boolean haveColor = in.readBoolean();
            int color = 0;
            if (haveColor)
                color = in.readInt();
            int number = in.readInt();
            boolean shouldAutoCancel = in.readBoolean();
            boolean usesStopWatch = in.readBoolean();
            String group = deserializeString(in);
            boolean groupSummary = in.readBoolean();
            int groupAlertBehavior = in.readInt();
            String sortKey = deserializeString(in);
            boolean showWhen = in.readBoolean();
            long when = showWhen ? in.readLong() : 0;
            String intentData = deserializeString(in);
            Bundle extras = null;
            try {
                extras = deserializeParcelable(in);
            } catch (ClassCastException cce) {
                Log.e("Unity", "Unexpect type of deserialized object", cce);
            }

            Notification.Builder builder = UnityNotificationManager.mUnityNotificationManager.createNotificationBuilder(channelId);
            builder.getExtras().putInt("id", id);
            if (title != null)
                builder.setContentTitle(title);
            if (text != null)
                builder.setContentText(text);
            UnityNotificationManager.setNotificationIcon(builder, "smallIcon", smallIcon);
            UnityNotificationManager.setNotificationIcon(builder, "largeIcon", largeIcon);
            if (fireTime != -1)
                builder.getExtras().putLong("fireTime", fireTime);
            if (repeatInterval != -1)
                builder.getExtras().putLong("repeatInterval", repeatInterval);
            if (bigText != null)
                builder.setStyle(new Notification.BigTextStyle().bigText(bigText));
            if (haveColor)
                UnityNotificationManager.setNotificationColor(builder, color);
            if (number >= 0)
                builder.setNumber(number);
            builder.setAutoCancel(shouldAutoCancel);
            UnityNotificationManager.setNotificationUsesChronometer(builder, usesStopWatch);
            if (group != null)
                UnityNotificationManager.setNotificationGroup(builder, group);
            if (groupSummary)
                UnityNotificationManager.setNotificationGroupSummary(builder, groupSummary);
            UnityNotificationManager.setNotificationGroupAlertBehavior(builder, groupAlertBehavior);
            if (sortKey != null && sortKey.length() > 0)
                UnityNotificationManager.setNotificationSortKey(builder, sortKey);
            if (showWhen) {
                builder.setShowWhen(true);
                builder.setWhen(when);
            }
            if (intentData != null)
                builder.getExtras().putString("data", intentData);
            if (extras != null)
                builder.setExtras(extras);

            Notification notification = builder.build();

            Intent intent = new Intent(context, UnityNotificationManager.class);
            intent.putExtra("unityNotification", notification);

            return intent;
        } catch (Exception e) {
            Log.e("Unity", "Failed to deserialize notification", e);
            return null;
        }
    }

    private static String deserializeString(DataInputStream in) throws IOException {
        int length = in.readInt();
        if (length <= 0)
            return null;
        byte[] bytes = new byte[length];
        in.read(bytes);
        return new String(bytes, StandardCharsets.UTF_8);
    }

    private static <T> T deserializeParcelable(DataInputStream in) throws IOException {
        int length = in.readInt();
        if (length <= 0)
            return null;
        byte[] bytes = new byte[length];
        in.read(bytes);

        try {
            Parcel p = Parcel.obtain();
            p.unmarshall(bytes, 0, bytes.length);
            p.setDataPosition(0);
            Bundle b = p.readParcelable(null);
            if (b != null) {
                return b.getParcelable("obj");
            }
        } catch (Exception e) {
            Log.e("Unity", "Failed to deserialize parcelable", e);
        }

        return null;
    }

    protected static Class<?> getOpenAppActivity(Context context, boolean fallbackToDefault) {
        ApplicationInfo ai = null;
        try {
            ai = context.getPackageManager().getApplicationInfo(context.getPackageName(), PackageManager.GET_META_DATA);
        } catch (PackageManager.NameNotFoundException e) {
            e.printStackTrace();
        }
        Bundle bundle = ai.metaData;

        String customActivityClassName = null;
        Class activityClass = null;

        if (bundle.containsKey("custom_notification_android_activity")) {
            customActivityClassName = bundle.getString("custom_notification_android_activity");

            try {
                activityClass = Class.forName(customActivityClassName);
            } catch (ClassNotFoundException ignored) {
                ;
            }
        }

        if (activityClass == null && fallbackToDefault) {
            Log.w("UnityNotifications", "No custom_notification_android_activity found, attempting to find app activity class");

            String classToFind = "com.unity3d.player.UnityPlayerActivity";
            try {
                return Class.forName(classToFind);
            } catch (ClassNotFoundException ignored) {
                Log.w("UnityNotifications", String.format("Attempting to find : %s, failed!", classToFind));
                classToFind = String.format("%s.UnityPlayerActivity", context.getPackageName());
                try {
                    return Class.forName(classToFind);
                } catch (ClassNotFoundException ignored1) {
                    Log.w("UnityNotifications", String.format("Attempting to find class based on package name: %s, failed!", classToFind));
                }
            }
        }

        return activityClass;
    }
}
