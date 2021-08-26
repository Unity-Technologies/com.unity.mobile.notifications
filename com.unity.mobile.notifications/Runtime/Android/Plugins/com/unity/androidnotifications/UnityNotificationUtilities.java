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
import android.os.Build;
import android.os.Bundle;
import android.os.Parcel;
import android.os.Parcelable;
import android.util.Base64;
import android.util.Log;

public class UnityNotificationUtilities {
    // magic stands for "Unity Mobile Notifications Notification"
    private static final byte[] UNITY_MAGIC_NUMBER = new byte[] { 'U', 'M', 'N', 'N'};
    private static final byte[] UNITY_MAGIC_NUMBER_PARCELLED = new byte[] { 'U', 'M', 'N', 'P'};
    private static final int NOTIFICATION_SERIALIZATION_VERSION = 0;
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

    /* Originally we used to serialize a bundle with predefined list of values.
       After we exposed entire Notification.Builder to users, this is not sufficient anymore.
       Unfortunately, while Notification itself is Parcelable and can be marshalled to bytes,
       it's contents are not guaranteed to be (Binder objects).
       Hence what we try to do here is:
       - serialize as is
       - fallback 1: serialize our known properties + serialize extras as is
       - fallback 2: serialize our known stuff
    */
    protected static String serializeNotificationIntent(Intent intent) {
        try {
            Notification notification = intent.getParcelableExtra("unityNotification");
            if (notification == null)
                return null;
            ByteArrayOutputStream data = new ByteArrayOutputStream();
            DataOutputStream out = new DataOutputStream(data);
            boolean success = serializeNotificationParcel(intent, out);
            if (!success) {
                data.reset();
                success = serializeNotificationCustom(notification, out);
            }
            if (success) {
                out.close();
                byte[] bytes = data.toByteArray();
                return Base64.encodeToString(bytes, 0, bytes.length, 0);
            }
        } catch (Exception e) {
            Log.e("Unity", "Failed to serialize notification", e);
        }

        return null;
    }

    private static boolean serializeNotificationParcel(Intent intent, DataOutputStream out) {
        try {
            byte[] bytes = serializeParcelable(intent);
            if (bytes == null || bytes.length == 0)
                return false;
            out.write(UNITY_MAGIC_NUMBER_PARCELLED);
            out.writeInt(INTENT_SERIALIZATION_VERSION);
            out.writeInt(bytes.length);
            out.write(bytes);
            return true;
        } catch (Exception e) {
            Log.e("Unity", "Failed to serialize notification as Parcel", e);
        }

        return false;
    }

    private static boolean serializeNotificationCustom(Notification notification, DataOutputStream out) {
        try {
            out.write(UNITY_MAGIC_NUMBER);
            out.writeInt(NOTIFICATION_SERIALIZATION_VERSION);

            // serialize extras
            boolean showWhen = notification.extras.getBoolean(Notification.EXTRA_SHOW_WHEN, false);
            byte[] extras = serializeParcelable(notification.extras);
            out.writeInt(extras == null ? 0 : extras.length);
            if (extras != null && extras.length > 0)
                out.write(extras);
            else {
                // parcelling may fail in case it contains binder object, when that happens serialize manually what we care about
                out.writeInt(notification.extras.getInt("id"));
                serializeString(out, notification.extras.getString(Notification.EXTRA_TITLE));
                serializeString(out, notification.extras.getString(Notification.EXTRA_TEXT));
                serializeString(out, notification.extras.getString("smallIcon"));
                serializeString(out, notification.extras.getString("largeIcon"));
                out.writeLong(notification.extras.getLong("fireTime", -1));
                out.writeLong(notification.extras.getLong("repeatInterval", -1));
                serializeString(out,  Build.VERSION.SDK_INT < Build.VERSION_CODES.LOLLIPOP ? null : notification.extras.getString(Notification.EXTRA_BIG_TEXT));
                out.writeBoolean(notification.extras.getBoolean(Notification.EXTRA_SHOW_CHRONOMETER, false));
                out.writeBoolean(showWhen);
                serializeString(out, notification.extras.getString("data"));
            }

            serializeString(out, Build.VERSION.SDK_INT < Build.VERSION_CODES.O ? null : notification.getChannelId());
            Integer color = UnityNotificationManager.getNotificationColor(notification);
            out.writeBoolean(color != null);
            if (color != null)
                out.writeInt(color);
            out.writeInt(notification.number);
            out.writeBoolean(0 != (notification.flags & Notification.FLAG_AUTO_CANCEL));
            serializeString(out, notification.getGroup());
            out.writeBoolean(0 != (notification.flags & Notification.FLAG_GROUP_SUMMARY));
            out.writeInt(UnityNotificationManager.getNotificationGroupAlertBehavior(notification));
            serializeString(out, notification.getSortKey());
            if (showWhen)
                out.writeLong(notification.when);

            return true;
        } catch (Exception e) {
            Log.e("Unity", "Failed to serialize notification", e);
            return false;
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
            byte[] result = p.marshall();
            p.recycle();
            return result;
        } catch (Exception e) {
            Log.e("Unity", "Failed to serialize Parcelable", e);
            return null;
        }
    }

    protected static Intent deserializeNotificationIntent(Context context, String src) {
        byte[] bytes = Base64.decode(src, 0);
        return deserializeNotificationIntent(context, bytes);
    }

    /* See serialization method above for explaination of fallbacks.
       This one matches it with one additional fallback: support for "old" bundle serialization.
    */
    private static Intent deserializeNotificationIntent(Context context, byte[] bytes) {
        ByteArrayInputStream data = new ByteArrayInputStream(bytes);
        DataInputStream in = new DataInputStream(data);
        Intent intent = deserializeNotificationIntent(in);
        if (intent != null)
            return intent;
        data.reset();
        Notification notification = deserializeNotificationCustom(in);
        if (notification == null) {
            notification = deserializedFromOldIntent(bytes);
        }
        if (notification == null)
            return null;
        intent = new Intent(context, UnityNotificationManager.class);
        intent.putExtra("unityNotification", notification);
        return intent;
    }

    private static boolean readAndCheckMagicNumber(DataInputStream in, byte[] magic) {
        try {
            boolean magicNumberMatch = true;
            for (int i = 0; i < magic.length; ++i) {
                byte b = in.readByte();
                if (b != magic[i]) {
                    magicNumberMatch = false;
                    break;
                }
            }

            return magicNumberMatch;
        } catch (Exception e) {
            return false;
        }
    }

    private static Intent deserializeNotificationIntent(DataInputStream in) {
        try {
            if (!readAndCheckMagicNumber(in, UNITY_MAGIC_NUMBER_PARCELLED))
                return null;
            int version = in.readInt();
            if (version <0 || version > INTENT_SERIALIZATION_VERSION)
                return null;
            return deserializeParcelable(in);
        } catch (Exception e) {
            Log.e("Unity", "Failed to deserialize notification intent", e);
            return null;
        }
    }

    private static Notification deserializeNotificationCustom(DataInputStream in) {
        try {
            if (!readAndCheckMagicNumber(in, UNITY_MAGIC_NUMBER))
                return null;
            int version = in.readInt();
            if (version < 0 || version > NOTIFICATION_SERIALIZATION_VERSION)
                return null;

            // deserialize extras
            int id = 0;
            String title, text, smallIcon, largeIcon, bigText, intentData;
            long fireTime, repeatInterval;
            boolean usesStopWatch, showWhen;
            Bundle extras = null;
            try {
                extras = deserializeParcelable(in);
            } catch (ClassCastException cce) {
                Log.e("Unity", "Unexpect type of deserialized object", cce);
            }

            if (extras == null) {
                // extras serialized manually
                id = in.readInt();
                title = deserializeString(in);
                text = deserializeString(in);
                smallIcon = deserializeString(in);
                largeIcon = deserializeString(in);
                fireTime = in.readLong();
                repeatInterval = in.readLong();
                bigText = deserializeString(in);
                usesStopWatch = in.readBoolean();
                showWhen = in.readBoolean();
                intentData = deserializeString(in);
            } else {
                title = extras.getString(Notification.EXTRA_TITLE);
                text = extras.getString(Notification.EXTRA_TEXT);
                smallIcon = extras.getString("smallIcon");
                largeIcon = extras.getString("largeIcon");
                fireTime = extras.getLong("fireTime", -1);
                repeatInterval = extras.getLong("repeatInterval", -1);
                bigText = Build.VERSION.SDK_INT < Build.VERSION_CODES.LOLLIPOP ? null : extras.getString(Notification.EXTRA_BIG_TEXT);
                usesStopWatch = extras.getBoolean(Notification.EXTRA_SHOW_CHRONOMETER, false);
                showWhen = extras.getBoolean(Notification.EXTRA_SHOW_WHEN, false);
                intentData = extras.getString("data");
            }

            String channelId = deserializeString(in);
            boolean haveColor = in.readBoolean();
            int color = 0;
            if (haveColor)
                color = in.readInt();
            int number = in.readInt();
            boolean shouldAutoCancel = in.readBoolean();
            String group = deserializeString(in);
            boolean groupSummary = in.readBoolean();
            int groupAlertBehavior = in.readInt();
            String sortKey = deserializeString(in);
            long when = showWhen ? in.readLong() : 0;

            Notification.Builder builder = UnityNotificationManager.mUnityNotificationManager.createNotificationBuilder(channelId);
            if (extras != null)
                builder.setExtras(extras);
            else {
                builder.getExtras().putInt("id", id);
                UnityNotificationManager.setNotificationIcon(builder, "smallIcon", smallIcon);
                UnityNotificationManager.setNotificationIcon(builder, "largeIcon", largeIcon);
                if (fireTime != -1)
                    builder.getExtras().putLong("fireTime", fireTime);
                if (repeatInterval != -1)
                    builder.getExtras().putLong("repeatInterval", repeatInterval);
                if (intentData != null)
                    builder.getExtras().putString("data", intentData);
            }
            if (title != null)
                builder.setContentTitle(title);
            if (text != null)
                builder.setContentText(text);
            if (bigText != null)
                builder.setStyle(new Notification.BigTextStyle().bigText(bigText));
            if (haveColor)
                UnityNotificationManager.setNotificationColor(builder, color);
            if (number >= 0)
                builder.setNumber(number);
            builder.setAutoCancel(shouldAutoCancel);
            UnityNotificationManager.setNotificationUsesChronometer(builder, usesStopWatch);
            if (group != null && group.length() > 0)
                builder.setGroup(group);
            builder.setGroupSummary(groupSummary);
            UnityNotificationManager.setNotificationGroupAlertBehavior(builder, groupAlertBehavior);
            if (sortKey != null && sortKey.length() > 0)
                builder.setSortKey(sortKey);
            if (showWhen) {
                builder.setShowWhen(true);
                builder.setWhen(when);
            }

            return builder.build();
        } catch (Exception e) {
            Log.e("Unity", "Failed to deserialize notification", e);
            return null;
        }
    }

    private static Notification deserializedFromOldIntent(byte[] bytes) {
        try {
            Parcel p = Parcel.obtain();
            p.unmarshall(bytes, 0, bytes.length);
            p.setDataPosition(0);
            Bundle bundle = new Bundle();
            bundle.readFromParcel(p);

            int id = bundle.getInt("id", -1);
            String channelId = bundle.getString("channelID");
            String textTitle = bundle.getString("textTitle");
            String textContent = bundle.getString("textContent");
            String smallIcon = bundle.getString("smallIconStr");
            boolean autoCancel = bundle.getBoolean("autoCancel", false);
            boolean usesChronometer = bundle.getBoolean("usesChronometer", false);
            long fireTime = bundle.getLong("fireTime", -1);
            long repeatInterval = bundle.getLong("repeatInterval", -1);
            String largeIcon = bundle.getString("largeIconStr");
            int style = bundle.getInt("style", -1);
            int color = bundle.getInt("color", 0);
            int number = bundle.getInt("number", 0);
            String intentData = bundle.getString("data");
            String group = bundle.getString("group");
            boolean groupSummary = bundle.getBoolean("groupSummary", false);
            String sortKey = bundle.getString("sortKey");
            int groupAlertBehaviour = bundle.getInt("groupAlertBehaviour", -1);
            boolean showTimestamp = bundle.getBoolean("showTimestamp", false);

            Notification.Builder builder = UnityNotificationManager.mUnityNotificationManager.createNotificationBuilder(channelId);
            builder.getExtras().putInt("id", id);
            builder.setContentTitle(textTitle);
            builder.setContentText(textContent);
            UnityNotificationManager.setNotificationIcon(builder, "smallIcon", smallIcon);
            builder.setAutoCancel(autoCancel);
            builder.setUsesChronometer(usesChronometer);
            builder.getExtras().putLong("fireTime", fireTime);
            builder.getExtras().putLong("repeatInterval", repeatInterval);
            UnityNotificationManager.setNotificationIcon(builder, "largeIcon", largeIcon);
            if (style == 2)
                builder.setStyle(new Notification.BigTextStyle().bigText(textContent));
            if (color != 0)
                UnityNotificationManager.setNotificationColor(builder, color);
            if (number >= 0)
                builder.setNumber(number);
            if (intentData != null)
                builder.getExtras().putString("data", intentData);
            if (null != group && group.length() > 0)
                builder.setGroup(group);
            builder.setGroupSummary(groupSummary);
            if (null != sortKey && sortKey.length() > 0)
                builder.setSortKey(sortKey);
            UnityNotificationManager.setNotificationGroupAlertBehavior(builder, groupAlertBehaviour);
            builder.setShowWhen(showTimestamp);
            return builder.build();
        } catch (Exception e) {
            Log.e("Unity", "Failed to deserialize old style notification", e);
            return null;
        }
    }

    private static String deserializeString(DataInputStream in) throws IOException {
        int length = in.readInt();
        if (length <= 0)
            return null;
        byte[] bytes = new byte[length];
        int didRead = in.read(bytes);
        if (didRead != bytes.length)
            throw new IOException("Insufficient amount of bytes read");
        return new String(bytes, StandardCharsets.UTF_8);
    }

    private static <T extends Parcelable> T deserializeParcelable(DataInputStream in) throws IOException {
        int length = in.readInt();
        if (length <= 0)
            return null;
        byte[] bytes = new byte[length];
        int didRead = in.read(bytes);
        if (didRead != bytes.length)
            throw new IOException("Insufficient amount of bytes read");

        try {
            Parcel p = Parcel.obtain();
            p.unmarshall(bytes, 0, bytes.length);
            p.setDataPosition(0);
            Bundle b = p.readParcelable(UnityNotificationUtilities.class.getClassLoader());
            p.recycle();
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
