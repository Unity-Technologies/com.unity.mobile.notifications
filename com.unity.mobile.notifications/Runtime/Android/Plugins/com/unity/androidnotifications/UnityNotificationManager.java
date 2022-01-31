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
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.BadParcelableException;
import android.provider.Settings;
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
    private static int mSentSinceLastHousekeeping = 0;
    private static boolean mPerformingHousekeeping = false;

    public Context mContext = null;
    protected Activity mActivity = null;
    protected Class mOpenActivity = null;
    protected boolean mRescheduleOnRestart = false;

    protected static final int SAMSUNG_NOTIFICATION_LIMIT = 500;
    protected static final String TAG_UNITY = "UnityNotifications";

    protected static final String KEY_FIRE_TIME = "fireTime";
    protected static final String KEY_ID = "id";
    protected static final String KEY_INTENT_DATA = "data";
    protected static final String KEY_LARGE_ICON = "largeIcon";
    protected static final String KEY_REPEAT_INTERVAL = "repeatInterval";
    protected static final String KEY_NOTIFICATION = "unityNotification";
    protected static final String KEY_SMALL_ICON = "smallIcon";
    protected static final String KEY_CHANNEL_ID = "channelID";

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
            Log.e(TAG_UNITY, "Failed to load meta-data, NameNotFound: " + e.getMessage());
        } catch (NullPointerException e) {
            Log.e(TAG_UNITY, "Failed to load meta-data, NullPointer: " + e.getMessage());
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
        int id = extras.getInt(KEY_ID, -1);
        long repeatInterval = extras.getLong(KEY_REPEAT_INTERVAL, -1);
        long fireTime = extras.getLong(KEY_FIRE_TIME, -1);
        Notification notification = notificationBuilder.build();

        Intent openAppIntent = UnityNotificationManager.buildOpenAppIntent(mContext, mOpenActivity);
        openAppIntent.putExtra(KEY_NOTIFICATION, notification);
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

        Intent intent = buildNotificationIntentUpdateList(mContext, id);

        if (intent != null) {
            if (this.mRescheduleOnRestart) {
                intent.putExtra(KEY_NOTIFICATION, notification);
                UnityNotificationManager.saveNotificationIntent(mContext, intent);
            }

            // content intent can't and shouldn't be saved, set it now and rebuild
            notificationBuilder.setContentIntent(pendingIntent);
            finalizeNotificationForDisplay(mContext, notificationBuilder);
            notification = notificationBuilder.build();
            intent.putExtra(KEY_NOTIFICATION, notification);

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
    private static synchronized Intent buildNotificationIntentUpdateList(Context context, int notificationId) {
        Set<String> ids = getScheduledNotificationIDs(context);
        if (android.os.Build.MANUFACTURER.equals("samsung") && ids.size() >= (SAMSUNG_NOTIFICATION_LIMIT - 1)) {
            // There seems to be a limit of 500 concurrently scheduled alarms on Samsung devices.
            // Attempting to schedule more than that might cause the app to crash.
            Log.w(TAG_UNITY, String.format("Attempting to schedule more than %1$d notifications. There is a limit of %1$d concurrently scheduled Alarms on Samsung devices" +
                    " either wait for the currently scheduled ones to be triggered or cancel them if you wish to schedule additional notifications.",
                    SAMSUNG_NOTIFICATION_LIMIT));
            return null;
        }

        Intent intent = buildNotificationIntent(context);
        ids = new HashSet<>(ids);
        ids.add(String.valueOf(notificationId));
        saveScheduledNotificationIDs(context, ids);
        scheduleHousekeeping(context, ids);
        return intent;
    }

    private static synchronized void scheduleHousekeeping(Context context, Set<String> ids) {
        ++mSentSinceLastHousekeeping;
        if (mSentSinceLastHousekeeping > 50) {
            mSentSinceLastHousekeeping = 0;
            Thread housekeepingThread = new Thread(() -> {
                try {
                    // when scheduling lots of notifications at once we can have more than one housekeeping thread running
                    // synchronize them and chain to happen one after the other
                    synchronized (UnityNotificationManager.class) {
                        while (mPerformingHousekeeping) {
                            UnityNotificationManager.class.wait();
                        }
                        mPerformingHousekeeping = true;
                    }

                    performNotificationHousekeeping(context, ids);
                } catch (InterruptedException e) {
                    Log.e(TAG_UNITY, "Notification housekeeping interrupted");
                } finally {
                    synchronized (UnityNotificationManager.class) {
                        mPerformingHousekeeping = false;
                        UnityNotificationManager.class.notify();
                    }
                }
            });
            housekeepingThread.start();
        }
    }

    private static void performNotificationHousekeeping(Context context, Set<String> ids) {
        Log.d(TAG_UNITY, "Checking for invalid notification IDs still hanging around");

        Set<String> invalid = findInvalidNotificationIds(context, ids);
        synchronized (UnityNotificationManager.class) {
            // list might have changed while we searched
            Set<String> currentIds = new HashSet<>(getScheduledNotificationIDs(context));
            for (String id : invalid)
                currentIds.remove(id);
            saveScheduledNotificationIDs(context, currentIds);
        }

        // in case we have saved intents, clear them
        for (String id : invalid)
            deleteExpiredNotificationIntent(context, id);
    }

    private static Set<String> findInvalidNotificationIds(Context context, Set<String> ids) {
        Intent intent = buildNotificationIntent(context);
        HashSet<String> invalid = new HashSet<String>();
        for (String id : ids) {
            // Get the given broadcast PendingIntent by id as request code.
            // FLAG_NO_CREATE is set to return null if the described PendingIntent doesn't exist.
            PendingIntent broadcast = getBroadcastPendingIntent(context, Integer.valueOf(id), intent, PendingIntent.FLAG_NO_CREATE);
            if (broadcast == null) {
                invalid.add(id);
            }
        }

        return invalid;
    }

    protected static Intent buildNotificationIntent(Context context) {
        Intent intent = new Intent(context, UnityNotificationManager.class);
        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
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
        Notification notification = intent.getParcelableExtra(KEY_NOTIFICATION);
        String notification_id = Integer.toString(notification.extras.getInt(KEY_ID, -1));
        SharedPreferences prefs = context.getSharedPreferences(getSharedPrefsNameByNotificationId(notification_id), Context.MODE_PRIVATE);

        UnityNotificationUtilities.serializeNotificationIntent(prefs, intent);
    }

    protected static String getSharedPrefsNameByNotificationId(String id)
    {
        return String.format("u_notification_data_%s", id);
    }

    // Load all the notification intents from SharedPreferences.
    protected static synchronized List<Intent> loadNotificationIntents(Context context) {
        Set<String> ids = getScheduledNotificationIDs(context);

        List<Intent> intent_data_list = new ArrayList<Intent>();
        Set<String> idsMarkedForRemoval = new HashSet<String>();

        for (String id : ids) {
            SharedPreferences prefs = context.getSharedPreferences(getSharedPrefsNameByNotificationId(id), Context.MODE_PRIVATE);
            Intent intent = UnityNotificationUtilities.deserializeNotificationIntent(context, prefs);

            if (intent != null)
                intent_data_list.add(intent);
            else
                idsMarkedForRemoval.add(id);
        }

        if (idsMarkedForRemoval.size() > 0) {
            ids = new HashSet<>(ids);
            for (String id : idsMarkedForRemoval) {
                ids.remove(id);
                deleteExpiredNotificationIntent(context, id);
            }
            saveScheduledNotificationIDs(context, ids);
        }

        return intent_data_list;
    }

    private static boolean canScheduleExactAlarms(AlarmManager alarmManager) {
        // The commented-out if below is the correct one and should replace the one further down
        // However it requires compile SDK 31 to compile, cutting edge and not shipped with Unity at the moment of writing this
        // It means exact timing for notifications is not supported on Android 12+ out of the box
        //if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S)
            //return alarmManager.canScheduleExactAlarms();
        if (Build.VERSION.SDK_INT >= 31)
            return false;
        return Build.VERSION.SDK_INT >= Build.VERSION_CODES.M;
    }

    // Call AlarmManager to set the broadcast intent with fire time and interval.
    protected static void scheduleNotificationIntentAlarm(Context context, long repeatInterval, long fireTime, PendingIntent broadcast) {
        AlarmManager alarmManager = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);

        if (repeatInterval <= 0) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M && canScheduleExactAlarms(alarmManager)) {
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
        Set<String> ids;
        synchronized (UnityNotificationManager.class) {
            ids = this.getScheduledNotificationIDs(mContext);
            saveScheduledNotificationIDs(mContext, new HashSet<>());
        }

        if (ids.size() > 0) {
            Context context = mContext;
            new Thread(() -> {
                for (String id : ids) {
                    cancelPendingNotificationIntent(context, Integer.valueOf(id));
                    if (this.mRescheduleOnRestart) {
                        deleteExpiredNotificationIntent(context, id);
                    }
                }
            }).start();
        }
    }

    private static synchronized Set<String> getScheduledNotificationIDs(Context context) {
        SharedPreferences prefs = context.getSharedPreferences(NOTIFICATION_IDS_SHARED_PREFS, Context.MODE_PRIVATE);
        Set<String> ids = prefs.getStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, new HashSet<String>());
        return ids;
    }

    private static synchronized void saveScheduledNotificationIDs(Context context, Set<String> ids) {
        SharedPreferences.Editor editor = context.getSharedPreferences(NOTIFICATION_IDS_SHARED_PREFS, Context.MODE_PRIVATE).edit().clear();
        editor.putStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, ids);
        editor.apply();
    }

    // Cancel a pending notification by id.
    public void cancelPendingNotification(int id) {
        synchronized (UnityNotificationManager.class) {
            UnityNotificationManager.cancelPendingNotificationIntent(mContext, id);
            String idStr = String.valueOf(id);
            removeScheduledNotificationID(mContext, idStr);
            if (this.mRescheduleOnRestart) {
                UnityNotificationManager.deleteExpiredNotificationIntent(mContext, idStr);
            }
        }
    }

    // Cancel a pending notification by id.
    protected static void cancelPendingNotificationIntent(Context context, int id) {
        Intent intent = new Intent(context, UnityNotificationManager.class);
        PendingIntent broadcast = getBroadcastPendingIntent(context, id, intent, PendingIntent.FLAG_NO_CREATE);

        if (broadcast != null) {
            if (context != null) {
                AlarmManager alarmManager = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
                alarmManager.cancel(broadcast);
            }
            broadcast.cancel();
        }
    }

    protected static synchronized void removeScheduledNotificationID(Context context, String id) {
        Set<String> ids = getScheduledNotificationIDs(context);
        if (ids.contains(id)) {
            ids = new HashSet<>(ids);
            ids.remove(id);
            saveScheduledNotificationIDs(context, ids);
        }
    }

    // Delete the notification intent from SharedPreferences by id.
    protected static synchronized void deleteExpiredNotificationIntent(Context context, String id) {
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
            if (!intent.hasExtra(KEY_NOTIFICATION))
                return;

            UnityNotificationManager.sendNotification(context, intent);
        } catch (BadParcelableException e) {
            Log.w(TAG_UNITY, e.toString());
        }
    }

    // Send a notification.
    protected static void sendNotification(Context context, Intent intent) {
        Notification notification = intent.getParcelableExtra(KEY_NOTIFICATION);
        int id = notification.extras.getInt(KEY_ID, -1);

        UnityNotificationManager.notify(context, id, notification);
    }

    // Call the system notification service to notify the notification.
    protected static void notify(Context context, int id, Notification notification) {
        getNotificationManager(context).notify(id, notification);

        try {
            mNotificationCallback.onSentNotification(notification);
        } catch (RuntimeException ex) {
            Log.w(TAG_UNITY, "Can not invoke OnNotificationReceived event when the app is not running!");
        }

        boolean isRepeatable = notification.extras.getLong(KEY_REPEAT_INTERVAL, 0L) > 0;

        if (!isRepeatable) {
            String idStr = String.valueOf(id);
            removeScheduledNotificationID(context, idStr);
            deleteExpiredNotificationIntent(context, idStr);
        }
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
        String icon = notificationBuilder.getExtras().getString(KEY_SMALL_ICON);
        int iconId = UnityNotificationUtilities.findResourceIdInContextByName(context, icon);
        if (iconId == 0) {
            iconId = context.getApplicationInfo().icon;
        }
        notificationBuilder.setSmallIcon(iconId);
        icon = notificationBuilder.getExtras().getString(KEY_LARGE_ICON);
        iconId = UnityNotificationUtilities.findResourceIdInContextByName(context, icon);
        if (iconId != 0) {
            notificationBuilder.setLargeIcon(BitmapFactory.decodeResource(context.getResources(), iconId));
        }
    }

    public Notification.Builder createNotificationBuilder(String channelID) {
        return createNotificationBuilder(mContext, channelID);
    }

    @SuppressWarnings("deprecation")
    protected static Notification.Builder createNotificationBuilder(Context context, String channelID) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) {
            Notification.Builder notificationBuilder = new Notification.Builder(context);

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
            notificationBuilder.getExtras().putString(KEY_CHANNEL_ID, channelID);

            return notificationBuilder;
        } else {
            return new Notification.Builder(context, channelID);
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

    public void showNotificationSettings(String channelId) {
        Intent settingsIntent;
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) {
            settingsIntent = new Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS);
            Uri uri = Uri.fromParts("package", mContext.getPackageName(), null);
            settingsIntent.setData(uri);
        } else {
            if (channelId != null && channelId.length() > 0) {
                settingsIntent = new Intent(Settings.ACTION_CHANNEL_NOTIFICATION_SETTINGS);
                settingsIntent.putExtra(Settings.EXTRA_CHANNEL_ID, channelId);
            } else {
                settingsIntent = new Intent(Settings.ACTION_APP_NOTIFICATION_SETTINGS);
            }

            settingsIntent.putExtra(Settings.EXTRA_APP_PACKAGE, mContext.getPackageName());
        }

        settingsIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        mActivity.startActivity(settingsIntent);
    }
}
