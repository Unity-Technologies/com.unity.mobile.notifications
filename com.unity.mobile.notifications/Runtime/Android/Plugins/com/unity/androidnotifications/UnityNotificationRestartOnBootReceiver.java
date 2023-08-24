package com.unity.androidnotifications;

import android.app.Notification;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.os.AsyncTask;
import android.os.Bundle;
import android.util.Log;

import java.util.Calendar;
import java.util.Date;
import java.util.List;

import static com.unity.androidnotifications.UnityNotificationManager.KEY_FIRE_TIME;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_ID;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_REPEAT_INTERVAL;
import static com.unity.androidnotifications.UnityNotificationManager.TAG_UNITY;

public class UnityNotificationRestartOnBootReceiver extends BroadcastReceiver {
    private static final long EXPIRATION_TRESHOLD = 600000;  // 10 minutes

    @Override
    public void onReceive(Context context, Intent received_intent) {
        Log.d(TAG_UNITY, "Rescheduling notifications after restart");
        if (Intent.ACTION_BOOT_COMPLETED.equals(received_intent.getAction())) {
            AsyncTask.execute(() -> { rescheduleSavedNotifications(context); });
        }
    }

    private static void rescheduleSavedNotifications(Context context) {
        UnityNotificationManager manager = UnityNotificationManager.getNotificationManagerImpl(context);
        List<Notification.Builder> saved_notifications = manager.loadSavedNotifications();
        Date currentDate = Calendar.getInstance().getTime();

        for (Notification.Builder notificationBuilder : saved_notifications) {
            rescheduleNotification(manager, currentDate, notificationBuilder);
        }
    }

    private static boolean rescheduleNotification(UnityNotificationManager manager, Date currentDate, Notification.Builder notificationBuilder) {
        try {
            Bundle extras = notificationBuilder.getExtras();
            long repeatInterval = extras.getLong(KEY_REPEAT_INTERVAL, 0L);
            long fireTime = extras.getLong(KEY_FIRE_TIME, 0L);
            Date fireTimeDate = new Date(fireTime);

            boolean isRepeatable = repeatInterval > 0;

            if (fireTimeDate.after(currentDate) || isRepeatable) {
                manager.scheduleAlarmWithNotification(notificationBuilder);
                return true;
            } else if (currentDate.getTime() - fireTime < EXPIRATION_TRESHOLD) {
                // notification is in the past, but not by much, send now
                int id = extras.getInt(KEY_ID);
                manager.notify(id, notificationBuilder);
                return true;
            } else {
                Log.d(TAG_UNITY, "Notification expired, not rescheduling, ID: " + extras.getInt(KEY_ID, -1));
                return false;
            }
        } catch (Exception e) {
            Log.e(TAG_UNITY, "Failed to reschedule notification", e);
            return false;
        }
    }
}
