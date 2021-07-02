package com.unity.androidnotifications;

import android.app.Notification;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;

import java.util.Calendar;
import java.util.Date;
import java.util.List;

public class UnityNotificationRestartOnBootReceiver extends BroadcastReceiver {

    @Override
    public void onReceive(Context context, Intent received_intent) {
        if (Intent.ACTION_BOOT_COMPLETED.equals(received_intent.getAction())) {
            List<Intent> saved_notifications = UnityNotificationManager.loadNotificationIntents(context);

            for (Intent data_intent : saved_notifications) {
                Notification notification = data_intent.getParcelableExtra("unityNotification");
                if (notification == null)
                    continue;
                long repeatInterval = notification.extras.getLong("repeatInterval", 0L);
                long fireTime = notification.extras.getLong("fireTime", 0L);
                Date currentDate = Calendar.getInstance().getTime();
                Date fireTimeDate = new Date(fireTime);

                int id = notification.extras.getInt("id", -1);
                boolean isRepeatable = repeatInterval > 0;

                if (fireTimeDate.after(currentDate) || isRepeatable) {
                    Intent openAppIntent = UnityNotificationManager.buildOpenAppIntent(context, UnityNotificationUtilities.getOpenAppActivity(context, true));
                    openAppIntent.putExtra("unityNotification", notification);

                    PendingIntent pendingIntent = PendingIntent.getActivity(context, id, openAppIntent, 0);
                    Intent intent = UnityNotificationManager.buildNotificationIntent(context, id);
                    Notification.Builder notificationBuilder = Notification.Builder.recoverBuilder(context, notification);
                    notificationBuilder.setContentIntent(pendingIntent);
                    UnityNotificationManager.finalizeNotificationForDisplay(context, notificationBuilder);
                    notification = notificationBuilder.build();
                    intent.putExtra("unityNotification", notification);

                    PendingIntent broadcast = PendingIntent.getBroadcast(context, id, intent, PendingIntent.FLAG_UPDATE_CURRENT);
                    UnityNotificationManager.scheduleNotificationIntentAlarm(context, repeatInterval, fireTime, broadcast);
                } else {
                    UnityNotificationManager.deleteExpiredNotificationIntent(context, Integer.toString(id));
                }
            }
        }
    }
}
