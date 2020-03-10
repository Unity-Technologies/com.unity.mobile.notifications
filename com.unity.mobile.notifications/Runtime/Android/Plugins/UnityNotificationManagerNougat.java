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

    // Send notification for Android N and above.
    protected static void sendNotificationNougat(Context context, Intent intent) {
        Notification.Builder notificationBuilder = UnityNotificationManager.buildNotification(context, intent);

        // TODO: setGroup/setGroupSummary/setSortKey are introduced in API Level 20, why we check N here?
        // https://developer.android.com/reference/android/app/Notification.Builder
        // If this is something we should move to UnityNotificationManager.buildNotification(), we probably can remove
        // UnityNotificationManagerNougat as we don't need to do something special for Android N.
        if (Build.VERSION.SDK_INT > Build.VERSION_CODES.N) {
            String group = intent.getStringExtra("group");
            if (group != null && group.length() > 0) {
                notificationBuilder.setGroup(group);
            }

            boolean groupSummary = intent.getBooleanExtra("groupSummary", false);
            if (groupSummary)
                notificationBuilder.setGroupSummary(groupSummary);

            String sortKey = intent.getStringExtra("sortKey");
            if (sortKey != null && sortKey.length() > 0) {
                notificationBuilder.setSortKey(sortKey);
            }

            // groupAlertBehaviour is only supported for Android O and above.
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                int groupAlertBehaviour = intent.getIntExtra("groupAlertBehaviour", -1);
                if (groupAlertBehaviour >= 0) {
                    notificationBuilder.setGroupAlertBehavior(groupAlertBehaviour);
                }
            }
        }

        int id = intent.getIntExtra("id", -1);
        UnityNotificationManager.notify(context, id, notificationBuilder.build(), intent);
    }
}
