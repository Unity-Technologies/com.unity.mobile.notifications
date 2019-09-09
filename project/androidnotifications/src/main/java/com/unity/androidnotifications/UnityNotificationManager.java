package com.unity.androidnotifications;

import android.app.Activity;
import android.app.AlarmManager;
import android.app.Notification;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.pm.ActivityInfo;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.content.res.Resources;
import android.graphics.BitmapFactory;
import android.os.Build;
import android.os.Bundle;
import android.os.Parcel;
import android.service.notification.StatusBarNotification;
import android.support.annotation.Keep;
import android.util.Base64;
import android.util.Log;
import android.content.SharedPreferences;

import static android.app.Notification.VISIBILITY_PUBLIC;

import java.time.Duration;
import java.time.Instant;
import java.util.Calendar;
import java.util.Collections;
import java.util.Date;
import java.util.Set;
import java.util.HashSet;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;


import java.lang.Integer;

@Keep
public class UnityNotificationManager extends BroadcastReceiver
{
    private static NotificationCallback mNotificationCallback;
    public static UnityNotificationManager mManager;

    public Context mContext = null;
    public Activity mActivity = null;
    public Class mOpenActivity = null;
    public boolean reschedule_on_restart = false;

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

    public static UnityNotificationManager getNotificationManagerImpl(Context context) {

        if (mManager != null)
            return mManager;

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            mManager = new UnityNotificationManagerOreo(context, (Activity) context);

        } else if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            mManager = new UnityNotificationManagerNougat(context, (Activity) context);
        }
        else
        {
            mManager = new UnityNotificationManager(context, (Activity) context);
        }

        return mManager;
    }


    public static UnityNotificationManager getNotificationManagerImpl(Context context, Activity activity) {

        if (mManager != null)
            return mManager;

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            mManager = new UnityNotificationManagerOreo(context, activity);

        } else if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            mManager = new UnityNotificationManagerNougat(context, activity);
        }
        else
        {
            mManager = new UnityNotificationManager(context, activity);
        }

        return mManager;
    }

    public static String SerializeNotificationIntent(Intent intent) {
        Bundle bundle = intent.getExtras();

        Parcel parcel = Parcel.obtain();
        bundle.writeToParcel(parcel, 0);
        byte[] byt = parcel.marshall();

        return Base64.encodeToString(byt, 0, byt.length, 0);
    }

    public static Intent DeserializeNotificationIntent(String s, Context context)
    {

        byte[] newByt = Base64.decode(s, 0);

        Bundle newBundle = new Bundle();
        Parcel newParcel = Parcel.obtain();
        newParcel.unmarshall(newByt, 0, newByt.length);
        newParcel.setDataPosition(0);
        newBundle.readFromParcel(newParcel);

        Intent intent = new Intent(context, UnityNotificationManager.class);
        intent.putExtras(newBundle);

        return intent;
    }

    public static void SaveNotificationIntent(Intent intent, Context context) {

        String notification_id = Integer.toString(intent.getIntExtra("id", 0));;
        SharedPreferences prefs = context.getSharedPreferences(String.format("u_notification_data_%s", notification_id), Context.MODE_PRIVATE);

        SharedPreferences.Editor editor = prefs.edit();
        editor.clear();

        String data = UnityNotificationManager.SerializeNotificationIntent(intent);
        editor.putString("data", data);

        editor.commit();

        // Store IDs
        SharedPreferences idsPrefs = context.getSharedPreferences("UNITY_STORED_NOTIFICATION_IDS", Context.MODE_PRIVATE);
        Set<String> idsSet = idsPrefs.getStringSet(SHARED_PREFS_NOTIFICATION_IDS, new HashSet<String>());

        Set<String> idsSetCopy = new HashSet<String>(idsSet);
        idsSetCopy.add(notification_id);

        SharedPreferences.Editor idsEditor = idsPrefs.edit();
        idsEditor.clear();
        idsEditor.putStringSet(SHARED_PREFS_NOTIFICATION_IDS, idsSetCopy);
        idsEditor.commit();

        UnityNotificationManager.LoadNotificationIntents(context);

    }
    public static void deleteExpiredNotificationIntent(int id, Context context)
    {
        String id_str = Integer.toString(id);
        UnityNotificationManager.deleteExpiredNotificationIntent(id_str, context);
    }

    public static void deleteExpiredNotificationIntent(String id, Context context)
    {
        SharedPreferences idsPrefs = context.getSharedPreferences("UNITY_STORED_NOTIFICATION_IDS", Context.MODE_PRIVATE);
        Set<String> idsSet = idsPrefs.getStringSet(SHARED_PREFS_NOTIFICATION_IDS, new HashSet<String>());

        if (BuildConfig.DEBUG) {
            Log.w("UnityNotifications", String.format("\n Deleting expired notification intent : %s ", id));
        }

        cancelPendingNotificationIntentInternal(Integer.valueOf(id), context);

        Set<String> idsSetCopy = new HashSet<String>(idsSet);
        idsSetCopy.remove(id);

        SharedPreferences.Editor editor = idsPrefs.edit();
        editor.putStringSet(SHARED_PREFS_NOTIFICATION_IDS, idsSetCopy);
        editor.commit();

        SharedPreferences notificationPrefs =
                context.getSharedPreferences(String.format("u_notification_data_%s", id), Context.MODE_PRIVATE);
        notificationPrefs.edit().clear().commit();

    }

    public static List<Intent> LoadNotificationIntents(Context context)
    {
        SharedPreferences idsPrefs = context.getSharedPreferences("UNITY_STORED_NOTIFICATION_IDS", Context.MODE_PRIVATE);
        Set<String> idsSet = idsPrefs.getStringSet(SHARED_PREFS_NOTIFICATION_IDS, new HashSet<String>());
        Set<String> idsSetCopy = new HashSet<String>(idsSet);

        List<Intent> intent_data_list = new ArrayList<Intent> ();

        if (BuildConfig.DEBUG) {
            Log.w("UnityNotifications", String.format(" \n Loading serialized notification intents. Total Intents : %d \n", idsSetCopy.size()));
        }

        Set<String> idsMarkedForRemoval = new HashSet<String>();

        for (String id : idsSetCopy) {
            SharedPreferences notificationPrefs =
                    context.getSharedPreferences(String.format("u_notification_data_%s", id), Context.MODE_PRIVATE);
            String serializedIntentData = notificationPrefs.getString("data","");

            if (serializedIntentData.length() > 1)
            {
                Intent intent = UnityNotificationManager.DeserializeNotificationIntent(serializedIntentData, context);
                intent_data_list.add(intent);
            }
            else
            {
                idsMarkedForRemoval.add(id);
            }

        }

        for (String id : idsMarkedForRemoval) {
            UnityNotificationManager.deleteExpiredNotificationIntent(id, context);
        }

        return intent_data_list;
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

        try {

            ApplicationInfo ai = activity.getPackageManager().getApplicationInfo(activity.getPackageName(), PackageManager.GET_META_DATA);
            Bundle bundle = ai.metaData;

            Boolean reschedule_on_restart = bundle.getBoolean("reschedule_notifications_on_restart");

            if (reschedule_on_restart)
            {
                ComponentName receiver = new ComponentName(context, UnityNotificationRestartOnBootReceiver.class);
                PackageManager pm = context.getPackageManager();

                pm.setComponentEnabledSetting(receiver,
                        PackageManager.COMPONENT_ENABLED_STATE_ENABLED,
                        PackageManager.DONT_KILL_APP);
            }

            this.reschedule_on_restart = reschedule_on_restart;

            mOpenActivity = GetOpenAppActivity(context, false);
            if (mOpenActivity == null)
                mOpenActivity = activity.getClass();


        } catch (PackageManager.NameNotFoundException e) {
            Log.e("UnityNotifications", "Failed to load meta-data, NameNotFound: " + e.getMessage());
        } catch (NullPointerException e) {
            Log.e("UnityNotifications", "Failed to load meta-data, NullPointer: " + e.getMessage());
        }

    }

    public static Class<?> GetOpenAppActivity(Context context, Boolean fallbackToDefault)
    {
        ApplicationInfo ai = null;
        try {
            ai = context.getPackageManager().getApplicationInfo(context.getPackageName(), PackageManager.GET_META_DATA);
        } catch (PackageManager.NameNotFoundException e) {
            e.printStackTrace();
        }
        Bundle bundle = ai.metaData;

        String customActivityClassName = null;
        Class activityClass = null;

        if (bundle.containsKey("custom_notification_android_activity")) {
            customActivityClassName = bundle.getString("custom_notification_android_activity");

            try {
                activityClass = Class.forName(customActivityClassName);
            } catch (ClassNotFoundException ignored) {
                ;
            }
        }

        if (activityClass == null && fallbackToDefault)
        {
            try {
                return Class.forName("com.unity3d.player.UnityPlayerActivity");
            } catch (ClassNotFoundException ignored) {
                ;
            }
        }

        return activityClass;
    }

    public NotificationManager getNotificationManager()
    {
        return getNotificationManager(mContext);
    }

    public void setNotificationCallback(NotificationCallback notificationCallback)
    {
        UnityNotificationManager.mNotificationCallback = notificationCallback;
    }


    public static Intent prepareNotificationIntent(Intent intent, Context context, PendingIntent pendingIntent)
    {

        Intent data_intent = (Intent)intent.clone();
        int id = data_intent.getIntExtra("id", 0);

        SharedPreferences prefs = context.getSharedPreferences("UNITY_NOTIFICATIONS", Context.MODE_PRIVATE);
        Set<String> idsSet = prefs.getStringSet(SHARED_PREFS_NOTIFICATION_IDS, new HashSet<String>());

        Set<String> idsSetCopy = new HashSet<String>(idsSet);
        Set<String> validIdsSet = new HashSet<String>();

        data_intent.putExtra("tapIntent", pendingIntent);
        for(String sId : idsSetCopy )
        {
            PendingIntent broadcast = PendingIntent.getBroadcast(context, Integer.valueOf(sId), intent, PendingIntent.FLAG_NO_CREATE);

            if (broadcast != null) {
                validIdsSet.add(sId);
            }
        }

        if (BuildConfig.DEBUG) {
            Log.w("UnityNotifications", "Currently scheduled : " + Integer.toString(validIdsSet.size()));
        }

        if (android.os.Build.MANUFACTURER.equals("samsung") && validIdsSet.size() >= 499)
        {
            // There seems to be a limit of 500 concurrently scheduled alarms on Samsung devices.
            // Attempting to schedule more than that might cause the app to crash.
            Log.w("UnityNotifications", "Attempting to schedule more than 500 notifications. There is a limit of 500 concurrently scheduled Alarms on Samsung devices" +
                    " either wait for the currently scheduled ones to be triggered or cancel them if you wish to schedule additional notifications.");
            data_intent = null;

        }
        else {
            validIdsSet.add(Integer.toString(id));
            data_intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);

        }

        SharedPreferences.Editor editor = prefs.edit();
        editor.clear();
        editor.putStringSet(SHARED_PREFS_NOTIFICATION_IDS, validIdsSet);
        editor.apply();

        return data_intent;

    }


    public void scheduleNotificationIntent(Intent data_intent_source)//int id, String channelID, String textTitle, String textContent, String smallIcon, boolean autoCancel, String category, int visibility, long[] vibrationPattern, boolean usesChronometer, Date originalTime, Date fireTime, long repeatInterval)
    {

        Instant starts = null;
        if (BuildConfig.DEBUG) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                starts = Instant.now();
            }
        }

        String d = UnityNotificationManager.SerializeNotificationIntent(data_intent_source);
        Intent data_intent = UnityNotificationManager.DeserializeNotificationIntent(d, mContext);

        int id = data_intent.getIntExtra("id", 0);

        Intent openAppIntent = UnityNotificationManager.buildOpenAppIntent(data_intent, mContext, mOpenActivity);
        PendingIntent pendingIntent = PendingIntent.getActivity(mContext, id, openAppIntent, 0);
        Intent intent = prepareNotificationIntent(data_intent, mContext, pendingIntent);

        if (intent != null) {

            if (this.reschedule_on_restart) {
                UnityNotificationManager.SaveNotificationIntent(data_intent, mContext);
            }

            PendingIntent broadcast = PendingIntent.getBroadcast(mContext, id, intent, PendingIntent.FLAG_UPDATE_CURRENT);
            UnityNotificationManager.scheduleNotificationIntentAlarm(intent, mContext, broadcast);
        }

        if (BuildConfig.DEBUG) {
            if (starts != null) {
                Instant ends = null;
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                    ends = Instant.now();
                    Log.w("UnityNotifications", Long.toString(Duration.between(starts, ends).toMillis()));
                }
            }
        }
    }

    public static Intent buildOpenAppIntent(Intent data_intent, Context context, Class c)
    {
        Intent openAppIntent = new Intent(context, c);
        openAppIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        openAppIntent.putExtras(data_intent);

        return openAppIntent;
    }

    public static void scheduleNotificationIntentAlarm(Intent intent, Context context, PendingIntent broadcast)
    {
        long repeatInterval = intent.getLongExtra("repeatInterval", 0L);
        long fireTime = intent.getLongExtra("fireTime", 0L);
        int id = intent.getIntExtra("id", 0);

        AlarmManager alarmManager = (AlarmManager)context.getSystemService(Context.ALARM_SERVICE);

        Date fireTimeDt = new Date(fireTime);
        Date currentDt = Calendar.getInstance().getTime();

        if (repeatInterval <= 0)
        {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M)
            {
                alarmManager.setExactAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, fireTime, broadcast);
            }
            else
            {
                alarmManager.set(AlarmManager.RTC_WAKEUP, fireTime, broadcast);
            }
        }
        else
        {
            alarmManager.setInexactRepeating(AlarmManager.RTC_WAKEUP, fireTime, repeatInterval, broadcast);
        }
    }

    protected static Notification.Builder buildNotification(Intent intent, Context context)
    {
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
            if (color != 0)
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

        return notificationBuilder;
    }

    public static void sendNotification(Intent intent, Context context) {

        Notification.Builder notificationBuilder = UnityNotificationManager.buildNotification(intent, context);
        int id = intent.getIntExtra("id", -1);

        UnityNotificationManager.notify(context, id, notificationBuilder, intent);
    }

    protected static void notify(Context context, int id, Notification.Builder notificationBuilder, Intent intent)
    {
        getNotificationManager(context).notify(id, notificationBuilder.build());

        try {
            mNotificationCallback.onSentNotification(intent);
        }
        catch (RuntimeException ex)
        {
            Log.w("UnityNotifications", "Can not invoke OnNotificationReceived event when the app is not running!");
        }

        boolean isRepeatable = intent.getLongExtra("repeatInterval", 0L) > 0;

        if (!isRepeatable)
            UnityNotificationManager.deleteExpiredNotificationIntent(id, context);
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
        editor.clear();
        editor.putStringSet("ChannelIDs", channelIdsSet);
        editor.commit();

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

        editor.commit();
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
                editor.clear();
                editor.putStringSet("ChannelIDs", channelIdsSet);
                editor.commit();

                SharedPreferences channelPrefs = mContext.getSharedPreferences(String.format("unity_notification_channel_%s", id), Context.MODE_PRIVATE);
                editor = channelPrefs.edit();
                editor.clear();
                editor.commit();
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

        for (int i = 0; i < idsArrStr.length; i++)
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

    public static Intent CreateNotificationIntent(Activity activity)
    {
        return new Intent(activity, UnityNotificationManager.class);
    }

    private PendingIntent retrieveScheduledNotification(int requestCode)
    {
        Intent intent = CreateNotificationIntent(mActivity);
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
        Intent intent = CreateNotificationIntent(mActivity);
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

    private static void cancelPendingNotificationIntentInternal(int requestCode, Context context)
    {
        Intent intent = new Intent(context, UnityNotificationManager.class);
        PendingIntent broadcast = PendingIntent.getBroadcast(context, requestCode, intent, PendingIntent.FLAG_NO_CREATE);

        if (broadcast != null) {
            if (context != null) {
                AlarmManager alarmManager = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
                alarmManager.cancel(broadcast);
            }
            broadcast.cancel();
        }

        SharedPreferences prefs = context.getSharedPreferences("UNITY_NOTIFICATIONS", Context.MODE_PRIVATE);
        Set<String> idsSet = prefs.getStringSet(SHARED_PREFS_NOTIFICATION_IDS, new HashSet<String>());
        Set<String> idsSetCopy = new HashSet<String>(idsSet);

        String requestCodeStr = Integer.toString(requestCode);
        if (idsSetCopy.contains(requestCodeStr)) {
            idsSetCopy.remove(Integer.toString(requestCode));

            SharedPreferences.Editor editor = prefs.edit();
            editor.clear();
            editor.putStringSet(SHARED_PREFS_NOTIFICATION_IDS, idsSetCopy);
            editor.commit();
        }
    }

    public void cancelPendingNotificationIntent(int requestCode)
    {
        UnityNotificationManager.cancelPendingNotificationIntentInternal(requestCode, mContext);
        if (this.reschedule_on_restart)
        {
            UnityNotificationManager.deleteExpiredNotificationIntent(requestCode, mContext);
        }
    }

    public void cancelAllNotifications()
    {
        getNotificationManager().cancelAll();
    }

    @Override
    public void onReceive(Context context, Intent intent)
    {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N)
        {
            UnityNotificationManagerNougat.sendNotificationNougat(intent, context);
        }
        else {
            UnityNotificationManager.sendNotification(intent, context);
        }
    }

}
