package com.unity.androidnotifications;

import android.app.Activity;
import android.app.Notification;
import android.content.Context;
import android.content.Intent;
import android.os.Build;
import android.support.annotation.Keep;

@Keep
public class UnityNotificationManagerNougat extends UnityNotificationManager {
    public UnityNotificationManagerNougat(Context context, Activity activity) {
        super(context, activity);
    }

    protected static void sendNotificationNougat(Context context, Intent intent) {
        Notification.Builder notificationBuilder = UnityNotificationManager.buildNotification(context, intent);
        int id = intent.getIntExtra("id", -1);

        if (Build.VERSION.SDK_INT > Build.VERSION_CODES.N) {
            String group = intent.getStringExtra("group");
            boolean groupSummary = intent.getBooleanExtra("groupSummary", false);

            String sortKey = intent.getStringExtra("sortKey");
            int groupAlertBehaviour = intent.getIntExtra("groupAlertBehaviour", -1);

            if (group != null && group.length() > 0) {
                notificationBuilder.setGroup(group);
            }

            if (groupSummary)
                notificationBuilder.setGroupSummary(groupSummary);

            if (sortKey != null && sortKey.length() > 0) {
                notificationBuilder.setSortKey(sortKey);
            }

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                if (groupAlertBehaviour >= 0) {
                    notificationBuilder.setGroupAlertBehavior(groupAlertBehaviour);
                }
            }
        }

        UnityNotificationManager.notify(context, id, notificationBuilder, intent);
    }
}
