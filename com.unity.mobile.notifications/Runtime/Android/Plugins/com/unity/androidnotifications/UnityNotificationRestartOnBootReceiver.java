package com.unity.androidnotifications;

import android.app.Notification;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;

import java.util.Calendar;
import java.util.Date;
import java.util.List;

import static com.unity.androidnotifications.UnityNotificationManager.KEY_FIRE_TIME;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_ID;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_NOTIFICATION;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_REPEAT_INTERVAL;

public class UnityNotificationRestartOnBootReceiver extends BroadcastReceiver {

    @Override
    public void onReceive(Context context, Intent received_intent) {
        if (Intent.ACTION_BOOT_COMPLETED.equals(received_intent.getAction())) {
            List<Intent> saved_notifications = UnityNotificationManager.loadNotificationIntents(context);

            for (Intent data_intent : saved_notifications) {
                Notification notification = data_intent.getParcelableExtra(KEY_NOTIFICATION);
                if (notification == null)
                    continue;
                long repeatInterval = notification.extras.getLong(KEY_REPEAT_INTERVAL, 0L);
                long fireTime = notification.extras.getLong(KEY_FIRE_TIME, 0L);
                Date currentDate = Calendar.getInstance().getTime();
                Date fireTimeDate = new Date(fireTime);

                int id = notification.extras.getInt(KEY_ID, -1);
                boolean isRepeatable = repeatInterval > 0;

                if (fireTimeDate.after(currentDate) || isRepeatable) {
                    Intent openAppIntent = UnityNotificationManager.buildOpenAppIntent(context, UnityNotificationUtilities.getOpenAppActivity(context, true));
                    openAppIntent.putExtra(KEY_NOTIFICATION, notification);

                    PendingIntent pendingIntent = PendingIntent.getActivity(context, id, openAppIntent, 0);
                    Intent intent = UnityNotificationManager.buildNotificationIntent(context);
                    Notification.Builder notificationBuilder = UnityNotificationUtilities.recoverBuilder(context, notification);
                    if (notificationBuilder == null)
                        continue;
                    notificationBuilder.setContentIntent(pendingIntent);
                    UnityNotificationManager.finalizeNotificationForDisplay(context, notificationBuilder);
                    notification = notificationBuilder.build();
                    intent.putExtra(KEY_NOTIFICATION, notification);

                    PendingIntent broadcast = UnityNotificationManager.getBroadcastPendingIntent(context, id, intent, PendingIntent.FLAG_UPDATE_CURRENT);
                    UnityNotificationManager.scheduleNotificationIntentAlarm(context, repeatInterval, fireTime, broadcast);
                } else {
                    String idStr = String.valueOf(id);
                    UnityNotificationManager.removeScheduledNotificationID(context, idStr);
                    UnityNotificationManager.deleteExpiredNotificationIntent(context, idStr);
                }
            }
        }
    }
}
