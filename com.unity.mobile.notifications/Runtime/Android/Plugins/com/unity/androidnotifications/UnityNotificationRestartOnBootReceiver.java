package com.unity.androidnotifications;

import android.app.Notification;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
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

    @Override
    public void onReceive(Context context, Intent received_intent) {
        Log.d(TAG_UNITY, "Rescheduling notifications after restart");
        if (Intent.ACTION_BOOT_COMPLETED.equals(received_intent.getAction())) {
            rescheduleSavedNotifications(context);
        }
    }

    private static void rescheduleSavedNotifications(Context context) {
        UnityNotificationManager manager = UnityNotificationManager.getNotificationManagerImpl(context);
        List<Notification.Builder> saved_notifications = manager.loadSavedNotifications();

        for (Notification.Builder notificationBuilder : saved_notifications) {
            Bundle extras = notificationBuilder.getExtras();
            long repeatInterval = extras.getLong(KEY_REPEAT_INTERVAL, 0L);
            long fireTime = extras.getLong(KEY_FIRE_TIME, 0L);
            Date currentDate = Calendar.getInstance().getTime();
            Date fireTimeDate = new Date(fireTime);

            boolean isRepeatable = repeatInterval > 0;

            if (fireTimeDate.after(currentDate) || isRepeatable) {
                manager.scheduleAlarmWithNotification(notificationBuilder);
            } else {
                Log.d(TAG_UNITY, "Notification expired, not rescheduling, ID: " + extras.getInt(KEY_ID, -1));
            }
        }
    }
}
