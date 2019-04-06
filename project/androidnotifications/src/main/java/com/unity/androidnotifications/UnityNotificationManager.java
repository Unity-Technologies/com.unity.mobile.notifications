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
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.content.res.Resources;
import android.graphics.BitmapFactory;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.Debug;
import android.os.Parcel;
import android.service.notification.StatusBarNotification;
import android.util.Base64;
import android.util.Log;
import android.content.SharedPreferences;

import static android.app.Notification.DEFAULT_VIBRATE;
import static android.app.Notification.PRIORITY_DEFAULT;
import static android.app.Notification.VISIBILITY_PUBLIC;

import java.text.DateFormat;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Calendar;
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

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
        {
            return new UnityNotificationManagerOreo(context, (Activity) context);

        }
        else if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N)
        {
            return new UnityNotificationManagerNougat(context, (Activity) context);

        }

        return new UnityNotificationManager(context, (Activity) context);

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

        idsSet.add(notification_id);
//        Log.w("UnityNotifications", String.format("  - - - - NEW SaveNotificationIntent : %s", notification_id));


        SharedPreferences.Editor idsEditor = idsPrefs.edit();
        idsEditor.clear();
        idsEditor.putStringSet(SHARED_PREFS_NOTIFICATION_IDS, idsSet);
        idsEditor.commit();

//        for (String str : idsSet) {
//            Log.w("UnityNotifications", String.format("  - - - Found Notification Intents : %s", str));
//        }

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

        Log.w("UnityNotifications", String.format("\n Deleting expired notification intent : %s ", id ));

        idsSet.remove(id);

        SharedPreferences.Editor editor = idsPrefs.edit();
        editor.putStringSet(SHARED_PREFS_NOTIFICATION_IDS, idsSet);
        editor.commit();

        SharedPreferences notificationPrefs =
                context.getSharedPreferences(String.format("u_notification_data_%s", id), Context.MODE_PRIVATE);
        notificationPrefs.edit().clear().commit();

        // - - -


//        Log.w("UnityNotifications", String.format("\n Deleting expired notification intent : %s ", id ));
    }

    public static List<Intent> LoadNotificationIntents(Context context)
    {
        SharedPreferences idsPrefs = context.getSharedPreferences("UNITY_STORED_NOTIFICATION_IDS", Context.MODE_PRIVATE);
        Set<String> idsSet = idsPrefs.getStringSet(SHARED_PREFS_NOTIFICATION_IDS, new HashSet<String>());

        List<Intent> intent_data_list = new ArrayList<Intent> ();

         //
//         Log.w("UnityNotifications", String.format(" \n LoadNotificationIntents -- - Total Intents : %d \n", idsSet.size()));
        //



        Set<String> idsMarkedForRemoval = new HashSet<String>();

        for (String id : idsSet) {

//            Log.w("UnityNotifications", String.format(" --- Available Intent : %s", id));

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

//        Log.w("UnityNotifications", String.format(" \n -- - Loaded Intent Count : %d \n", intent_data_list.size()));

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

//            Log.w("UnityNotifications", String.format("Reschedule notifications on restart : %b", reschedule_on_restart));

        } catch (PackageManager.NameNotFoundException e) {
            Log.e("UnityNotifications", "Failed to load meta-data, NameNotFound: " + e.getMessage());
        } catch (NullPointerException e) {
            Log.e("UnityNotifications", "Failed to load meta-data, NullPointer: " + e.getMessage());
        }

    }

    public NotificationManager getNotificationManager()
    {
        return getNotificationManager(mContext);
    }

    public void setNotificationCallback(NotificationCallback notificationCallback)
    {
        UnityNotificationManager.mNotificationCallback = notificationCallback;
    }

    public void scheduleNotificationIntent(Intent data_intent_source)//int id, String channelID, String textTitle, String textContent, String smallIcon, boolean autoCancel, String category, int visibility, long[] vibrationPattern, boolean usesChronometer, Date originalTime, Date fireTime, long repeatInterval)
    {
        String d = UnityNotificationManager.SerializeNotificationIntent(data_intent_source);
        Intent data_intent = UnityNotificationManager.DeserializeNotificationIntent(d, mContext);

        if (this.reschedule_on_restart) {
            UnityNotificationManager.SaveNotificationIntent(data_intent, mContext);
        }

        int id = data_intent.getIntExtra("id", 0);

        Intent openAppIntent = UnityNotificationManager.buildOpenAppIntent(data_intent, mContext, UnityNotificationManager.GetUnityActivity());

        // - - - - - -
        PendingIntent pendingIntent = PendingIntent.getActivity(mContext, id, openAppIntent, 0);
        Intent intent = UnityNotificationManager.prepareNotificationIntent(data_intent, mContext, pendingIntent);

        // - - - - - -
        PendingIntent broadcast = PendingIntent.getBroadcast(mContext, id, intent, PendingIntent.FLAG_UPDATE_CURRENT);
        UnityNotificationManager.scheduleNotificationIntentAlarm(intent, mContext, broadcast);
    }

    public static Class<?> GetUnityActivity()
    {
        String className = "com.unity3d.player.UnityPlayerActivity";
        try {
            return Class.forName(className);
        } catch (ClassNotFoundException ignored) {
            return null;

        }

    }

    public static Intent buildOpenAppIntent(Intent data_intent, Context context, Class c)
    {
        Intent openAppIntent = new Intent(context, c);
        openAppIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_SINGLE_TOP);

        openAppIntent.setData(Uri.parse("http://www.google.com"));
        openAppIntent.putExtra("data", data_intent.getStringExtra("data"));

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

//        Log.w("UnityNotifications", String.format("\n Scheduled notification intent at : %s \n current time : %s \n",
//                fireTimeDt.toString(),
//                currentDt
//                )
//        );

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

    public static Intent prepareNotificationIntent(Intent intent, Context context, PendingIntent pendingIntent)
    {
        int id = intent.getIntExtra("id", 0);

        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);

        intent.putExtra("tapIntent", pendingIntent);

        SharedPreferences prefs = context.getSharedPreferences("UNITY_NOTIFICATIONS", Context.MODE_PRIVATE);
        Set<String> idsSet = prefs.getStringSet(SHARED_PREFS_NOTIFICATION_IDS, new HashSet<String>());

        //
//        Log.w("UnityNotifications", String.format(" \n prepareNotificationIntent -- - Total Intents :\n"));
        //
        idsSet.add(Integer.toString(id));

        SharedPreferences.Editor editor = prefs.edit();
        editor.clear();
        editor.putStringSet(SHARED_PREFS_NOTIFICATION_IDS, idsSet);
        editor.commit();

        return intent;
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


//        Log.w("UnityNotifications", String.format(" --  - Alarm: sendNotification : %d", id));


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
            Log.w("UnityNotifications", "Can not invoke OnNotificationReceived event when app is not running!");
        }

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

//        Log.w("UnityNotifications", String.format(" \n getScheduledNotificationIDs -- - getScheduledNotificationIDs :\n"));

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
        if (broadcast != null) {
            broadcast.cancel();
        }

        SharedPreferences prefs = mContext.getSharedPreferences("UNITY_NOTIFICATIONS", Context.MODE_PRIVATE);
        Set<String> idsSet = prefs.getStringSet(SHARED_PREFS_NOTIFICATION_IDS, new HashSet<String>());

//        Log.w("UnityNotifications", String.format(" \n cancelPendingNotificationIntent -- - cancelPendingNotificationIntent :\n"));

        String requestCodeStr = Integer.toString(requestCode);
        if (idsSet.contains(requestCodeStr)) {
            idsSet.remove(Integer.toString(requestCode));

            SharedPreferences.Editor editor = prefs.edit();
            editor.clear();
            editor.putStringSet(SHARED_PREFS_NOTIFICATION_IDS, idsSet);
            editor.commit();
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
