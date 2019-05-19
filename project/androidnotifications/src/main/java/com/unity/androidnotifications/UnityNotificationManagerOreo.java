package com.unity.androidnotifications;

import android.app.Activity;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.content.Context;
import android.content.SharedPreferences;
import android.os.Build;
import android.support.annotation.Keep;

import java.util.ArrayList;
import java.util.List;

import static android.app.Notification.PRIORITY_DEFAULT;
import static android.app.Notification.VISIBILITY_PUBLIC;
import static android.app.NotificationManager.IMPORTANCE_NONE;

@Keep
public class UnityNotificationManagerOreo extends UnityNotificationManagerNougat {

    public static NotificationChannelWrapper NotificationChannelToWrapper(NotificationChannel channel)
    {
        NotificationChannelWrapper wrapper = new NotificationChannelWrapper();

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
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
        }
        return wrapper;
    }

    public static NotificationChannelWrapper getOreoNotificationChannel(String id, Context context)
    {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O)
        {
            return null;
        }
        List<NotificationChannelWrapper> channelList = new ArrayList<NotificationChannelWrapper>();

        for(NotificationChannel ch : getNotificationManager(context).getNotificationChannels())
        {
            if (ch.getId() == id)
                return NotificationChannelToWrapper(ch);
        }
        return null;
    }


    public UnityNotificationManagerOreo(Context context, Activity activity)
    {
        super(context, activity);
    }

    @Override
    public void registerNotificationChannel(
            String id,
            String title,
            int importance,
            String description,
            boolean enableLights,
            boolean enableVibration,
            boolean canBypassDnd,
            boolean canShowBadge,
            long[] vibrationPattern,
            int lockscreenVisibility)
    {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
        {
            NotificationChannel channel = new NotificationChannel(id, title, importance);
            channel.setDescription(description);
            channel.enableLights(enableLights);
            channel.enableVibration(enableVibration);
            channel.setBypassDnd(canBypassDnd);
            channel.setShowBadge(canShowBadge);
            channel.setVibrationPattern(vibrationPattern);
            channel.setLockscreenVisibility(lockscreenVisibility);

            getNotificationManager().createNotificationChannel(channel);
        }
    }

    @Override
    public NotificationChannelWrapper[] getNotificationChannels()
    {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O)
        {
            return null;
        }
        List<NotificationChannelWrapper> channelList = new ArrayList<NotificationChannelWrapper>();

        for(NotificationChannel ch : getNotificationManager().getNotificationChannels())
        {
            channelList.add(NotificationChannelToWrapper(ch));
        }

        return channelList.toArray(new NotificationChannelWrapper[channelList.size()]);
    }

    @Override
    public void deleteNotificationChannel(String id)
    {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
        {
            getNotificationManager().deleteNotificationChannel(id);
        }
    }

}
