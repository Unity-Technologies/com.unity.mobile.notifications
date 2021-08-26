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
import java.util.Calendar;
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
    public void scheduleNotification(Notification.Builder notificationBuilder) {
        Bundle extras = notificationBuilder.getExtras();
        int id = extras.getInt("id", -1);
        long repeatInterval = extras.getLong("repeatInterval", -1);
        long fireTime = extras.getLong("fireTime", -1);
        Notification notification = notificationBuilder.build();

        Intent openAppIntent = UnityNotificationManager.buildOpenAppIntent(mContext, mOpenActivity);
        openAppIntent.putExtra("unityNotification", notification);
        PendingIntent pendingIntent = getActivityPendingIntent(mContext, id, openAppIntent, 0);

        // if less than a second in the future, notify right away
        if (fireTime - Calendar.getInstance().getTime().getTime() < 1000) {
            notificationBuilder.setContentIntent(pendingIntent);
            finalizeNotificationForDisplay(mContext, notificationBuilder);
            notification = notificationBuilder.build();
            notify(mContext, id, notification);
            if (repeatInterval <= 0)
                return;
            // schedule at next repetition
            fireTime += repeatInterval;
        }

        Intent intent = buildNotificationIntent(mContext, id);

        if (intent != null) {
            if (this.mRescheduleOnRestart) {
                intent.putExtra("unityNotification", notification);
                UnityNotificationManager.saveNotificationIntent(mContext, intent);
            }

            // content intent can't and shouldn't be saved, set it now and rebuild
            notificationBuilder.setContentIntent(pendingIntent);
            finalizeNotificationForDisplay(mContext, notificationBuilder);
            notification = notificationBuilder.build();
            intent.putExtra("unityNotification", notification);

            PendingIntent broadcast = getBroadcastPendingIntent(mContext, id, intent, PendingIntent.FLAG_UPDATE_CURRENT);
            UnityNotificationManager.scheduleNotificationIntentAlarm(mContext, repeatInterval, fireTime, broadcast);
        }
    }

    // Build an Intent to open the given activity with the data from input Intent.
    protected static Intent buildOpenAppIntent(Context context, Class className) {
        Intent openAppIntent = new Intent(context, className);
        openAppIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_SINGLE_TOP);

        return openAppIntent;
    }

    // Build a notification Intent to store the PendingIntent.
    protected static synchronized Intent buildNotificationIntent(Context context, int notificationId) {
        Intent intent = new Intent(context, UnityNotificationManager.class);

        SharedPreferences prefs = context.getSharedPreferences(NOTIFICATION_IDS_SHARED_PREFS, Context.MODE_PRIVATE);
        Set<String> ids = new HashSet<String>(prefs.getStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, new HashSet<String>()));

        Set<String> validNotificationIds = new HashSet<String>();
        for (String id : ids) {
            // Get the given broadcast PendingIntent by id as request code.
            // FLAG_NO_CREATE is set to return null if the described PendingIntent doesn't exist.
            PendingIntent broadcast = getBroadcastPendingIntent(context, Integer.valueOf(id), intent, PendingIntent.FLAG_NO_CREATE);

            if (broadcast != null) {
                validNotificationIds.add(id);
            }
        }

        if (android.os.Build.MANUFACTURER.equals("samsung") && validNotificationIds.size() >= 499) {
            // There seems to be a limit of 500 concurrently scheduled alarms on Samsung devices.
            // Attempting to schedule more than that might cause the app to crash.
            Log.w("UnityNotifications", "Attempting to schedule more than 500 notifications. There is a limit of 500 concurrently scheduled Alarms on Samsung devices" +
                    " either wait for the currently scheduled ones to be triggered or cancel them if you wish to schedule additional notifications.");
            intent = null;
        } else {
            validNotificationIds.add(Integer.toString(notificationId));
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        }

        SharedPreferences.Editor editor = prefs.edit().clear();
        editor.putStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, validNotificationIds);
        editor.apply();

        return intent;
    }

    public static PendingIntent getActivityPendingIntent(Context context, int id, Intent intent, int flags) {
        if (Build.VERSION.SDK_INT >= 23)
            return PendingIntent.getActivity(context, id, intent, flags | PendingIntent.FLAG_IMMUTABLE);
        else
            return PendingIntent.getActivity(context, id, intent, flags);
    }

    public static PendingIntent getBroadcastPendingIntent(Context context, int id, Intent intent, int flags) {
        if (Build.VERSION.SDK_INT >= 23)
            return PendingIntent.getBroadcast(context, id, intent, flags | PendingIntent.FLAG_IMMUTABLE);
        else
            return PendingIntent.getBroadcast(context, id, intent, flags);
    }

    // Save the notification intent to SharedPreferences if reschedule_on_restart is true,
    // which will be consumed by UnityNotificationRestartOnBootReceiver for device reboot.
    protected static synchronized void saveNotificationIntent(Context context, Intent intent) {
        Notification notification = intent.getParcelableExtra("unityNotification");
        String notification_id = Integer.toString(notification.extras.getInt("id", -1));
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
    protected static synchronized List<Intent> loadNotificationIntents(Context context) {
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
    protected static void scheduleNotificationIntentAlarm(Context context, long repeatInterval, long fireTime, PendingIntent broadcast) {
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
        return (getBroadcastPendingIntent(mContext, id, intent, PendingIntent.FLAG_NO_CREATE) != null);
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
        synchronized (UnityNotificationManager.class) {
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
    }

    // Cancel a pending notification by id.
    public void cancelPendingNotificationIntent(int id) {
        synchronized (UnityNotificationManager.class) {
            UnityNotificationManager.cancelPendingNotificationIntent(mContext, id);
            if (this.mRescheduleOnRestart) {
                UnityNotificationManager.deleteExpiredNotificationIntent(mContext, Integer.toString(id));
            }
        }
    }

    // Cancel a pending notification by id.
    protected static synchronized void cancelPendingNotificationIntent(Context context, int id) {
        Intent intent = new Intent(context, UnityNotificationManager.class);
        PendingIntent broadcast = getBroadcastPendingIntent(context, id, intent, PendingIntent.FLAG_NO_CREATE);

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
    protected static synchronized void deleteExpiredNotificationIntent(Context context, String id) {
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
            if (!intent.hasExtra("unityNotification"))
                return;

            UnityNotificationManager.sendNotification(context, intent);
        } catch (BadParcelableException e) {
            Log.w("UnityNotifications", e.toString());
        }
    }

    // Send a notification.
    protected static void sendNotification(Context context, Intent intent) {
        Notification notification = intent.getParcelableExtra("unityNotification");
        int id = notification.extras.getInt("id", -1);

        UnityNotificationManager.notify(context, id, notification);
    }

    // Call the system notification service to notify the notification.
    protected static void notify(Context context, int id, Notification notification) {
        getNotificationManager(context).notify(id, notification);

        try {
            mNotificationCallback.onSentNotification(notification);
        } catch (RuntimeException ex) {
            Log.w("UnityNotifications", "Can not invoke OnNotificationReceived event when the app is not running!");
        }

        boolean isRepeatable = notification.extras.getLong("repeatInterval", 0L) > 0;

        if (!isRepeatable)
            UnityNotificationManager.deleteExpiredNotificationIntent(context, Integer.toString(id));
    }

    public static Integer getNotificationColor(Notification notification) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.LOLLIPOP)
            return null;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            if (!notification.extras.containsKey(Notification.EXTRA_COLORIZED))
                return null;
        }

        return notification.color;
    }

    public static int getNotificationGroupAlertBehavior(Notification notification) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
            return notification.getGroupAlertBehavior();
        return 0;
    }

    public static void finalizeNotificationForDisplay(Context context, Notification.Builder notificationBuilder) {
        String icon = notificationBuilder.getExtras().getString("smallIcon");
        int iconId = UnityNotificationUtilities.findResourceIdInContextByName(context, icon);
        if (iconId == 0) {
            iconId = context.getApplicationInfo().icon;
        }
        notificationBuilder.setSmallIcon(iconId);
        icon = notificationBuilder.getExtras().getString("largeIcon");
        iconId = UnityNotificationUtilities.findResourceIdInContextByName(context, icon);
        if (iconId != 0) {
            notificationBuilder.setLargeIcon(BitmapFactory.decodeResource(context.getResources(), iconId));
        }
    }

    @SuppressWarnings("deprecation")
    public Notification.Builder createNotificationBuilder(String channelID) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) {
            Notification.Builder notificationBuilder = new Notification.Builder(mContext);

            // For device below Android O, we use the values from NotificationChannelWrapper to set visibility, priority etc.
            NotificationChannelWrapper fakeNotificationChannel = getNotificationChannel(mContext, channelID);

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

            return notificationBuilder;
        } else {
            return new Notification.Builder(mContext, channelID);
        }
    }

    public static void setNotificationIcon(Notification.Builder notificationBuilder, String keyName, String icon) {
        if (icon == null || icon.length() == 0 && notificationBuilder.getExtras().getString(keyName) != null)
            notificationBuilder.getExtras().remove(keyName);
        else
            notificationBuilder.getExtras().putString(keyName, icon);
    }

    public static void setNotificationColor(Notification.Builder notificationBuilder, int color) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            if (color != 0) {
                notificationBuilder.setColor(color);
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                    notificationBuilder.setColorized(true);
                }
            }
        }
    }

    public static void setNotificationUsesChronometer(Notification.Builder notificationBuilder, boolean usesChrono) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP_MR1)
            notificationBuilder.setUsesChronometer(usesChrono);
    }

    public static void setNotificationGroupAlertBehavior(Notification.Builder notificationBuilder, int behavior) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
            notificationBuilder.setGroupAlertBehavior(behavior);
    }

    public static String getNotificationChannelId(Notification notification) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            return notification.getChannelId();
        }

        return null;
    }
}
