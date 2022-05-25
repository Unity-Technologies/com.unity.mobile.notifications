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

    protected static NotificationChannelWrapper notificationChannelToWrapper(NotificationChannel channel) {
        NotificationChannelWrapper wrapper = new NotificationChannelWrapper();

        wrapper.id = channel.getId();
        wrapper.name = channel.getName().toString();
        wrapper.importance = channel.getImportance();
        wrapper.description = channel.getDescription();
        wrapper.enableLights = channel.shouldShowLights();
        wrapper.enableVibration = channel.shouldVibrate();
        wrapper.canBypassDnd = channel.canBypassDnd();
        wrapper.canShowBadge = channel.canShowBadge();
        wrapper.vibrationPattern = channel.getVibrationPattern();
        wrapper.lockscreenVisibility = channel.getLockscreenVisibility();

        return wrapper;
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
