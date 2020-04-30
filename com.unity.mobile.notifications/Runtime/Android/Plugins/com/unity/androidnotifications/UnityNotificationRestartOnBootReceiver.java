package com.unity.androidnotifications;

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
                long fireTime = data_intent.getLongExtra("fireTime", 0L);
                Date currentDate = Calendar.getInstance().getTime();
                Date fireTimeDate = new Date(fireTime);

                int id = data_intent.getIntExtra("id", -1);
                boolean isRepeatable = data_intent.getLongExtra("repeatInterval", 0L) > 0;

                if (fireTimeDate.after(currentDate) || isRepeatable) {
                    Intent openAppIntent = UnityNotificationManager.buildOpenAppIntent(data_intent, context, UnityNotificationUtilities.getOpenAppActivity(context, true));

                    PendingIntent pendingIntent = PendingIntent.getActivity(context, id, openAppIntent, 0);
                    Intent intent = UnityNotificationManager.buildNotificationIntent(context, data_intent, pendingIntent);

                    PendingIntent broadcast = PendingIntent.getBroadcast(context, id, intent, PendingIntent.FLAG_UPDATE_CURRENT);
                    UnityNotificationManager.scheduleNotificationIntentAlarm(context, intent, broadcast);
                } else {
                    UnityNotificationManager.deleteExpiredNotificationIntent(context, Integer.toString(id));
                }
            }
        }
    }
}
