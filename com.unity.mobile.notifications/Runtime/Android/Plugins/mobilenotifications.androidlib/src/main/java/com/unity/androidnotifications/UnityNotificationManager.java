package com.unity.androidnotifications;

import android.Manifest;
import android.annotation.TargetApi;
import android.app.Activity;
import android.app.ActivityManager;
import android.app.AlarmManager;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationChannelGroup;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.content.SharedPreferences;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.drawable.Icon;
import android.net.Uri;
import android.os.AsyncTask;
import android.os.Build;
import android.os.Bundle;
import android.provider.Settings;
import android.service.notification.StatusBarNotification;
import android.util.Log;

import static android.app.ActivityManager.RunningAppProcessInfo.IMPORTANCE_FOREGROUND;
import static android.app.ActivityManager.RunningAppProcessInfo.IMPORTANCE_VISIBLE;
import static android.app.Notification.VISIBILITY_PUBLIC;

import java.io.InputStream;
import java.lang.Integer;
import java.util.Calendar;
import java.util.Random;
import java.util.Set;
import java.util.HashSet;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;

import com.unity3d.player.UnityPlayer;

public class UnityNotificationManager extends BroadcastReceiver {
    static UnityNotificationManager mUnityNotificationManager;

    private Context mContext = null;
    private Activity mActivity = null;
    private Class mOpenActivity = null;
    private UnityNotificationBackgroundThread mBackgroundThread;
    private Random mRandom;
    private HashSet<Integer> mVisibleNotifications;
    private ConcurrentHashMap<Integer, Notification.Builder> mScheduledNotifications;
    private NotificationCallback mNotificationCallback;
    private int mExactSchedulingSetting = -1;

    private static final int PERMISSION_STATUS_ALLOWED = 1;
    private static final int PERMISSION_STATUS_DENIED = 2;
    private static final int PERMISSION_STATUS_NOTIFICATIONS_BLOCKED_FOR_APP = 5;
    static final String TAG_UNITY = "UnityNotifications";

    public static final String KEY_FIRE_TIME = "fireTime";
    public static final String KEY_ID = "id";
    public static final String KEY_INTENT_DATA = "data";
    public static final String KEY_LARGE_ICON = "largeIcon";
    public static final String KEY_REPEAT_INTERVAL = "repeatInterval";
    public static final String KEY_NOTIFICATION = "unityNotification";
    public static final String KEY_NOTIFICATION_ID = "com.unity.NotificationID";
    public static final String KEY_SMALL_ICON = "smallIcon";
    public static final String KEY_CHANNEL_ID = "channelID";
    public static final String KEY_SHOW_IN_FOREGROUND = "com.unity.showInForeground";
    public static final String KEY_NOTIFICATION_DISMISSED = "com.unity.NotificationDismissed";
    public static final String KEY_BIG_LARGE_ICON = "com.unity.BigLargeIcon";
    public static final String KEY_BIG_PICTURE = "com.unity.BigPicture";
    public static final String KEY_BIG_CONTENT_TITLE = "com.unity.BigContentTytle";
    public static final String KEY_BIG_SUMMARY_TEXT = "com.unity.BigSummaryText";
    public static final String KEY_BIG_CONTENT_DESCRIPTION = "com.unity.BigContentDescription";
    public static final String KEY_BIG_SHOW_WHEN_COLLAPSED = "com.unity.BigShowWhenCollapsed";

    static final String NOTIFICATION_CHANNELS_SHARED_PREFS = "UNITY_NOTIFICATIONS";
    static final String NOTIFICATION_CHANNELS_SHARED_PREFS_KEY = "ChannelIDs";
    static final String NOTIFICATION_IDS_SHARED_PREFS = "UNITY_STORED_NOTIFICATION_IDS";
    static final String NOTIFICATION_IDS_SHARED_PREFS_KEY = "UNITY_NOTIFICATION_IDS";

    private void initialize(Activity activity, NotificationCallback notificationCallback) {
        // always assign these, as callback here is always new, activity and context might be
        mContext = activity.getApplicationContext();
        mActivity = activity;
        mNotificationCallback = notificationCallback;
        if (mScheduledNotifications == null)
            mScheduledNotifications = new ConcurrentHashMap();
        if (mBackgroundThread == null || !mBackgroundThread.isAlive())
            mBackgroundThread = new UnityNotificationBackgroundThread(this, mScheduledNotifications);
        if (mRandom == null)
            mRandom = new Random();
        if (mVisibleNotifications == null)
            mVisibleNotifications = new HashSet<>();

        Bundle metaData = getAppMetadata();

        Boolean rescheduleOnRestart = false;
        if (metaData != null)
            rescheduleOnRestart = metaData.getBoolean("reschedule_notifications_on_restart", false);

        if (rescheduleOnRestart) {
            ComponentName receiver = new ComponentName(mContext, UnityNotificationRestartReceiver.class);
            PackageManager pm = mContext.getPackageManager();

            pm.setComponentEnabledSetting(receiver,
                PackageManager.COMPONENT_ENABLED_STATE_ENABLED,
                PackageManager.DONT_KILL_APP);
        }

        mOpenActivity = UnityNotificationUtilities.getOpenAppActivity(mContext);
        if (mOpenActivity == null)
            throw new RuntimeException("Failed to determine Activity to be opened when tapping notification");
        if (!mBackgroundThread.isAlive())
            mBackgroundThread.start();
    }

    static synchronized UnityNotificationManager getNotificationManagerImpl(Context context) {
        if (mUnityNotificationManager == null) {
            mUnityNotificationManager = new UnityNotificationManager();
            mUnityNotificationManager.mVisibleNotifications = new HashSet<>();
            mUnityNotificationManager.mScheduledNotifications = new ConcurrentHashMap();
        }

        // always assign context, as it might change
        mUnityNotificationManager.mContext = context.getApplicationContext();
        return mUnityNotificationManager;
    }

    // Called from managed code.
    public static synchronized UnityNotificationManager getNotificationManagerImpl(Activity activity, NotificationCallback notificationCallback) {
        if (mUnityNotificationManager == null) {
            mUnityNotificationManager = new UnityNotificationManager();
        }

        mUnityNotificationManager.initialize(activity, notificationCallback);
        return mUnityNotificationManager;
    }

    private Bundle getAppMetadata() {
        try {
            return mContext.getPackageManager().getApplicationInfo(mContext.getPackageName(), PackageManager.GET_META_DATA).metaData;
        } catch (PackageManager.NameNotFoundException e) {
            return null;
        }
    }

    public NotificationManager getNotificationManager() {
        return (NotificationManager) mContext.getSystemService(Context.NOTIFICATION_SERVICE);
    }

    public int getTargetSdk() {
        return mContext.getApplicationInfo().targetSdkVersion;
    }

    @TargetApi(Build.VERSION_CODES.N)
    public int areNotificationsEnabled() {
        boolean permissionGranted = true;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU)
            permissionGranted = mContext.checkCallingOrSelfPermission(Manifest.permission.POST_NOTIFICATIONS) == PackageManager.PERMISSION_GRANTED;
        boolean notificationsEnabled = getNotificationManager().areNotificationsEnabled();
        if (permissionGranted)
            return notificationsEnabled ? PERMISSION_STATUS_ALLOWED : PERMISSION_STATUS_NOTIFICATIONS_BLOCKED_FOR_APP;
        return PERMISSION_STATUS_DENIED;
    }

    public void registerNotificationChannelGroup(String id, String name, String description) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannelGroup group = new NotificationChannelGroup(id, name);
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
                group.setDescription(description);
            }

            getNotificationManager().createNotificationChannelGroup(group);
        }
    }

    public void deleteNotificationChannelGroup(String id) {
        if (id == null)
            return;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            getNotificationManager().deleteNotificationChannelGroup(id);
        } else {
            for (NotificationChannelWrapper c : getNotificationChannels()) {
                if (id.equals(c.group))
                    deleteNotificationChannel(c.id);
            }
        }
    }

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
            int lockscreenVisibility,
            String group) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel channel = new NotificationChannel(id, name, importance);
            channel.setDescription(description);
            channel.enableLights(enableLights);
            channel.enableVibration(enableVibration);
            channel.setBypassDnd(canBypassDnd);
            channel.setShowBadge(canShowBadge);
            channel.setVibrationPattern(vibrationPattern);
            channel.setLockscreenVisibility(lockscreenVisibility);
            channel.setGroup(group);

            getNotificationManager().createNotificationChannel(channel);
        } else {
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
            editor.putString("group", group);

            editor.apply();
        }
    }

    private static String getSharedPrefsNameByChannelId(String id)
    {
        return String.format("unity_notification_channel_%s", id);
    }

    public NotificationChannelWrapper getNotificationChannel(String id) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel ch = getNotificationManagerImpl(mContext).getNotificationManager().getNotificationChannel(id);
            if (ch == null)
                return null;
            return notificationChannelToWrapper(ch);
        }

        SharedPreferences prefs = mContext.getSharedPreferences(getSharedPrefsNameByChannelId(id), Context.MODE_PRIVATE);
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
        channel.group = prefs.getString("group", null);
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

    @TargetApi(Build.VERSION_CODES.O)
    private static NotificationChannelWrapper notificationChannelToWrapper(Object chan) {
        // Possibly unavailable classes cannot be in API, breaks reflection code looping over when searching for method
        NotificationChannel channel = (NotificationChannel)chan;
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
        wrapper.group = channel.getGroup();

        return wrapper;
    }

    public void deleteNotificationChannel(String id) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            getNotificationManager().deleteNotificationChannel(id);
        } else {
            SharedPreferences prefs = mContext.getSharedPreferences(NOTIFICATION_CHANNELS_SHARED_PREFS, Context.MODE_PRIVATE);
            Set<String> channelIds = prefs.getStringSet(NOTIFICATION_CHANNELS_SHARED_PREFS_KEY, new HashSet());

            if (!channelIds.contains(id))
                return;

            // Remove from the notification channel ids SharedPreferences.
            channelIds = new HashSet(channelIds);
            channelIds.remove(id);
            SharedPreferences.Editor editor = prefs.edit().clear();
            editor.putStringSet(NOTIFICATION_CHANNELS_SHARED_PREFS_KEY, channelIds);
            editor.apply();

            // Delete the notification channel SharedPreferences.
            SharedPreferences channelPrefs = mContext.getSharedPreferences(getSharedPrefsNameByChannelId(id), Context.MODE_PRIVATE);
            channelPrefs.edit().clear().apply();
        }
    }

    public NotificationChannelWrapper[] getNotificationChannels() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            List<NotificationChannel> channels = getNotificationManager().getNotificationChannels();
            if (channels.size() == 0)
                return null;
            NotificationChannelWrapper[] channelList = new NotificationChannelWrapper[channels.size()];
            int i = 0;
            for (NotificationChannel ch : channels) {
                channelList[i++] = notificationChannelToWrapper(ch);
            }

            return channelList;
        } else {
            SharedPreferences prefs = mContext.getSharedPreferences(NOTIFICATION_CHANNELS_SHARED_PREFS, Context.MODE_PRIVATE);
            Set<String> channelIdsSet = prefs.getStringSet(NOTIFICATION_CHANNELS_SHARED_PREFS_KEY, new HashSet());
            if (channelIdsSet.size() == 0)
                return null;
            NotificationChannelWrapper[] channels = new NotificationChannelWrapper[channelIdsSet.size()];
            int i = 0;
            for (String k : channelIdsSet) {
                channels[i++] = getNotificationChannel(k);
            }
            return channels;
        }
    }

    private int generateUniqueId() {
        int id = 0;
        do {
            id += mRandom.nextInt(1000);
        } while (mScheduledNotifications.containsKey(Integer.valueOf(id)));

        return id;
    }

    public int scheduleNotification(Notification.Builder notificationBuilder, boolean customized) {
        Bundle extras = notificationBuilder.getExtras();
        int id;
        if (extras.containsKey(KEY_ID))
            id = notificationBuilder.getExtras().getInt(KEY_ID, -1);
        else {
            id = generateUniqueId();
            extras.putInt(KEY_ID, id);
        }

        boolean addedNew = mScheduledNotifications.putIfAbsent(id, notificationBuilder) == null;
        mBackgroundThread.enqueueNotification(id, notificationBuilder, customized, addedNew);
        return id;
    }

    void performNotificationScheduling(int id, Notification.Builder notificationBuilder, boolean customized) {
        Bundle extras = notificationBuilder.getExtras();
        long repeatInterval = extras.getLong(KEY_REPEAT_INTERVAL, -1);
        long fireTime = extras.getLong(KEY_FIRE_TIME, -1);

        // if less than a second in the future, notify right away
        boolean fireNow = fireTime - Calendar.getInstance().getTime().getTime() < 1000;
        if (!fireNow || repeatInterval > 0) {
            if (fireNow) {
                // schedule at next repetition
                fireTime += repeatInterval;
            }

            Intent intent = buildNotificationIntent();

            if (intent != null) {
                saveNotification(notificationBuilder.build(), customized);
                scheduleAlarmWithNotification(notificationBuilder, intent, fireTime);
            }
        }

        if (fireNow) {
            Notification notification = buildNotificationForSending(mOpenActivity, notificationBuilder);
            notify(id, notification);
        }
    }

    void scheduleAlarmWithNotification(Notification.Builder notificationBuilder, Intent intent, long fireTime) {
        Bundle extras = notificationBuilder.getExtras();
        int id = extras.getInt(KEY_ID, -1);
        long repeatInterval = extras.getLong(KEY_REPEAT_INTERVAL, -1);
        // fireTime not taken from notification, because we may have adjusted it

        // when rescheduling after boot notification may be absent
        // also, we may be replacing an existing notification
        mScheduledNotifications.put(Integer.valueOf(id), notificationBuilder);
        intent.putExtra(KEY_NOTIFICATION_ID, id);

        PendingIntent broadcast = getBroadcastPendingIntent(id, intent, PendingIntent.FLAG_UPDATE_CURRENT);
        scheduleNotificationIntentAlarm(repeatInterval, fireTime, broadcast);
    }

    void scheduleAlarmWithNotification(Notification.Builder notificationBuilder) {
        long fireTime = notificationBuilder.getExtras().getLong(KEY_FIRE_TIME, 0L);
        Intent intent = buildNotificationIntent();
        scheduleAlarmWithNotification(notificationBuilder, intent, fireTime);
    }

    private Notification buildNotificationForSending(Class openActivity, Notification.Builder builder) {
        int id = builder.getExtras().getInt(KEY_ID, -1);
        Intent openAppIntent = new Intent(mContext, openActivity);
        openAppIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        openAppIntent.putExtra(KEY_NOTIFICATION_ID, id);
        PendingIntent pendingIntent = getActivityPendingIntent(id, openAppIntent, 0);
        builder.setContentIntent(pendingIntent);

        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.M) {
            // Can't check StatusBar notifications pre-M, so ask to be notified when dismissed
            Intent deleteIntent = new Intent(mContext, UnityNotificationManager.class);
            deleteIntent.setAction(KEY_NOTIFICATION_DISMISSED); // need action to distinguish intent from content one
            deleteIntent.putExtra(KEY_NOTIFICATION_DISMISSED, id);
            PendingIntent deletePending = getBroadcastPendingIntent(id, deleteIntent, 0);
            builder.setDeleteIntent(deletePending);
        }

        finalizeNotificationForDisplay(builder);
        return builder.build();
    }

    void performNotificationHousekeeping(Set<String> ids) {
        Log.d(TAG_UNITY, "Checking for invalid notification IDs still hanging around");

        Set<String> invalid = findInvalidNotificationIds(ids);
        Set<String> currentIds = new HashSet<>(ids);
        for (String id : invalid) {
            currentIds.remove(id);
            mScheduledNotifications.remove(id);
        }

        // in case we have saved intents, clear them
        for (String id : invalid)
            deleteExpiredNotificationIntent(id);
    }

    private Set<String> findInvalidNotificationIds(Set<String> ids) {
        Intent intent = buildNotificationIntent();
        HashSet<String> invalid = new HashSet<String>();
        for (String id : ids) {
            // Get the given broadcast PendingIntent by id as request code.
            // FLAG_NO_CREATE is set to return null if the described PendingIntent doesn't exist.
            PendingIntent broadcast = getBroadcastPendingIntent(Integer.valueOf(id), intent, PendingIntent.FLAG_NO_CREATE);
            if (broadcast == null) {
                invalid.add(id);
            }
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            StatusBarNotification[] active = getNotificationManager().getActiveNotifications();
            for (StatusBarNotification notification : active) {
                // any notifications in status bar are still valid
                String id = String.valueOf(notification.getId());
                invalid.remove(id);
            }
        }
        else synchronized (this) {
            for (Integer visibleId : mVisibleNotifications) {
                String id = String.valueOf(visibleId);
                invalid.remove(id);
            }
        }

        // if app is launched with notification, user still has access to it
        if (UnityPlayer.currentActivity != null) {
            Intent currentIntent = UnityPlayer.currentActivity.getIntent();
            if (currentIntent.hasExtra(KEY_NOTIFICATION_ID)) {
                int id = currentIntent.getExtras().getInt(KEY_NOTIFICATION_ID);
                invalid.remove(String.valueOf(id));
            }
        }

        return invalid;
    }

    private Intent buildNotificationIntent() {
        Intent intent = new Intent(mContext, UnityNotificationManager.class);
        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        return intent;
    }

    private PendingIntent getActivityPendingIntent(int id, Intent intent, int flags) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M)
            return PendingIntent.getActivity(mContext, id, intent, flags | PendingIntent.FLAG_IMMUTABLE);
        else
            return PendingIntent.getActivity(mContext, id, intent, flags);
    }

    private PendingIntent getBroadcastPendingIntent(int id, Intent intent, int flags) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M)
            return PendingIntent.getBroadcast(mContext, id, intent, flags | PendingIntent.FLAG_IMMUTABLE);
        else
            return PendingIntent.getBroadcast(mContext, id, intent, flags);
    }

    // Save the notification intent to SharedPreferences if reschedule_on_restart is true,
    // which will be consumed by UnityNotificationRestartOnBootReceiver for device reboot.
    synchronized void saveNotification(Notification notification, boolean customized) {
        String notification_id = Integer.toString(notification.extras.getInt(KEY_ID, -1));
        SharedPreferences prefs = mContext.getSharedPreferences(getSharedPrefsNameByNotificationId(notification_id), Context.MODE_PRIVATE);
        UnityNotificationUtilities.serializeNotification(prefs, notification, customized);
    }

    static String getSharedPrefsNameByNotificationId(String id) {
        return String.format("u_notification_data_%s", id);
    }

    // Load all the notification intents from SharedPreferences.
    synchronized List<Notification.Builder> loadSavedNotifications() {
        Set<String> ids = getScheduledNotificationIDs();

        List<Notification.Builder> intent_data_list = new ArrayList();
        Set<String> idsMarkedForRemoval = new HashSet<String>();

        for (String id : ids) {
            SharedPreferences prefs = mContext.getSharedPreferences(getSharedPrefsNameByNotificationId(id), Context.MODE_PRIVATE);
            Notification.Builder builder = null;
            Object notification = UnityNotificationUtilities.deserializeNotification(mContext, prefs);
            if (notification != null) {
                if (notification instanceof Notification.Builder)
                    builder = (Notification.Builder)notification;
                else
                    builder = UnityNotificationUtilities.recoverBuilder(mContext, (Notification)notification);
            }

            if (builder != null)
                intent_data_list.add(builder);
            else
                idsMarkedForRemoval.add(id);
        }

        if (idsMarkedForRemoval.size() > 0) {
            ids = new HashSet<>(ids);
            for (String id : idsMarkedForRemoval) {
                ids.remove(id);
                deleteExpiredNotificationIntent(id);
            }
            saveScheduledNotificationIDs(ids);
        }

        return intent_data_list;
    }

    private boolean canScheduleExactAlarms(AlarmManager alarmManager) {
        // exact scheduling supported since Android 6
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.M)
            return false;
        if (mExactSchedulingSetting < 0) {
            Bundle metaData = getAppMetadata();
            if (metaData != null)
                mExactSchedulingSetting = metaData.getInt("com.unity.androidnotifications.exact_scheduling", 1);
        }
        if (mExactSchedulingSetting == 0)
            return false;
        if (Build.VERSION.SDK_INT < 31)
            return true;

        return alarmManager.canScheduleExactAlarms();
    }

    public boolean canScheduleExactAlarms() {
        AlarmManager alarmManager = (AlarmManager) mContext.getSystemService(Context.ALARM_SERVICE);
        return canScheduleExactAlarms(alarmManager);
    }

    // Call AlarmManager to set the broadcast intent with fire time and interval.
    private void scheduleNotificationIntentAlarm(long repeatInterval, long fireTime, PendingIntent broadcast) {
        AlarmManager alarmManager = (AlarmManager) mContext.getSystemService(Context.ALARM_SERVICE);

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
            for (StatusBarNotification n : getNotificationManager().getActiveNotifications()) {
                if (id == n.getId())
                    return 2;
            }
        } else synchronized (this) {
            for (Integer notificationId : mVisibleNotifications) {
                if (notificationId.intValue() == id)
                    return 2;
            }
        }

        if (mScheduledNotifications.containsKey(id))
            return 1;
        if (checkIfPendingNotificationIsRegistered(id))
            return 1;

        return 0;
    }

    // Check if the pending notification with the given id has been registered.
    public boolean checkIfPendingNotificationIsRegistered(int id) {
        Intent intent = new Intent(mActivity, UnityNotificationManager.class);
        return (getBroadcastPendingIntent(id, intent, PendingIntent.FLAG_NO_CREATE) != null);
    }

    // Cancel all the pending notifications.
    public void cancelAllPendingNotificationIntents() {
        mBackgroundThread.enqueueCancelAllNotifications();
    }

    private synchronized Set<String> getScheduledNotificationIDs() {
        SharedPreferences prefs = mContext.getSharedPreferences(NOTIFICATION_IDS_SHARED_PREFS, Context.MODE_PRIVATE);
        Set<String> ids = prefs.getStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, new HashSet<String>());
        return ids;
    }

    synchronized void saveScheduledNotificationIDs(Set<String> ids) {
        SharedPreferences.Editor editor = mContext.getSharedPreferences(NOTIFICATION_IDS_SHARED_PREFS, Context.MODE_PRIVATE).edit().clear();
        editor.putStringSet(NOTIFICATION_IDS_SHARED_PREFS_KEY, ids);
        editor.apply();
    }

    // Cancel a pending notification by id.
    public void cancelPendingNotification(int id) {
        mBackgroundThread.enqueueCancelNotification(id);
    }

    // Cancel a pending notification by id.
    void cancelPendingNotificationIntent(int id) {
        Intent intent = new Intent(mContext, UnityNotificationManager.class);
        PendingIntent broadcast = getBroadcastPendingIntent(id, intent, PendingIntent.FLAG_NO_CREATE);

        if (broadcast != null) {
            AlarmManager alarmManager = (AlarmManager) mContext.getSystemService(Context.ALARM_SERVICE);
            alarmManager.cancel(broadcast);
            broadcast.cancel();
        }
    }

    // Delete the notification intent from SharedPreferences by id.
    synchronized void deleteExpiredNotificationIntent(String id) {
        SharedPreferences notificationPrefs = mContext.getSharedPreferences(getSharedPrefsNameByNotificationId(id), Context.MODE_PRIVATE);
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
        // This method is called on OS created instance and that instance is recreated during various times
        // for example sending app to background will cause new instance to be created when alarm fires
        // since we also create one instance for our uses, always forward to that instance (creating if necessary)
        getNotificationManagerImpl(context).onReceive(intent);
    }

    public void onReceive(Intent intent) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.M) {
            if (KEY_NOTIFICATION_DISMISSED.equals(intent.getAction())) {
                int removedId = intent.getIntExtra(KEY_NOTIFICATION_DISMISSED, -1);
                if (removedId > 0) synchronized (this) {
                    mVisibleNotifications.remove(removedId);
                }
                return;
            }
        }
        showNotification(intent);
    }

    private void showNotification(Intent intent) {
        Object notification = getNotificationOrIdForIntent(intent);
        if (notification == null) {
            return;
        }

        if (notification instanceof Notification) {
            Notification notif = (Notification) notification;
            int id = notif.extras.getInt(KEY_ID, -1);
            notify(id, notif);
            return;
        }

        Integer notificationId = (Integer)notification;
        Notification.Builder builder = mScheduledNotifications.get(notificationId);
        if (builder != null) {
            notify(notificationId, builder);
            return;
        }

        AsyncTask.execute(() -> {
            Notification.Builder nb = deserializeNotificationBuilder(notificationId);
            if (nb == null) {
                Log.e(TAG_UNITY, "Failed to recover builder, can't send notification");
                return;
            }

            notify(notificationId, nb);
        });
    }

    void notify(int id, Notification.Builder builder) {
        Class openActivity;
        if (mOpenActivity == null) {
            openActivity = UnityNotificationUtilities.getOpenAppActivity(mContext);
            if (openActivity == null) {
                Log.e(TAG_UNITY, "Activity not found, cannot show notification");
                return;
            }
        }
        else {
            openActivity = mOpenActivity;
        }

        Notification notification = buildNotificationForSending(openActivity, builder);
        if (notification != null) {
            notify(id, notification);
        }
    }

    // Call the system notification service to notify the notification.
    private void notify(int id, Notification notification) {
        boolean showInForeground = notification.extras.getBoolean(KEY_SHOW_IN_FOREGROUND, true);
        if (!isInForeground() || showInForeground) {
            getNotificationManager().notify(id, notification);
            if (Build.VERSION.SDK_INT < Build.VERSION_CODES.M) synchronized (this) {
                mVisibleNotifications.add(Integer.valueOf(id));
            }
        }

        long repeatInterval = notification.extras.getLong(KEY_REPEAT_INTERVAL, -1);
        if (repeatInterval <= 0) {
            mScheduledNotifications.remove(id);
            cancelPendingNotificationIntent(id);
        }

        try {
            if (mNotificationCallback != null)
                mNotificationCallback.onSentNotification(notification);
        } catch (RuntimeException ex) {
            Log.w(TAG_UNITY, "Can not invoke OnNotificationReceived event when the app is not running!");
        }
    }

    public static Integer getNotificationColor(Notification notification) {
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

    private void finalizeNotificationForDisplay(Notification.Builder notificationBuilder) {
        String icon = notificationBuilder.getExtras().getString(KEY_SMALL_ICON);
        Object ico = getIconForUri(icon);
        if (ico != null && Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            notificationBuilder.setSmallIcon((Icon)ico);
        } else {
            int iconId = UnityNotificationUtilities.findResourceIdInContextByName(mContext, icon);
            if (iconId == 0) {
                iconId = mContext.getApplicationInfo().icon;
            }
            notificationBuilder.setSmallIcon(iconId);
        }

        icon = notificationBuilder.getExtras().getString(KEY_LARGE_ICON);
        Object largeIcon = getIcon(icon);
        if (largeIcon != null) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M && largeIcon instanceof Icon)
                notificationBuilder.setLargeIcon((Icon)largeIcon);
            else
                notificationBuilder.setLargeIcon((Bitmap)largeIcon);
        }

        setupBigPictureStyle(notificationBuilder);
    }

    private Object getIcon(String icon) {
        if (icon == null || icon.length() == 0)
            return null;
        if (icon.charAt(0) == '/') {
            return BitmapFactory.decodeFile(icon);
        }

        Object ico = getIconForUri(icon);
        if (ico != null)
            return ico;

        return getIconFromResources(icon, false);
    }

    private Object getIconForUri(String uri) {
        if (uri == null || uri.length() == 0)
            return null;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M && uri.indexOf("://") > 0) {
            return Icon.createWithContentUri(uri);
        }

        return null;
    }

    private Object getIconFromResources(String name, boolean forceBitmap) {
        int iconId = UnityNotificationUtilities.findResourceIdInContextByName(mContext, name);
        if (iconId != 0) {
            if (!forceBitmap && Build.VERSION.SDK_INT >= Build.VERSION_CODES.M)
                return Icon.createWithResource(mContext, iconId);
            return BitmapFactory.decodeResource(mContext.getResources(), iconId);
        }

        return null;
    }

    private Bitmap loadBitmap(String uri) {
        try {
            InputStream in = mContext.getContentResolver().openInputStream(Uri.parse(uri));
            return BitmapFactory.decodeStream(in);
        } catch (Exception e) {
            Log.e(TAG_UNITY, "Failed to load image " + uri, e);
            return null;
        }
    }

    @SuppressWarnings("deprecation")
    public Notification.Builder createNotificationBuilder(String channelID) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) {
            Notification.Builder notificationBuilder = new Notification.Builder(mContext);

            // For device below Android O, we use the values from NotificationChannelWrapper to set visibility, priority etc.
            NotificationChannelWrapper fakeNotificationChannel = getNotificationChannel(channelID);

            if (fakeNotificationChannel.vibrationPattern != null && fakeNotificationChannel.vibrationPattern.length > 0) {
                notificationBuilder.setDefaults(Notification.DEFAULT_LIGHTS | Notification.DEFAULT_SOUND);
                notificationBuilder.setVibrate(fakeNotificationChannel.vibrationPattern);
            } else {
                notificationBuilder.setDefaults(Notification.DEFAULT_ALL);
            }

            notificationBuilder.setVisibility((int) fakeNotificationChannel.lockscreenVisibility);

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
            return new Notification.Builder(mContext, channelID);
        }
    }

    public static void setNotificationIcon(Notification.Builder notificationBuilder, String keyName, String icon) {
        if (icon == null || icon.length() == 0 && notificationBuilder.getExtras().getString(keyName) != null)
            notificationBuilder.getExtras().remove(keyName);
        else
            notificationBuilder.getExtras().putString(keyName, icon);
    }

    public void setupBigPictureStyle(Notification.Builder builder,
            String largeIcon, String picture, String contentTitle, String contentDescription, String summaryText, boolean showWhenCollapsed) {
        Bundle extras = builder.getExtras();
        if (picture == null || picture.length() == 0)
            return;
        extras.putString(KEY_BIG_LARGE_ICON, largeIcon);
        extras.putString(KEY_BIG_PICTURE, picture);
        extras.putString(KEY_BIG_CONTENT_TITLE, contentTitle);
        extras.putString(KEY_BIG_SUMMARY_TEXT, summaryText);
        extras.putString(KEY_BIG_CONTENT_DESCRIPTION, contentDescription);
        extras.putBoolean(KEY_BIG_SHOW_WHEN_COLLAPSED, showWhenCollapsed);
    }

    private void setupBigPictureStyle(Notification.Builder builder) {
        Bundle extras = builder.getExtras();
        String picture = extras.getString(KEY_BIG_PICTURE);
        if (picture == null)
            return;  // not big picture style
        Notification.BigPictureStyle style = new Notification.BigPictureStyle();
        String largeIcon = extras.getString(KEY_BIG_LARGE_ICON);
        Object ico = getIcon(largeIcon);
        if (ico != null) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M && ico instanceof Icon)
                style.bigLargeIcon((Icon)ico);
            else
                style.bigLargeIcon((Bitmap)ico);
        }

        if (picture.charAt(0) == '/') {
            style.bigPicture(BitmapFactory.decodeFile(picture));
        } else if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M && picture.indexOf("://") > 0) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                Icon icon = Icon.createWithContentUri(picture);
                style.bigPicture(icon);
            } else {
                Bitmap pic = loadBitmap(picture);
                if (pic != null) {
                    style.bigPicture(pic);
                }
            }
        } else {
            Object pic = getIconFromResources(picture, Build.VERSION.SDK_INT < Build.VERSION_CODES.S);
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && pic instanceof Icon)
                style.bigPicture((Icon)pic);
            else if (pic instanceof Bitmap)
                style.bigPicture((Bitmap)pic);
        }

        style.setBigContentTitle(extras.getString(KEY_BIG_CONTENT_TITLE));
        style.setSummaryText(extras.getString(KEY_BIG_SUMMARY_TEXT));
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            style.setContentDescription(extras.getString(KEY_BIG_CONTENT_DESCRIPTION));
            style.showBigPictureWhenCollapsed(extras.getBoolean(KEY_BIG_SHOW_WHEN_COLLAPSED, false));
        }

        builder.setStyle(style);
    }

    public static void setNotificationColor(Notification.Builder notificationBuilder, int color) {
        if (color != 0) {
            notificationBuilder.setColor(color);
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                notificationBuilder.setColorized(true);
            }
        }
    }

    public static void setNotificationUsesChronometer(Notification.Builder notificationBuilder, boolean usesChrono) {
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

    private static boolean isInForeground() {
        ActivityManager.RunningAppProcessInfo appProcessInfo = new ActivityManager.RunningAppProcessInfo();
        ActivityManager.getMyMemoryState(appProcessInfo);
        return (appProcessInfo.importance == IMPORTANCE_FOREGROUND || appProcessInfo.importance == IMPORTANCE_VISIBLE);
    }

    public Notification getNotificationFromIntent(Intent intent) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            if (intent.hasExtra(KEY_NOTIFICATION_ID)) {
                int id = intent.getExtras().getInt(KEY_NOTIFICATION_ID);
                StatusBarNotification[] shownNotifications = getNotificationManager().getActiveNotifications();
                for (StatusBarNotification n : shownNotifications) {
                    if (n.getId() == id) {
                        return n.getNotification();
                    }
                }
            }
        }

        Object notification = getNotificationOrBuilderForIntent(intent);
        if (notification == null)
            return null;
        if (notification instanceof Notification)
            return (Notification)notification;
        Notification.Builder builder = (Notification.Builder)notification;
        return builder.build();
    }

    private Object getNotificationOrIdForIntent(Intent intent) {
        if (intent.hasExtra(KEY_NOTIFICATION_ID)) {
            return intent.getExtras().getInt(KEY_NOTIFICATION_ID);
        } else if (intent.hasExtra(KEY_NOTIFICATION)) {
            // old code path where Notification object is in intent
            // in case the app was replaced and there still are pending alarms with notification
            return intent.getParcelableExtra(KEY_NOTIFICATION);
        }

        return null;
    }

    private Object getNotificationOrBuilderForIntent(Intent intent) {
        Object notification = getNotificationOrIdForIntent(intent);
        if (notification instanceof Integer) {
            Integer notificationId = (Integer)notification;
            if ((notification = mScheduledNotifications.get(notificationId)) == null) {
                // in case we don't have cached notification, deserialize from storage
                return deserializeNotificationBuilder(notificationId);
            }
        }

        return notification;
    }

    private Notification.Builder deserializeNotificationBuilder(Integer notificationId) {
        SharedPreferences prefs = mContext.getSharedPreferences(getSharedPrefsNameByNotificationId(notificationId.toString()), Context.MODE_PRIVATE);
        Object notification = UnityNotificationUtilities.deserializeNotification(mContext, prefs);
        if (notification == null) {
            return null;
        }

        if (notification instanceof Notification) {
            return UnityNotificationUtilities.recoverBuilder(mContext, (Notification)notification);
        }

        return (Notification.Builder)notification;
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

// Provide a wrapper for NotificationChannel.
// Create this wrapper for all Android versions as NotificationChannel is only available for Android O or above.
class NotificationChannelWrapper {
    public String id;
    public String name;
    public int importance;
    public String description;
    public boolean enableLights;
    public boolean enableVibration;
    public boolean canBypassDnd;
    public boolean canShowBadge;
    public long[] vibrationPattern;
    public int lockscreenVisibility;
    public String group;
}

// Implemented in C# to receive callback on notification show
interface NotificationCallback {
    void onSentNotification(Notification notification);
}
