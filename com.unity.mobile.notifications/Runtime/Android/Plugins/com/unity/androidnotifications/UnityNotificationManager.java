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
import android.content.SharedPreferences;
import android.graphics.BitmapFactory;
import android.os.Build;
import android.os.Bundle;
import android.os.BadParcelableException;
import android.service.notification.StatusBarNotification;
import android.util.Log;

import static android.app.Notification.VISIBILITY_PUBLIC;

import java.lang.Integer;
import java.util.Set;
import java.util.HashSet;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public class UnityNotificationManager extends BroadcastReceiver {
    protected static NotificationCallback mNotificationCallback;
    protected static UnityNotificationManager mUnityNotificationManager;

    public Context mContext = null;
    protected Activity mActivity = null;
    protected Class mOpenActivity = null;
    protected boolean mRescheduleOnRestart = false;

    protected static final String NOTIFICATION_CHANNELS_SHARED_PREFS = "UNITY_NOTIFICATIONS";
    protected static final String NOTIFICATION_CHANNELS_SHARED_PREFS_KEY = "ChannelIDs";
    protected static final String NOTIFICATION_IDS_SHARED_PREFS = "UNITY_STORED_NOTIFICATION_IDS";
    protected static final String NOTIFICATION_IDS_SHARED_PREFS_KEY = "UNITY_NOTIFICATION_IDS";

    // Constructor with zero parameter is necessary for system to call onReceive() callback.
    public UnityNotificationManager() {
        super();
    }

    // Called from Unity managed code to do initialization.
    public UnityNotificationManager(Context context, Activity activity) {
        super();
        mContext = context;
        mActivity = activity;

        try {
            ApplicationInfo ai = activity.getPackageManager().getApplicationInfo(activity.getPackageName(), PackageManager.GET_META_DATA);
            Bundle bundle = ai.metaData;

            Boolean rescheduleOnRestart = bundle.getBoolean("reschedule_notifications_on_restart");

            if (rescheduleOnRestart) {
                ComponentName receiver = new ComponentName(context, UnityNotificationRestartOnBootReceiver.class);
                PackageManager pm = context.getPackageManager();

                pm.setComponentEnabledSetting(receiver,
                    PackageManager.COMPONENT_ENABLED_STATE_ENABLED,
                    PackageManager.DONT_KILL_APP);
            }

            this.mRescheduleOnRestart = rescheduleOnRestart;

            mOpenActivity = UnityNotificationUtilities.getOpenAppActivity(context, false);
            if (mOpenActivity == null)
                mOpenActivity = activity.getClass();
        } catch (PackageManager.NameNotFoundException e) {
            Log.e("UnityNotifications", "Failed to load meta-data, NameNotFound: " + e.getMessage());
        } catch (NullPointerException e) {
            Log.e("UnityNotifications", "Failed to load meta-data, NullPointer: " + e.getMessage());
        }
    }

    public static UnityNotificationManager getNotificationManagerImpl(Context context) {
        return getNotificationManagerImpl(context, (Activity) context);
    }

    // Called from managed code.
    public static UnityNotificationManager getNotificationManagerImpl(Context context, Activity activity) {
        if (mUnityNotificationManager != null)
            return mUnityNotificationManager;

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            mUnityNotificationManager = new UnityNotificationManagerOreo(context, activity);
        } else {
            mUnityNotificationManager = new UnityNotificationManager(context, activity);
        }

        return mUnityNotificationManager;
    }

    public NotificationManager getNotificationManager() {
        return getNotificationManager(mContext);
    }

    // Get system notification service.
    public static NotificationManager getNotificationManager(Context context) {
        return (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);
    }

    // Called from managed code.
    public void setNotificationCallback(NotificationCallback notificationCallback) {
        UnityNotificationManager.mNotificationCallback = notificationCallback;
    }

    // Register a new notification channel.
    // This function will only be called for devices which are low than Android O.
    public void registerNotificationChannel(
            String id,
            String name,
            int importance,
            String description,
            boolean enableLights,
            boolean enableVibration,
            boolean canBypassDnd,
            boolean canShowBadge,
            long[] vibrationPattern,
            int lockscreenVisibility) {
        SharedPreferences prefs = mContext.getSharedPreferences(NOTIFICATION_CHANNELS_SHARED_PREFS, Context.MODE_PRIVATE);
        Set<String> channelIds = new HashSet<String>(prefs.getStringSet(NOTIFICATION_CHANNELS_SHARED_PREFS_KEY, new HashSet<String>()));
        channelIds.add(id); // TODO: what if users create the channel again with the same id?

        // Add to notification channel ids SharedPreferences.
        SharedPreferences.Editor editor = prefs.edit().clear();
        editor.putStringSet("ChannelIDs", channelIds);
        editor.apply();

        // Store the channel into a SharedPreferences.
        SharedPreferences channelPrefs = mContext.getSharedPreferences(getSharedPrefsNameByChannelId(id), Context.MODE_PRIVATE);
        editor = channelPrefs.edit();

        editor.putString("title", name); // Sadly I can't change the "title" here to "name" due to backward compatibility.
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

    protected static String getSharedPrefsNameByChannelId(String id)
    {
        return String.format("unity_notification_channel_%s", id);
    }

    // Get a notification channel by id.
    // This function will only be called for devices which are low than Android O.
    protected static NotificationChannelWrapper getNotificationChannel(Context context, String id) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            return UnityNotificationManagerOreo.getOreoNotificationChannel(context, id);
        }

        SharedPreferences prefs = context.getSharedPreferences(getSharedPrefsNameByChannelId(id), Context.MODE_PRIVATE);
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

        if (vibrationPattern.length > 1) {
            for (int i = 0; i < vibrationPatternStr.length; i++) {
                try {
                    vibrationPattern[i] = Long.parseLong(vibrationPatternStr[i]);
                } catch (NumberFormatException e) {
                    vibrationPattern[i] = 1;
                }
            }
        }

        channel.vibrationPattern = vibrationPattern.length > 1 ? vibrationPattern : null;
        return channel;
    }

    // Get a notification channel by id.
    // This function will only be called for devices which are low than Android O.
    protected NotificationChannelWrapper getNotificationChannel(String id) {
        return UnityNotificationManager.getNotificationChannel(mContext, id);
    }

    // Delete a notification channel by id.
    // This function will only be called for devices which are low than Android O.
    public void deleteNotificationChannel(String id) {
        SharedPreferences prefs = mContext.getSharedPreferences(NOTIFICATION_CHANNELS_SHARED_PREFS, Context.MODE_PRIVATE);
        Set<String> channelIds = new HashSet<String>(prefs.getStringSet(NOTIFICATION_CHANNELS_SHARED_PREFS_KEY, new HashSet<String>()));

        if (!channelIds.contains(id))
            return;

        // Remove from the notification channel ids SharedPreferences.
        channelIds.remove(id);
        SharedPreferences.Editor editor = prefs.edit().clear();
        editor.putStringSet(NOTIFICATION_CHANNELS_SHARED_PREFS_KEY, channelIds);
        editor.apply();

        // Delete the notification channel SharedPreferences.
        SharedPreferences channelPrefs = mContext.getSharedPreferences(getSharedPrefsNameByChannelId(id), Context.MODE_PRIVATE);
        channelPrefs.edit().clear().apply();
    }

    // Get all notification channels.
    // This function will only be called for devices which are low than Android O.
    public Object[] getNotificationChannels() {
        SharedPreferences prefs = mContext.getSharedPreferences(NOTIFICATION_CHANNELS_SHARED_PREFS, Context.MODE_PRIVATE);
        Set<String> channelIdsSet = prefs.getStringSet(NOTIFICATION_CHANNELS_SHARED_PREFS_KEY, new HashSet<String>());

        ArrayList<NotificationChannelWrapper> channels = new ArrayList<>();

        for (String k : channelIdsSet) {
            channels.add(getNotificationChannel(k));
        }
        return channels.toArray();
    }

    // This is called from Unity managed code to call AlarmManager to set a broadcast intent for sending a notification.
    public void scheduleNotificationIntent(Intent data_intent_source) {
        // TODO: why we serialize/deserialize again?
        String temp = UnityNotificationUtilities.serializeNotificationIntent(data_intent_source);
        Intent data_intent = UnityNotificationUtilities.deserializeNotificationIntent(mContext, temp);

        int id = data_intent.getIntExtra("id", 0);

        Intent openAppIntent = UnityNotificationManager.buildOpenAppIntent(data_intent, mContext, mOpenActivity);
        PendingIntent pendingIntent = PendingIntent.getActivity(mContext, id, openAppIntent, 0);
        Intent intent = buildNotificationIntent(mContext, data_intent, pendingIntent);

        if (intent != null) {
            if (this.mRescheduleOnRestart) {
                UnityNotificationManager.saveNotificationIntent(mContext, data_intent);
            }

            PendingIntent broadcast = PendingIntent.getBroadcast(mContext, id, intent, PendingIntent.FLAG_UPDATE_CURRENT);
            UnityNotificationManager.scheduleNotificationIntentAlarm(mContext, intent, broadcast);
        }
    }

    // Build an Intent to open the given activity with the data from input Intent.
    protected static Intent buildOpenAppIntent(Intent data_intent, Context context, Class className) {
        Intent openAppIntent = new Intent(context, className);
        openAppIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        openAppIntent.putExtras(data_intent);

        return openAppIntent;
    }

    // Build a notification Intent to store the PendingIntent.
    protected static Intent buildNotificationIntent(Context context, Intent intent, PendingIntent pendingIntent) {
        Intent data_intent = (Intent) intent.clone();
        data_intent.putExtra("tapIntent", pendingIntent);

        SharedPreferences prefs = context.getSharedPreferences(NOTIFICATION_IDS_SHARED_PREFS, Context.MODE_PRIVATE);
        Set<String> ids = new HashSet<String>(prefs.getStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, new HashSet<String>()));

        Set<String> validNotificationIds = new HashSet<String>();
        for (String id : ids) {
            // Get the given broadcast PendingIntent by id as request code.
            // FLAG_NO_CREATE is set to return null if the described PendingIntent doesn't exist.
            PendingIntent broadcast = PendingIntent.getBroadcast(context, Integer.valueOf(id), intent, PendingIntent.FLAG_NO_CREATE);

            if (broadcast != null) {
                validNotificationIds.add(id);
            }
        }

        if (android.os.Build.MANUFACTURER.equals("samsung") && validNotificationIds.size() >= 499) {
            // There seems to be a limit of 500 concurrently scheduled alarms on Samsung devices.
            // Attempting to schedule more than that might cause the app to crash.
            Log.w("UnityNotifications", "Attempting to schedule more than 500 notifications. There is a limit of 500 concurrently scheduled Alarms on Samsung devices" +
                    " either wait for the currently scheduled ones to be triggered or cancel them if you wish to schedule additional notifications.");
            data_intent = null;
        } else {
            int id = data_intent.getIntExtra("id", 0);
            validNotificationIds.add(Integer.toString(id));
            data_intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        }

        SharedPreferences.Editor editor = prefs.edit().clear();
        editor.putStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, validNotificationIds);
        editor.apply();

        return data_intent;
    }

    // Save the notification intent to SharedPreferences if reschedule_on_restart is true,
    // which will be consumed by UnityNotificationRestartOnBootReceiver for device reboot.
    protected static void saveNotificationIntent(Context context, Intent intent) {
        String notification_id = Integer.toString(intent.getIntExtra("id", 0));
        SharedPreferences prefs = context.getSharedPreferences(getSharedPrefsNameByNotificationId(notification_id), Context.MODE_PRIVATE);

        SharedPreferences.Editor editor = prefs.edit().clear();
        String data = UnityNotificationUtilities.serializeNotificationIntent(intent);
        editor.putString("data", data);
        editor.apply();

        // Add the id to notification ids SharedPreferences.
        SharedPreferences idsPrefs = context.getSharedPreferences(NOTIFICATION_IDS_SHARED_PREFS, Context.MODE_PRIVATE);
        Set<String> ids = new HashSet<String>(idsPrefs.getStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, new HashSet<String>()));
        ids.add(notification_id);

        SharedPreferences.Editor idsEditor = idsPrefs.edit().clear();
        idsEditor.putStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, ids);
        idsEditor.apply();

        // TODO: why we load after saving?
        UnityNotificationManager.loadNotificationIntents(context);
    }

    protected static String getSharedPrefsNameByNotificationId(String id)
    {
        return String.format("u_notification_data_%s", id);
    }

    // Load all the notification intents from SharedPreferences.
    protected static List<Intent> loadNotificationIntents(Context context) {
        SharedPreferences idsPrefs = context.getSharedPreferences(NOTIFICATION_IDS_SHARED_PREFS, Context.MODE_PRIVATE);
        Set<String> ids = new HashSet<String>(idsPrefs.getStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, new HashSet<String>()));

        List<Intent> intent_data_list = new ArrayList<Intent>();
        Set<String> idsMarkedForRemoval = new HashSet<String>();

        for (String id : ids) {
            SharedPreferences prefs = context.getSharedPreferences(getSharedPrefsNameByNotificationId(id), Context.MODE_PRIVATE);
            String serializedIntentData = prefs.getString("data", "");

            if (serializedIntentData.length() > 1) {
                Intent intent = UnityNotificationUtilities.deserializeNotificationIntent(context, serializedIntentData);
                intent_data_list.add(intent);
            } else {
                idsMarkedForRemoval.add(id);
            }
        }

        for (String id : idsMarkedForRemoval) {
            UnityNotificationManager.deleteExpiredNotificationIntent(context, id);
        }

        return intent_data_list;
    }

    // Call AlarmManager to set the broadcast intent with fire time and interval.
    protected static void scheduleNotificationIntentAlarm(Context context, Intent intent, PendingIntent broadcast) {
        long repeatInterval = intent.getLongExtra("repeatInterval", 0L);
        long fireTime = intent.getLongExtra("fireTime", 0L);

        AlarmManager alarmManager = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);

        if (repeatInterval <= 0) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                alarmManager.setExactAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, fireTime, broadcast);
            } else {
                alarmManager.set(AlarmManager.RTC_WAKEUP, fireTime, broadcast);
            }
        } else {
            alarmManager.setInexactRepeating(AlarmManager.RTC_WAKEUP, fireTime, repeatInterval, broadcast);
        }
    }

    // Check the notification status by id.
    public int checkNotificationStatus(int id) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            // TODO: what if the notification has been dismissed by the user?
            for (StatusBarNotification n : getNotificationManager().getActiveNotifications()) {
                if (id == n.getId())
                    return 2;
            }

            if (checkIfPendingNotificationIsRegistered(id))
                return 1;

            return 0;
        }
        return -1;
    }

    // Check if the pending notification with the given id has been registered.
    public boolean checkIfPendingNotificationIsRegistered(int id) {
        Intent intent = new Intent(mActivity, UnityNotificationManager.class);
        return (PendingIntent.getBroadcast(mContext, id, intent, PendingIntent.FLAG_NO_CREATE) != null);
    }

    // Cancel all the pending notifications.
    public void cancelAllPendingNotificationIntents() {
        int[] ids = this.getScheduledNotificationIDs();

        for (int id : ids) {
            cancelPendingNotificationIntent(id);
        }
    }

    // Get all notification ids from SharedPreferences.
    protected int[] getScheduledNotificationIDs() {
        SharedPreferences prefs = mContext.getSharedPreferences(NOTIFICATION_IDS_SHARED_PREFS, Context.MODE_PRIVATE);
        Set<String> ids = prefs.getStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, new HashSet<String>());

        // Convert the string array ids to int array ids.
        int[] intIds = new int[ids.size()];
        int index = 0;
        for (String id : ids) {
            intIds[index++] = Integer.valueOf(id);
        }
        return intIds;
    }

    // Cancel a pending notification by id.
    public void cancelPendingNotificationIntent(int id) {
        UnityNotificationManager.cancelPendingNotificationIntent(mContext, id);
        if (this.mRescheduleOnRestart) {
            UnityNotificationManager.deleteExpiredNotificationIntent(mContext, Integer.toString(id));
        }
    }

    // Cancel a pending notification by id.
    protected static void cancelPendingNotificationIntent(Context context, int id) {
        Intent intent = new Intent(context, UnityNotificationManager.class);
        PendingIntent broadcast = PendingIntent.getBroadcast(context, id, intent, PendingIntent.FLAG_NO_CREATE);

        if (broadcast != null) {
            if (context != null) {
                AlarmManager alarmManager = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
                alarmManager.cancel(broadcast);
            }
            broadcast.cancel();
        }

        SharedPreferences prefs = context.getSharedPreferences(NOTIFICATION_IDS_SHARED_PREFS, Context.MODE_PRIVATE);
        Set<String> ids = new HashSet<String>(prefs.getStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, new HashSet<String>()));

        String idStr = Integer.toString(id);
        if (ids.contains(idStr)) {
            ids.remove(Integer.toString(id));

            SharedPreferences.Editor editor = prefs.edit().clear();
            editor.putStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, ids);
            editor.apply();
        }
    }

    // Delete the notification intent from SharedPreferences by id.
    protected static void deleteExpiredNotificationIntent(Context context, String id) {
        SharedPreferences idsPrefs = context.getSharedPreferences(NOTIFICATION_IDS_SHARED_PREFS, Context.MODE_PRIVATE);
        Set<String> ids = new HashSet<String>(idsPrefs.getStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, new HashSet<String>()));

        cancelPendingNotificationIntent(context, Integer.valueOf(id));

        ids.remove(id);
        SharedPreferences.Editor editor = idsPrefs.edit();
        editor.putStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, ids);
        editor.apply();

        SharedPreferences notificationPrefs = context.getSharedPreferences(getSharedPrefsNameByNotificationId(id), Context.MODE_PRIVATE);
        notificationPrefs.edit().clear().apply();
    }

    // Cancel a previously shown notification by id.
    public void cancelDisplayedNotification(int id) {
        getNotificationManager().cancel(id);
    }

    // Cancel all previously shown notifications.
    public void cancelAllNotifications() {
        getNotificationManager().cancelAll();
    }

    @Override
    public void onReceive(Context context, Intent intent) {
        try {
            if (!intent.hasExtra("channelID") || !intent.hasExtra("smallIconStr"))
                return;

            UnityNotificationManager.sendNotification(context, intent);
        } catch (BadParcelableException e) {
            Log.w("UnityNotifications", e.toString());
        }
    }

    // Send a notification.
    protected static void sendNotification(Context context, Intent intent) {
        Notification.Builder notificationBuilder = UnityNotificationManager.buildNotification(context, intent);
        int id = intent.getIntExtra("id", -1);

        UnityNotificationManager.notify(context, id, notificationBuilder.build(), intent);
    }

    // Create a Notification.Builder from the intent.
    @SuppressWarnings("deprecation")
    protected static Notification.Builder buildNotification(Context context, Intent intent) {
        String channelID = intent.getStringExtra("channelID");

        Notification.Builder notificationBuilder;
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) {
            notificationBuilder = new Notification.Builder(context);
        } else {
            notificationBuilder = new Notification.Builder(context, channelID);
        }

        String largeIconStr = intent.getStringExtra("largeIconStr");
        int largeIconId = UnityNotificationUtilities.findResourceIdInContextByName(context, largeIconStr);
        if (largeIconId != 0) {
            notificationBuilder.setLargeIcon(BitmapFactory.decodeResource(context.getResources(), largeIconId));
        }

        String smallIconStr = intent.getStringExtra("smallIconStr");
        int smallIconId = UnityNotificationUtilities.findResourceIdInContextByName(context, smallIconStr);
        if (smallIconId == 0) {
            smallIconId = context.getApplicationInfo().icon;
        }
        notificationBuilder.setSmallIcon(smallIconId);

        String textTitle = intent.getStringExtra("textTitle");
        String textContent = intent.getStringExtra("textContent");
        PendingIntent tapIntent = (PendingIntent) intent.getParcelableExtra("tapIntent");
        boolean autoCancel = intent.getBooleanExtra("autoCancel", true);

        notificationBuilder.setContentTitle(textTitle)
            .setContentText(textContent)
            .setContentIntent(tapIntent)
            .setAutoCancel(autoCancel);

        int number = intent.getIntExtra("number", 0);
        if (number >= 0)
            notificationBuilder.setNumber(number);

        int style = intent.getIntExtra("style", 0);
        if (style == 2)
            notificationBuilder.setStyle(new Notification.BigTextStyle().bigText(textContent));

        long timestampValue = intent.getLongExtra("timestamp", -1);
        notificationBuilder.setWhen(timestampValue);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT_WATCH) {
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
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.JELLY_BEAN_MR1) {
            boolean showTimestamp = intent.getBooleanExtra("showTimestamp", false);
            notificationBuilder.setShowWhen(showTimestamp);
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            int color = intent.getIntExtra("color", 0);
            if (color != 0) {
                notificationBuilder.setColor(color);
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                    notificationBuilder.setColorized(true);
                }
            }
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP_MR1) {
            boolean usesChronometer = intent.getBooleanExtra("usesChronometer", false);
            notificationBuilder.setUsesChronometer(usesChronometer);
        }

        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) {
            // For device below Android O, we use the values from NotificationChannelWrapper to set visibility, priority etc.
            NotificationChannelWrapper fakeNotificationChannel = getNotificationChannel(context, channelID);

            if (fakeNotificationChannel.vibrationPattern != null && fakeNotificationChannel.vibrationPattern.length > 0) {
                notificationBuilder.setDefaults(Notification.DEFAULT_LIGHTS | Notification.DEFAULT_SOUND);
                notificationBuilder.setVibrate(fakeNotificationChannel.vibrationPattern);
            } else {
                notificationBuilder.setDefaults(Notification.DEFAULT_ALL);
            }

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                notificationBuilder.setVisibility((int) fakeNotificationChannel.lockscreenVisibility);
            }

            // Need to convert Oreo channel importance to pre-Oreo priority.
            int priority;
            switch (fakeNotificationChannel.importance) {
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
        } else {
            // groupAlertBehaviour is only supported for Android O and above.
            int groupAlertBehaviour = intent.getIntExtra("groupAlertBehaviour", 0);
            notificationBuilder.setGroupAlertBehavior(groupAlertBehaviour);
        }

        return notificationBuilder;
    }

    // Call the system notification service to notify the notification.
    protected static void notify(Context context, int id, Notification notification, Intent intent) {
        getNotificationManager(context).notify(id, notification);

        try {
            mNotificationCallback.onSentNotification(intent);
        } catch (RuntimeException ex) {
            Log.w("UnityNotifications", "Can not invoke OnNotificationReceived event when the app is not running!");
        }

        boolean isRepeatable = intent.getLongExtra("repeatInterval", 0L) > 0;

        if (!isRepeatable)
            UnityNotificationManager.deleteExpiredNotificationIntent(context, Integer.toString(id));
    }
}
