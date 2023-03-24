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
import android.content.SharedPreferences;
import android.content.pm.ActivityInfo;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.content.res.Resources;
import android.os.Build;
import android.os.Bundle;
import android.os.Parcel;
import android.os.Parcelable;
import android.util.Base64;
import android.util.Log;

import static com.unity.androidnotifications.UnityNotificationManager.KEY_CHANNEL_ID;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_FIRE_TIME;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_ID;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_INTENT_DATA;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_LARGE_ICON;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_NOTIFICATION;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_REPEAT_INTERVAL;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_SMALL_ICON;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_SHOW_IN_FOREGROUND;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_BIG_LARGE_ICON;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_BIG_PICTURE;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_BIG_CONTENT_TITLE;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_BIG_SUMMARY_TEXT;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_BIG_CONTENT_DESCRIPTION;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_BIG_SHOW_WHEN_COLLAPSED;
import static com.unity.androidnotifications.UnityNotificationManager.TAG_UNITY;

class UnityNotificationUtilities {
    /*
        We serialize notifications and save them to shared prefs, so that if app is killed, we can recreate them.
        The serialized BLOB starts with a four byte magic number descibing serialization type, followed by an integer version.
        IMPORTANT: IF YOU DO A CHANGE THAT AFFECTS THE LAYOUT, BUMP THE VERSION, AND ENSURE OLD VERSION STILL DESERIALIZES. ADD TEST.
        In real life app can get updated having old serialized notifications present, so we should be able to deserialize them.
    */
    // magic stands for "Unity Mobile Notifications Notification"
    static final byte[] UNITY_MAGIC_NUMBER = new byte[] { 'U', 'M', 'N', 'N'};
    private static final byte[] UNITY_MAGIC_NUMBER_PARCELLED = new byte[] { 'U', 'M', 'N', 'P'};
    private static final int NOTIFICATION_SERIALIZATION_VERSION = 3;
    private static final int INTENT_SERIALIZATION_VERSION = 0;

    static final String SAVED_NOTIFICATION_PRIMARY_KEY = "data";
    static final String SAVED_NOTIFICATION_FALLBACK_KEY = "fallback.data";

    protected static int findResourceIdInContextByName(Context context, String name) {
        if (name == null)
            return 0;

        try {
            Resources res = context.getResources();
            if (res != null) {
                int id = res.getIdentifier(name, "mipmap", context.getPackageName());
                if (id == 0)
                    return res.getIdentifier(name, "drawable", context.getPackageName());
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
       - serialize as is if notification is possibly customized by user
       - otherwise serialize our stuff, since there is nothing more
    */
    protected static void serializeNotification(SharedPreferences prefs, Notification notification, boolean serializeParcel) {
        try {
            String serialized;
            ByteArrayOutputStream data = new ByteArrayOutputStream();
            DataOutputStream out = new DataOutputStream(data);
            if (serializeParcel) {
                Intent intent = new Intent();
                intent.putExtra(KEY_NOTIFICATION, notification);
                if (serializeNotificationParcel(intent, out)) {
                    out.close();
                    byte[] bytes = data.toByteArray();
                    serialized = Base64.encodeToString(bytes, 0, bytes.length, 0);
                } else {
                    return; // failed
                }
            }
            else {
                if (serializeNotificationCustom(notification, out)) {
                    out.flush();
                    byte[] bytes = data.toByteArray();
                    serialized = Base64.encodeToString(bytes, 0, bytes.length, 0);
                } else {
                    return; // failed
                }
            }

            SharedPreferences.Editor editor = prefs.edit().clear();
            editor.putString(SAVED_NOTIFICATION_PRIMARY_KEY, serialized);
            editor.apply();
        } catch (Exception e) {
            Log.e(TAG_UNITY, "Failed to serialize notification", e);
        }
    }

    static boolean serializeNotificationParcel(Intent intent, DataOutputStream out) {
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
            Log.e(TAG_UNITY, "Failed to serialize notification as Parcel", e);
        } catch (OutOfMemoryError e) {
            Log.e(TAG_UNITY, "Failed to serialize notification as Parcel", e);
        }

        return false;
    }

    private static boolean serializeNotificationCustom(Notification notification, DataOutputStream out) {
        try {
            out.write(UNITY_MAGIC_NUMBER);
            out.writeInt(NOTIFICATION_SERIALIZATION_VERSION);

            // serialize extras
            boolean showWhen = notification.extras.getBoolean(Notification.EXTRA_SHOW_WHEN, false);

            out.writeInt(notification.extras.getInt(KEY_ID));
            serializeString(out, notification.extras.getString(Notification.EXTRA_TITLE));
            serializeString(out, notification.extras.getString(Notification.EXTRA_TEXT));
            serializeString(out, notification.extras.getString(KEY_SMALL_ICON));
            serializeString(out, notification.extras.getString(KEY_LARGE_ICON));
            out.writeLong(notification.extras.getLong(KEY_FIRE_TIME, -1));
            out.writeLong(notification.extras.getLong(KEY_REPEAT_INTERVAL, -1));
            serializeString(out,  Build.VERSION.SDK_INT < Build.VERSION_CODES.LOLLIPOP ? null : notification.extras.getString(Notification.EXTRA_BIG_TEXT));
            out.writeBoolean(notification.extras.getBoolean(Notification.EXTRA_SHOW_CHRONOMETER, false));
            out.writeBoolean(showWhen);
            serializeString(out, notification.extras.getString(KEY_INTENT_DATA));
            out.writeBoolean(notification.extras.getBoolean(KEY_SHOW_IN_FOREGROUND, true));

            String bigPicture = notification.extras.getString(KEY_BIG_PICTURE);
            serializeString(out, bigPicture);
            if (bigPicture != null && bigPicture.length() > 0) {
                // the following only need to be put in if big picture is there
                serializeString(out, notification.extras.getString(KEY_BIG_LARGE_ICON));
                serializeString(out, notification.extras.getString(KEY_BIG_CONTENT_TITLE));
                serializeString(out, notification.extras.getString(KEY_BIG_CONTENT_DESCRIPTION));
                serializeString(out, notification.extras.getString(KEY_BIG_SUMMARY_TEXT));
                out.writeBoolean(notification.extras.getBoolean(KEY_BIG_SHOW_WHEN_COLLAPSED, false));
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
            Log.e(TAG_UNITY, "Failed to serialize notification", e);
            return false;
        }
    }

    static void serializeString(DataOutputStream out, String s) throws IOException {
        if (s == null || s.length() == 0)
            out.writeInt(0);
        else {
            byte[] bytes = s.getBytes(StandardCharsets.UTF_8);
            out.writeInt(bytes.length);
            out.write(bytes);
        }
    }

    static byte[] serializeParcelable(Parcelable obj) {
        try {
            Parcel p = Parcel.obtain();
            Bundle b = new Bundle();
            b.putParcelable("obj", obj);
            p.writeParcelable(b, 0);
            byte[] result = p.marshall();
            p.recycle();
            return result;
        } catch (Exception e) {
            Log.e(TAG_UNITY, "Failed to serialize Parcelable", e);
        } catch (OutOfMemoryError e) {
            Log.e(TAG_UNITY, "Failed to serialize Parcelable", e);
        }

        return null;
    }

    protected static Object deserializeNotification(Context context, SharedPreferences prefs) {
        String serializedIntentData = prefs.getString(SAVED_NOTIFICATION_PRIMARY_KEY, "");
        if (null == serializedIntentData || serializedIntentData.length() <= 0)
            return null;
        byte[] bytes = Base64.decode(serializedIntentData, 0);
        Object notification = deserializeNotification(context, bytes);
        if (notification != null)
            return notification;
        serializedIntentData = prefs.getString(SAVED_NOTIFICATION_FALLBACK_KEY, "");
        if (null == serializedIntentData || serializedIntentData.length() <= 0)
            return null;
        bytes = Base64.decode(serializedIntentData, 0);
        return deserializeNotification(context, bytes);
    }

    /* See serialization method above for explaination of fallbacks.
       This one matches it with one additional fallback: support for "old" bundle serialization.
    */
    private static Object deserializeNotification(Context context, byte[] bytes) {
        ByteArrayInputStream data = new ByteArrayInputStream(bytes);
        DataInputStream in = new DataInputStream(data);
        Notification notification = deserializeNotificationParcelable(in);
        if (notification != null)
            return notification;
        data.reset();
        Notification.Builder builder = deserializeNotificationCustom(context, in);
        if (builder == null) {
            builder = deserializedFromOldIntent(context, bytes);
        }
        return builder;
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

    private static Notification deserializeNotificationParcelable(DataInputStream in) {
        try {
            if (!readAndCheckMagicNumber(in, UNITY_MAGIC_NUMBER_PARCELLED))
                return null;
            int version = in.readInt();
            if (version < 0 || version > INTENT_SERIALIZATION_VERSION)
                return null;
            Intent intent = deserializeParcelable(in);
            Notification notification = intent.getParcelableExtra(KEY_NOTIFICATION);
            return notification;
        } catch (Exception e) {
            Log.e(TAG_UNITY, "Failed to deserialize notification intent", e);
        } catch (OutOfMemoryError e) {
            Log.e(TAG_UNITY, "Failed to deserialize notification intent", e);
        }

        return null;
    }

    private static Notification.Builder deserializeNotificationCustom(Context context, DataInputStream in) {
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
            boolean usesStopWatch, showWhen, showInForeground = true;
            Bundle extras = null;
            String bigPicture = null, bigLargeIcon = null, bigContentTitle = null, bigSummary = null, bigContentDesc = null;
            boolean bigShowWhenCollapsed = false;
            if (version < 2) {
                // no longer serialized since v2
                extras = deserializeParcelable(in);
            }

            // before v2 it was extras or variables, since 2 always variables
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
                if (version > 0)
                    showInForeground = in.readBoolean();

                if (version >= 3) {
                    bigPicture = deserializeString(in);
                    if (bigPicture != null && bigPicture.length() > 0) {
                        // the following only need to be put in if big picture is there
                        bigLargeIcon = deserializeString(in);
                        bigContentTitle = deserializeString(in);
                        bigContentDesc = deserializeString(in);
                        bigSummary = deserializeString(in);
                        bigShowWhenCollapsed = in.readBoolean();
                    }
                }
            } else {
                title = extras.getString(Notification.EXTRA_TITLE);
                text = extras.getString(Notification.EXTRA_TEXT);
                smallIcon = extras.getString(KEY_SMALL_ICON);
                largeIcon = extras.getString(KEY_LARGE_ICON);
                fireTime = extras.getLong(KEY_FIRE_TIME, -1);
                repeatInterval = extras.getLong(KEY_REPEAT_INTERVAL, -1);
                bigText = Build.VERSION.SDK_INT < Build.VERSION_CODES.LOLLIPOP ? null : extras.getString(Notification.EXTRA_BIG_TEXT);
                usesStopWatch = extras.getBoolean(Notification.EXTRA_SHOW_CHRONOMETER, false);
                showWhen = extras.getBoolean(Notification.EXTRA_SHOW_WHEN, false);
                intentData = extras.getString(KEY_INTENT_DATA);
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

            UnityNotificationManager manager = UnityNotificationManager.getNotificationManagerImpl(context);
            Notification.Builder builder = manager.createNotificationBuilder(channelId);
            if (extras != null)
                builder.setExtras(extras);
            else {
                builder.getExtras().putInt(KEY_ID, id);
                UnityNotificationManager.setNotificationIcon(builder, KEY_SMALL_ICON, smallIcon);
                UnityNotificationManager.setNotificationIcon(builder, KEY_LARGE_ICON, largeIcon);
                if (fireTime != -1)
                    builder.getExtras().putLong(KEY_FIRE_TIME, fireTime);
                if (repeatInterval != -1)
                    builder.getExtras().putLong(KEY_REPEAT_INTERVAL, repeatInterval);
                if (intentData != null)
                    builder.getExtras().putString(KEY_INTENT_DATA, intentData);
                builder.getExtras().putBoolean(KEY_SHOW_IN_FOREGROUND, showInForeground);
            }
            if (title != null)
                builder.setContentTitle(title);
            if (text != null)
                builder.setContentText(text);
            if (bigText != null)
                builder.setStyle(new Notification.BigTextStyle().bigText(bigText));
            else if (bigPicture != null)
                manager.setupBigPictureStyle(builder, bigLargeIcon, bigPicture, bigContentTitle, bigContentDesc, bigSummary, bigShowWhenCollapsed);
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

            return builder;
        } catch (Exception e) {
            Log.e(TAG_UNITY, "Failed to deserialize notification", e);
        } catch (OutOfMemoryError e) {
            Log.e(TAG_UNITY, "Failed to deserialize notification", e);
        }

        return null;
    }

    private static Notification.Builder deserializedFromOldIntent(Context context, byte[] bytes) {
        try {
            Parcel p = Parcel.obtain();
            p.unmarshall(bytes, 0, bytes.length);
            p.setDataPosition(0);
            Bundle bundle = new Bundle();
            bundle.readFromParcel(p);

            int id = bundle.getInt(KEY_ID, -1);
            String channelId = bundle.getString("channelID");
            String textTitle = bundle.getString("textTitle");
            String textContent = bundle.getString("textContent");
            String smallIcon = bundle.getString("smallIconStr");
            boolean autoCancel = bundle.getBoolean("autoCancel", false);
            boolean usesChronometer = bundle.getBoolean("usesChronometer", false);
            long fireTime = bundle.getLong(KEY_FIRE_TIME, -1);
            long repeatInterval = bundle.getLong(KEY_REPEAT_INTERVAL, -1);
            String largeIcon = bundle.getString("largeIconStr");
            int style = bundle.getInt("style", -1);
            int color = bundle.getInt("color", 0);
            int number = bundle.getInt("number", 0);
            String intentData = bundle.getString(KEY_INTENT_DATA);
            String group = bundle.getString("group");
            boolean groupSummary = bundle.getBoolean("groupSummary", false);
            String sortKey = bundle.getString("sortKey");
            int groupAlertBehaviour = bundle.getInt("groupAlertBehaviour", -1);
            boolean showTimestamp = bundle.getBoolean("showTimestamp", false);

            Notification.Builder builder = UnityNotificationManager.getNotificationManagerImpl(context).createNotificationBuilder(channelId);
            builder.getExtras().putInt(KEY_ID, id);
            builder.setContentTitle(textTitle);
            builder.setContentText(textContent);
            UnityNotificationManager.setNotificationIcon(builder, KEY_SMALL_ICON, smallIcon);
            builder.setAutoCancel(autoCancel);
            builder.setUsesChronometer(usesChronometer);
            builder.getExtras().putLong(KEY_FIRE_TIME, fireTime);
            builder.getExtras().putLong(KEY_REPEAT_INTERVAL, repeatInterval);
            UnityNotificationManager.setNotificationIcon(builder, KEY_LARGE_ICON, largeIcon);
            if (style == 2)
                builder.setStyle(new Notification.BigTextStyle().bigText(textContent));
            if (color != 0)
                UnityNotificationManager.setNotificationColor(builder, color);
            if (number >= 0)
                builder.setNumber(number);
            if (intentData != null)
                builder.getExtras().putString(KEY_INTENT_DATA, intentData);
            if (null != group && group.length() > 0)
                builder.setGroup(group);
            builder.setGroupSummary(groupSummary);
            if (null != sortKey && sortKey.length() > 0)
                builder.setSortKey(sortKey);
            UnityNotificationManager.setNotificationGroupAlertBehavior(builder, groupAlertBehaviour);
            builder.setShowWhen(showTimestamp);
            return builder;
        } catch (Exception e) {
            Log.e(TAG_UNITY, "Failed to deserialize old style notification", e);
        } catch (OutOfMemoryError e) {
            Log.e(TAG_UNITY, "Failed to deserialize old style notification", e);
        }

        return null;
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
            Log.e(TAG_UNITY, "Failed to deserialize parcelable", e);
        } catch (OutOfMemoryError e) {
            Log.e(TAG_UNITY, "Failed to deserialize parcelable", e);
        }

        return null;
    }

    // Returns Activity class to be opened when notification is tapped
    // Search is done in this order:
    //   * class specified in meta-data key custom_notification_android_activity
    //   * the only enabled activity with name ending in either .UnityPlayerActivity or .UnityPlayerGameActivity
    //   * the only enabled activity in the package
    protected static Class<?> getOpenAppActivity(Context context) {
        try {
            PackageManager pm = context.getPackageManager();
            ApplicationInfo ai = pm.getApplicationInfo(context.getPackageName(), PackageManager.GET_META_DATA);
            Bundle bundle = ai.metaData;

            if (bundle.containsKey("custom_notification_android_activity")) {
                try {
                    return Class.forName(bundle.getString("custom_notification_android_activity"));
                } catch (ClassNotFoundException e) {
                    Log.e(TAG_UNITY, "Specified activity class for notifications not found: " + e.getMessage());
                }
            }

            Log.w(TAG_UNITY, "No custom_notification_android_activity found, attempting to find app activity class");

            ActivityInfo[] aInfo = pm.getPackageInfo(context.getPackageName(), PackageManager.GET_ACTIVITIES).activities;
            if (aInfo == null) {
                Log.e(TAG_UNITY, "Could not get package activities");
                return null;
            }

            String activityClassName = null;
            boolean activityIsUnity = false, activityConflict = false;
            for (ActivityInfo info : aInfo) {
                // activity alias not supported
                if (!info.enabled || info.targetActivity != null)
                    continue;

                boolean candidateIsUnity = isUnityActivity(info.name);
                if (activityClassName == null) {
                    activityClassName = info.name;
                    activityIsUnity = candidateIsUnity;
                    continue;
                }

                // two Unity activities is a hard conflict
                // two non-Unity activities is a conflict unless we find a Unity activity later on
                if (activityIsUnity == candidateIsUnity) {
                    activityConflict = true;
                    if (activityIsUnity && candidateIsUnity)
                        break;
                    continue;
                }

                if (candidateIsUnity) {
                    activityClassName = info.name;
                    activityIsUnity = candidateIsUnity;
                    activityConflict = false;
                }
            }

            if (activityConflict) {
                Log.e(TAG_UNITY, "Multiple choices for activity for notifications, set activity explicitly in Notification Settings");
                return null;
            }

            if (activityClassName == null) {
                Log.e(TAG_UNITY, "Activity class for notifications not found");
                return null;
            }

            return Class.forName(activityClassName);
        } catch (PackageManager.NameNotFoundException e) {
            e.printStackTrace();
        } catch (ClassNotFoundException e) {
            Log.e(TAG_UNITY, "Failed to find activity class: " + e.getMessage());
        }

        return null;
    }

    private static boolean isUnityActivity(String name) {
        return name.endsWith(".UnityPlayerActivity") || name.endsWith(".UnityPlayerGameActivity");
    }

    protected static Notification.Builder recoverBuilder(Context context, Notification notification) {
        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                Notification.Builder builder = Notification.Builder.recoverBuilder(context, notification);
                // extras not recovered, transfer manually
                builder.setExtras(notification.extras);
                return builder;
            }
        } catch (Exception e) {
            Log.e(TAG_UNITY, "Failed to recover builder for notification!", e);
        } catch (OutOfMemoryError e) {
            Log.e(TAG_UNITY, "Failed to recover builder for notification!", e);
        }

        return recoverBuilderCustom(context, notification);
    }

    private static Notification.Builder recoverBuilderCustom(Context context, Notification notification) {
        String channelID = notification.extras.getString(KEY_CHANNEL_ID);
        Notification.Builder builder = UnityNotificationManager.getNotificationManagerImpl(context).createNotificationBuilder(channelID);
        UnityNotificationManager.setNotificationIcon(builder, KEY_SMALL_ICON, notification.extras.getString(KEY_SMALL_ICON));
        String largeIcon = notification.extras.getString(KEY_LARGE_ICON);
        if (largeIcon != null && !largeIcon.isEmpty())
            UnityNotificationManager.setNotificationIcon(builder, KEY_LARGE_ICON, largeIcon);
        builder.setContentTitle(notification.extras.getString(Notification.EXTRA_TITLE));
        builder.setContentText(notification.extras.getString(Notification.EXTRA_TEXT));
        builder.setAutoCancel(0 != (notification.flags & Notification.FLAG_AUTO_CANCEL));
        if (notification.number >= 0)
            builder.setNumber(notification.number);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            String bigText = notification.extras.getString(Notification.EXTRA_BIG_TEXT);
            if (bigText != null)
                builder.setStyle(new Notification.BigTextStyle().bigText(bigText));
        }

        builder.setWhen(notification.when);
        String group = notification.getGroup();
        if (group != null && !group.isEmpty())
            builder.setGroup(group);
        builder.setGroupSummary(0 != (notification.flags & Notification.FLAG_GROUP_SUMMARY));
        String sortKey = notification.getSortKey();
        if (sortKey != null && !sortKey.isEmpty())
            builder.setSortKey(sortKey);
        builder.setShowWhen(notification.extras.getBoolean(Notification.EXTRA_SHOW_WHEN, false));
        Integer color = UnityNotificationManager.getNotificationColor(notification);
        if (color != null)
            UnityNotificationManager.setNotificationColor(builder, color);
        UnityNotificationManager.setNotificationUsesChronometer(builder, notification.extras.getBoolean(Notification.EXTRA_SHOW_CHRONOMETER, false));
        UnityNotificationManager.setNotificationGroupAlertBehavior(builder, UnityNotificationManager.getNotificationGroupAlertBehavior(notification));

        builder.getExtras().putInt(KEY_ID, notification.extras.getInt(KEY_ID, 0));
        builder.getExtras().putLong(KEY_REPEAT_INTERVAL, notification.extras.getLong(KEY_REPEAT_INTERVAL, 0));
        builder.getExtras().putLong(KEY_FIRE_TIME, notification.extras.getLong(KEY_FIRE_TIME, 0));
        String intentData = notification.extras.getString(KEY_INTENT_DATA);
        if (intentData != null && !intentData.isEmpty())
            builder.getExtras().putString(KEY_INTENT_DATA, intentData);

        return builder;
    }
}
