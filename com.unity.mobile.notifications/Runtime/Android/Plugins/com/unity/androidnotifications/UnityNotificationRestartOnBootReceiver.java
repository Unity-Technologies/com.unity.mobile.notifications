package com.unity.androidnotifications;

import android.app.Notification;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;

import java.util.Calendar;
import java.util.Date;
import java.util.List;

import static com.unity.androidnotifications.UnityNotificationManager.KEY_FIRE_TIME;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_REPEAT_INTERVAL;

public class UnityNotificationRestartOnBootReceiver extends BroadcastReceiver {

    @Override
    public void onReceive(Context context, Intent received_intent) {
        if (Intent.ACTION_BOOT_COMPLETED.equals(received_intent.getAction())) {
            rescheduleSavedNotifications(context);
        }
    }

    private static void rescheduleSavedNotifications(Context context) {
        List<Notification.Builder> saved_notifications = UnityNotificationManager.loadSavedNotifications(context);

        for (Notification.Builder notificationBuilder : saved_notifications) {
            Bundle extras = notificationBuilder.getExtras();
            long repeatInterval = extras.getLong(KEY_REPEAT_INTERVAL, 0L);
            long fireTime = extras.getLong(KEY_FIRE_TIME, 0L);
            Date currentDate = Calendar.getInstance().getTime();
            Date fireTimeDate = new Date(fireTime);

            boolean isRepeatable = repeatInterval > 0;

            if (fireTimeDate.after(currentDate) || isRepeatable) {
                UnityNotificationManager.scheduleAlarmWithNotification(notificationBuilder, context);
            }
        }
    }
}
