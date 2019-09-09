package com.unity.androidnotifications;

import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.support.annotation.Keep;
import android.util.Log;

import java.util.Calendar;
import java.util.Date;
import java.util.List;

@Keep
public class UnityNotificationRestartOnBootReceiver extends BroadcastReceiver {

    @Override
    public void onReceive(Context context, Intent received_intent) {
        if (Intent.ACTION_BOOT_COMPLETED.equals(received_intent.getAction())) {

            List<Intent> saved_notifications = UnityNotificationManager.LoadNotificationIntents(context);

            for (Intent data_intent : saved_notifications) {

                long fireTime = data_intent.getLongExtra("fireTime", 0L);
                Date currentDate = Calendar.getInstance().getTime();
                Date fireTimeDate=new Date(fireTime);

                int id = data_intent.getIntExtra("id", -1);
                boolean isRepeatable = data_intent.getLongExtra("repeatInterval", 0L) > 0;

                if (fireTimeDate.after(currentDate) || isRepeatable) {

                    if (BuildConfig.DEBUG) {
                        Log.w("UnityNotifications", String.format(" Rescheduling notification on boot : %d", id));
                    }

                    Intent openAppIntent = UnityNotificationManager.buildOpenAppIntent(data_intent, context, UnityNotificationManager.GetOpenAppActivity(context, true));

                    PendingIntent pendingIntent = PendingIntent.getActivity(context, id, openAppIntent, 0);
                    Intent intent = UnityNotificationManager.prepareNotificationIntent(data_intent, context, pendingIntent);

                    PendingIntent broadcast = PendingIntent.getBroadcast(context, id, intent, PendingIntent.FLAG_UPDATE_CURRENT);
                    UnityNotificationManager.scheduleNotificationIntentAlarm(intent, context, broadcast);
                }
                else
                {
                    UnityNotificationManager.deleteExpiredNotificationIntent(id, context);
                }
            }
        }
    }
}