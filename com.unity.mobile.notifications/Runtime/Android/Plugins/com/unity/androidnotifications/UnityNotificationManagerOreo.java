package com.unity.androidnotifications;

import android.app.Activity;
import android.app.NotificationChannel;
import android.content.Context;
import android.os.Build;

import java.util.ArrayList;
import java.util.List;

public class UnityNotificationManagerOreo extends UnityNotificationManager {
    public UnityNotificationManagerOreo(Context context, Activity activity) {
        super(context, activity);
    }

    protected static NotificationChannelWrapper getOreoNotificationChannel(Context context, String id) {
        assert Build.VERSION.SDK_INT >= Build.VERSION_CODES.O;

        List<NotificationChannelWrapper> channelList = new ArrayList<NotificationChannelWrapper>();

        for (NotificationChannel ch : getNotificationManager(context).getNotificationChannels()) {
            if (ch.getId() == id)
                return notificationChannelToWrapper(ch);
        }
        return null;
    }

    @Override
    public void deleteNotificationChannel(String id) {
        assert Build.VERSION.SDK_INT >= Build.VERSION_CODES.O;

        getNotificationManager().deleteNotificationChannel(id);
    }

    @Override
    public NotificationChannelWrapper[] getNotificationChannels() {
        assert Build.VERSION.SDK_INT >= Build.VERSION_CODES.O;

        List<NotificationChannelWrapper> channelList = new ArrayList<NotificationChannelWrapper>();

        for (NotificationChannel ch : getNotificationManager().getNotificationChannels()) {
            channelList.add(notificationChannelToWrapper(ch));
        }

        return channelList.toArray(new NotificationChannelWrapper[channelList.size()]);
    }
}
