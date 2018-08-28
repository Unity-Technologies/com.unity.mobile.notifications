package com.unity.androidnotifications;

import android.app.Activity;
import android.app.AlarmManager;
import android.app.Notification;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.res.Resources;
import android.graphics.BitmapFactory;
import android.os.Build;
import android.os.Debug;
import android.service.notification.StatusBarNotification;
import android.util.Log;
import android.content.SharedPreferences;

import static android.app.Notification.DEFAULT_VIBRATE;
import static android.app.Notification.PRIORITY_DEFAULT;
import static android.app.Notification.VISIBILITY_PUBLIC;

import java.text.DateFormat;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.Set;
import java.util.HashSet;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;


import java.lang.Integer;

public class UnityNotificationManager extends BroadcastReceiver
{

    public Context mContext;
    public Activity mActivity;
    private static NotificationCallback mNotificationCallback;
    public static NotificationManager mManager;
    /// Static stuff TODO cleanup

    public static final String UNITY_NOTIFICATION_SETTINGS = "UNITY_NOTIFICATIONS";
    public static final String SHARED_PREFS_NOTIFICATION_IDS = "UNITY_NOTIFICATION_IDS";
    public static final String DEFAULT_APP_ICON = "app_icon";

    public static int findResourceidInContextByName(String name, Context context, Activity activity)
    {
        if (name == null)
            return 0;

        Resources res = context.getResources();
        if (res != null)
        {
            int id = res.getIdentifier(name, "mipmap", activity.getPackageName());
            if (id == 0)
                return res.getIdentifier(name, "drawable", activity.getPackageName());
            else
                return id;
        }
        return 0;
    }

    public static UnityNotificationManager getNotificationManagerImpl(Context context, Activity activity) {

        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O)
        {
            return new UnityNotificationManager(context, activity);
        }
        else
        {
            return new UnityNotificationManagerOreo(context, activity);
        }
    }

    public static NotificationManager getNotificationManager(Context context)
    {
        return (NotificationManager)context.getSystemService(Context.NOTIFICATION_SERVICE);
    }

    public static  NotificationChannelWrapper getNotificationChannel(String id, Context context)
    {

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
        {
            return UnityNotificationManagerOreo.getOreoNotificationChannel(id, context);
        }

        SharedPreferences prefs = context.getSharedPreferences(String.format("unity_notification_channel_%s", id), Context.MODE_PRIVATE);
        NotificationChannelWrapper channel = new NotificationChannelWrapper();

        channel.id = id;
        channel.name = prefs.getString("title", "undefined");
        channel.importance = prefs.getInt("importance", NotificationManager.IMPORTANCE_DEFAULT);
        channel.description = prefs.getString("description", "undefined");
        channel.enableLights = prefs.getBoolean("enableLights", false);
        channel.enableVibration = prefs.getBoolean("enableVibration", false);
        channel.canBypassDnd = prefs.getBoolean("canBypassDnd", false);
        channel.canShowBadge = prefs.getBoolean("canShowBadge", false);
        channel.lockscreenVisibility = prefs.getInt("lockscreenVisibility", VISIBILITY_PUBLIC);
        String[] vibrationPatternStr = prefs.getString("vibrationPattern", "[]").split(",");

        long[] vibrationPattern = new long[vibrationPatternStr.length];

        if (vibrationPattern.length > 1)
        {
            for (int i = 0; i < vibrationPatternStr.length; i++)
            {
                try
                {
                    vibrationPattern[i] = Long.parseLong(vibrationPatternStr[i]);
                }
                catch (NumberFormatException e)
                {
                    vibrationPattern[i] = 1;
                }
            }
        }

        channel.vibrationPattern = vibrationPattern.length > 1 ? vibrationPattern : null;
        return channel;
    }

    public UnityNotificationManager()
    {
        super();
    }

    public UnityNotificationManager(Context context, Activity activity)
    {
        super();
        mContext = context;
        mActivity = activity;
    }

    public NotificationManager getNotificationManager()
    {
        return getNotificationManager(mContext);
    }

    public void setNotificationCallback(NotificationCallback notificationCallback)
    {
        UnityNotificationManager.mNotificationCallback = notificationCallback;
    }

    public void scheduleNotificationIntent(Intent intent)//int id, String channelID, String textTitle, String textContent, String smallIcon, boolean autoCancel, String category, int visibility, long[] vibrationPattern, boolean usesChronometer, Date originalTime, Date fireTime, long repeatInterval)
    {
        AlarmManager alarmManager;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M)
        {
            alarmManager = mActivity.getSystemService(AlarmManager.class);
        }
        else
        {
            alarmManager = (AlarmManager)mContext.getSystemService(Context.ALARM_SERVICE);
        }

        int id = intent.getIntExtra("id", 0);
        long repeatInterval = intent.getLongExtra("repeatInterval", 0L);
        long fireTime = intent.getLongExtra("fireTime", 0L);

        Intent openAppIntent = new Intent(mActivity, mActivity.getClass());
        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);

        PendingIntent tapIntent = PendingIntent.getActivity(mContext, id, openAppIntent, 0);
        intent.putExtra("tapIntent", tapIntent);

        SharedPreferences prefs = mContext.getSharedPreferences("UNITY_NOTIFICATIONS", Context.MODE_PRIVATE);
        Set<String> idsSet = prefs.getStringSet(SHARED_PREFS_NOTIFICATION_IDS, new HashSet<String>());

        idsSet.add(Integer.toString(id));

        SharedPreferences.Editor editor = prefs.edit();
        editor.putStringSet(SHARED_PREFS_NOTIFICATION_IDS, idsSet);
        editor.apply();


        if (repeatInterval <= 0)
        {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M)
            {
                alarmManager.setExactAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, fireTime, PendingIntent.getBroadcast(mActivity, id, intent, PendingIntent.FLAG_UPDATE_CURRENT));
            }
            else
            {
                alarmManager.set(AlarmManager.RTC_WAKEUP, fireTime, PendingIntent.getBroadcast(mActivity, id, intent, PendingIntent.FLAG_UPDATE_CURRENT));
            }
        }
        else
        {
            alarmManager.setInexactRepeating(AlarmManager.RTC_WAKEUP, fireTime, repeatInterval, PendingIntent.getBroadcast(mActivity, id, intent, PendingIntent.FLAG_UPDATE_CURRENT));
        }
    }

    public static void sendNotification(Intent intent, Context context)
    {
        int id = intent.getIntExtra("id", -1);
        String channelID = intent.getStringExtra("channelID");
        String textTitle = intent.getStringExtra("textTitle");
        String textContent = intent.getStringExtra("textContent");
        int smallIcon = intent.getIntExtra("smallIcon", 0);// R.drawable.ic_launcher_background);
        boolean autoCancel = intent.getBooleanExtra("autoCancel", true);
        long fireTime = intent.getLongExtra("fireTime", -1);
        boolean usesChronometer = intent.getBooleanExtra("usesChronometer", false);
        int largeIcon = intent.getIntExtra("largeIcon", 0);
        int lockscreenVisibility = intent.getIntExtra("lockscreenVisibility", 0);
        int style = intent.getIntExtra("style", 0);
        int color = intent.getIntExtra("color", 0);
        int number = intent.getIntExtra("number", 0);

        if (smallIcon == 0)
        {
            smallIcon = R.drawable.default_icon;
        }

        PendingIntent tapIntent = (PendingIntent)intent.getParcelableExtra("tapIntent");

        Notification.Builder notificationBuilder;

        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O)
        {
            notificationBuilder = new Notification.Builder(context);
        }
        else
        {
            notificationBuilder = new Notification.Builder(context, channelID);
        }

        if (largeIcon != 0)
        {
            notificationBuilder.setLargeIcon(BitmapFactory.decodeResource(context.getResources(), largeIcon));
        }

        notificationBuilder.setContentTitle(textTitle)
        .setContentText(textContent)
        .setSmallIcon(smallIcon)
        .setContentIntent(tapIntent)
        .setAutoCancel(autoCancel);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP)
        {
            if (color >= 0)
            {
                notificationBuilder.setColor(color);
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
                {
                    notificationBuilder.setColorized(true);
                }
            }
        }

        if (number >= 0)
            notificationBuilder.setNumber(number);

        if (style == 2)
            notificationBuilder.setStyle(new Notification.BigTextStyle().bigText(textContent));

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP_MR1)
        {
            notificationBuilder.setWhen(fireTime);
            notificationBuilder.setUsesChronometer(usesChronometer);
        }
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O)
        {
            NotificationChannelWrapper fakeNotificationChannel = getNotificationChannel(channelID, context);


            if (fakeNotificationChannel.vibrationPattern != null && fakeNotificationChannel.vibrationPattern.length > 0)
            {
                notificationBuilder.setDefaults(Notification.DEFAULT_LIGHTS | Notification.DEFAULT_SOUND);
                notificationBuilder.setVibrate(fakeNotificationChannel.vibrationPattern);
            }
            else
            {
                notificationBuilder.setDefaults(Notification.DEFAULT_ALL);
            }

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP)
            {
                notificationBuilder.setVisibility((int)fakeNotificationChannel.lockscreenVisibility);
            }

            // Need to convert Oreo channel importance to pre-Oreo priority.
            int priority;
            switch (fakeNotificationChannel.importance)
            {
                case NotificationManager.IMPORTANCE_HIGH:
                    priority = Notification.PRIORITY_MAX;
                    break;
                case NotificationManager.IMPORTANCE_DEFAULT:
                    priority = Notification.PRIORITY_DEFAULT;
                    break;
                case NotificationManager.IMPORTANCE_LOW:
                    priority = Notification.PRIORITY_LOW;
                    break;
                case NotificationManager.IMPORTANCE_NONE:
                    priority = Notification.PRIORITY_MIN;
                    break;
                default:
                    priority = Notification.PRIORITY_DEFAULT;
            }
            notificationBuilder.setPriority(priority);
        }

        getNotificationManager(context).notify(id, notificationBuilder.build());

        try {
            mNotificationCallback.onSentNotification(intent);
        }
        catch (RuntimeException ex)
        {
            Log.w("UnityNotifications", "Can not invoke OnNotificationReceived event when app is not running!");
        }
    }

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
        SharedPreferences prefs = mContext.getSharedPreferences(UNITY_NOTIFICATION_SETTINGS, Context.MODE_PRIVATE);
        Set<String> channelIdsSet = prefs.getStringSet("ChannelIDs", new HashSet<String>());
        channelIdsSet.add(id);

        SharedPreferences.Editor editor = prefs.edit();
        editor.putStringSet("ChannelIDs", channelIdsSet);
        editor.apply();

        SharedPreferences channelPrefs = mContext.getSharedPreferences(String.format("unity_notification_channel_%s", id), Context.MODE_PRIVATE);
        editor = channelPrefs.edit();

        editor.putString("title", title);
        editor.putInt("importance", importance);
        editor.putString("description", description);
        editor.putBoolean("enableLights", enableLights);
        editor.putBoolean("enableVibration", enableVibration);
        editor.putBoolean("canBypassDnd", canBypassDnd);
        editor.putBoolean("canShowBadge", canShowBadge);
        editor.putString("vibrationPattern", Arrays.toString(vibrationPattern));
        editor.putInt("lockscreenVisibility", lockscreenVisibility);

        editor.apply();
    }

    public Object[] getNotificationChannels()
    {
        SharedPreferences prefs = mContext.getSharedPreferences(UNITY_NOTIFICATION_SETTINGS, Context.MODE_PRIVATE);
        Set<String> channelIdsSet = prefs.getStringSet("ChannelIDs", new HashSet<String>());

        ArrayList<NotificationChannelWrapper> channels = new ArrayList<>();

        for (String k : channelIdsSet)
        {
            channels.add(getNotificationChannel(k));
        }
        return channels.toArray();
    }

    public void deleteNotificationChannel(String id)
    {
            SharedPreferences prefs = mContext.getSharedPreferences(UNITY_NOTIFICATION_SETTINGS, Context.MODE_PRIVATE);
            Set<String> channelIdsSet = prefs.getStringSet("ChannelIDs", new HashSet<String>());

            if (channelIdsSet.contains(id)) {

                channelIdsSet.remove(id);

                SharedPreferences.Editor editor = prefs.edit();
                editor.putStringSet("ChannelIDs", channelIdsSet);
                editor.apply();

                SharedPreferences channelPrefs = mContext.getSharedPreferences(String.format("unity_notification_channel_%s", id), Context.MODE_PRIVATE);
                editor = channelPrefs.edit();
                editor.clear();
                editor.apply();
            }
    }

    public NotificationChannelWrapper getNotificationChannel(String id)
    {
        return  UnityNotificationManager.getNotificationChannel(id, mContext);
    }

    public int[] getScheduledNotificationIDs()
    {
        SharedPreferences prefs = mContext.getSharedPreferences("UNITY_NOTIFICATIONS", Context.MODE_PRIVATE);
        Set<String> idsSet = prefs.getStringSet(SHARED_PREFS_NOTIFICATION_IDS, new HashSet<String>());

        String[] idsArrStr = idsSet.toArray(new String[idsSet.size()]);
        int[] idsArrInt = new int[idsSet.size()];

        for (int i = 0; i < idsSet.size(); i++)
        {
            idsArrInt[i] = Integer.valueOf(idsArrStr[i]);
        }
        return idsArrInt;
    }

    public void getScheduledNotifications()
    {
        int[] ids = getScheduledNotificationIDs();
        ArrayList<PendingIntent> intents = new ArrayList<PendingIntent>();

        for (int id : ids)
        {
            PendingIntent pIntent = retrieveScheduledNotification(id);
            if (pIntent != null)
            {
                intents.add(pIntent);
            }
        }
    }

    public Intent CreateNotificationIntent()
    {
        return new Intent(mActivity, UnityNotificationManager.class);
    }

    private PendingIntent retrieveScheduledNotification(int requestCode)
    {
        Intent intent = CreateNotificationIntent();
        PendingIntent pendingIntent = PendingIntent.getService(mContext, requestCode, intent, 0);
        return pendingIntent;
    }

    public int checkNotificationStatus(int requestCode)
    {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M)
        {
            for (StatusBarNotification n : getNotificationManager().getActiveNotifications())
            {
                if (requestCode == n.getId())
                    return 2;
            }

            if (checkIfPendingNotificationIsRegistered(requestCode))
                return 1;

            return 0;
        }
        return -1;
    }

    public boolean checkIfPendingNotificationIsRegistered(int requestCode)
    {
        Intent intent = CreateNotificationIntent();
        return (PendingIntent.getBroadcast(mContext, requestCode, intent, PendingIntent.FLAG_NO_CREATE) != null);
    }

    public void cancelAllPendingNotificationIntents()
    {
        int[] ids = this.getScheduledNotificationIDs();

        for (int id : ids)
        {
            cancelPendingNotificationIntent(id);
        }
    }

    public void cancelPendingNotificationIntent(int requestCode)
    {
        Intent intent = CreateNotificationIntent();
        PendingIntent broadcast = PendingIntent.getBroadcast(mContext, requestCode,
                intent, PendingIntent.FLAG_NO_CREATE);
        if (broadcast != null)
        {
            broadcast.cancel();

            SharedPreferences prefs = mContext.getSharedPreferences("UNITY_NOTIFICATIONS", Context.MODE_PRIVATE);
            Set<String> idsSet = prefs.getStringSet(SHARED_PREFS_NOTIFICATION_IDS, new HashSet<String>());

            idsSet.remove(Integer.toString(requestCode));

            SharedPreferences.Editor editor = prefs.edit();
            editor.putStringSet(SHARED_PREFS_NOTIFICATION_IDS, idsSet);
            editor.apply();
        }
    }

    public void cancelAllNotifications()
    {
        getNotificationManager().cancelAll();
    }

    @Override
    public void onReceive(Context context, Intent intent)
    {
        UnityNotificationManager.sendNotification(intent, context);
    }

}
