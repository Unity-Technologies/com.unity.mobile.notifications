package com.unity.androidnotifications;

import java.io.ByteArrayOutputStream;
import java.io.DataOutputStream;
import android.app.Notification;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Build;
import android.util.Base64;
import android.util.Log;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_ID;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_FIRE_TIME;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_REPEAT_INTERVAL;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_INTENT_DATA;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_SHOW_IN_FOREGROUND;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_SMALL_ICON;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_LARGE_ICON;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_NOTIFICATION;
import static com.unity.androidnotifications.UnityNotificationManager.TAG_UNITY;
import static com.unity.androidnotifications.UnityNotificationUtilities.UNITY_MAGIC_NUMBER;
import static com.unity.androidnotifications.UnityNotificationUtilities.SAVED_NOTIFICATION_PRIMARY_KEY;
import static com.unity.androidnotifications.UnityNotificationUtilities.SAVED_NOTIFICATION_FALLBACK_KEY;
import static com.unity.androidnotifications.UnityNotificationUtilities.serializeNotificationParcel;
import static com.unity.androidnotifications.UnityNotificationUtilities.serializeParcelable;
import static com.unity.androidnotifications.UnityNotificationUtilities.serializeString;

// Java class for testing purposes, not included in regular build
public class UnityNotificationTestUtils {
    // copy-paste of what serialization was in version 0 (except for hardcoded version number in here)
    private static boolean serializeNotificationCustom_v0(Notification notification, DataOutputStream out) {
        try {
            out.write(UNITY_MAGIC_NUMBER);
            out.writeInt(0);  // NOTIFICATION_SERIALIZATION_VERSION

            // serialize extras
            boolean showWhen = notification.extras.getBoolean(Notification.EXTRA_SHOW_WHEN, false);
            byte[] extras = serializeParcelable(notification.extras);
            out.writeInt(extras == null ? 0 : extras.length);
            if (extras != null && extras.length > 0)
                out.write(extras);
            else {
                // parcelling may fail in case it contains binder object, when that happens serialize manually what we care about
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

    // copy-paste of what serialization was in version 1 (except for hardcoded version number in here)
    private static boolean serializeNotificationCustom_v1(Notification notification, DataOutputStream out) {
        try {
            out.write(UNITY_MAGIC_NUMBER);
            out.writeInt(1);  // NOTIFICATION_SERIALIZATION_VERSION

            // serialize extras
            boolean showWhen = notification.extras.getBoolean(Notification.EXTRA_SHOW_WHEN, false);
            byte[] extras = serializeParcelable(notification.extras);
            out.writeInt(extras == null ? 0 : extras.length);
            if (extras != null && extras.length > 0)
                out.write(extras);
            else {
                // parcelling may fail in case it contains binder object, when that happens serialize manually what we care about
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

    // copy-paste of serializeNotification when it was primary & fallback keys
    // minor altereration: call serializeNotificationCustom_v1
    private static void serializeNotification(SharedPreferences prefs, Notification notification) {
        try {
            String serialized = null, fallback = null;
            ByteArrayOutputStream data = new ByteArrayOutputStream();
            DataOutputStream out = new DataOutputStream(data);
            if (serializeNotificationCustom_v1(notification, out)) {
                out.flush();
                byte[] bytes = data.toByteArray();
                fallback = Base64.encodeToString(bytes, 0, bytes.length, 0);
            }
            data.reset();
            Intent intent = new Intent();
            intent.putExtra(KEY_NOTIFICATION, notification);
            if (serializeNotificationParcel(intent, out)) {
                out.close();
                byte[] bytes = data.toByteArray();
                serialized = Base64.encodeToString(bytes, 0, bytes.length, 0);
            }
            else
                serialized = fallback;

            SharedPreferences.Editor editor = prefs.edit().clear();
            editor.putString(SAVED_NOTIFICATION_PRIMARY_KEY, serialized);
            editor.putString(SAVED_NOTIFICATION_FALLBACK_KEY, fallback);
            editor.apply();
        } catch (Exception e) {
            Log.e(TAG_UNITY, "Failed to serialize notification", e);
        }
    }

    // copy-paste of what serialization was in version 2 (except for hardcoded version number in here)
    private static boolean serializeNotificationCustom_v2(Notification notification, DataOutputStream out) {
        try {
            out.write(UNITY_MAGIC_NUMBER);
            out.writeInt(2); // NOTIFICATION_SERIALIZATION_VERSION

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
}
