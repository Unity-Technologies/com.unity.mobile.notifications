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

    @Override
    public NotificationChannelWrapper[] getNotificationChannels() {
        assert Build.VERSION.SDK_INT >= Build.VERSION_CODES.O;

        List<NotificationChannel> channels = getNotificationManager().getNotificationChannels();
        NotificationChannelWrapper[] channelList = new NotificationChannelWrapper[channels.size()];
        int i = 0;
        for (NotificationChannel ch : channels) {
            channelList[i++] = notificationChannelToWrapper(ch);
        }

        return channelList;
    }
}
