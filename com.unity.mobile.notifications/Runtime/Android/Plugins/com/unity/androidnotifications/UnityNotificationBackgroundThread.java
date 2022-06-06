package com.unity.androidnotifications;

import static com.unity.androidnotifications.UnityNotificationManager.KEY_ID;
import static com.unity.androidnotifications.UnityNotificationManager.TAG_UNITY;

import android.app.Notification;
import android.content.Context;
import android.util.Log;

import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.LinkedTransferQueue;
import java.util.Enumeration;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

public class UnityNotificationBackgroundThread extends Thread {
    private static abstract class Task {
        // returns true if notificationIds was modified (needs to be saved)
        public abstract boolean run(Context context, ConcurrentHashMap<Integer, Notification.Builder> notifications);
    }

    private static class ScheduleNotificationTask extends Task {
        private int notificationId;
        private Notification.Builder notificationBuilder;

        public ScheduleNotificationTask(int id, Notification.Builder builder) {
            notificationId = id;
            notificationBuilder = builder;
        }

        @Override
        public boolean run(Context context, ConcurrentHashMap<Integer, Notification.Builder> notifications) {
            String id = String.valueOf(notificationId);
            Integer ID = Integer.valueOf(notificationId);
            try {
                UnityNotificationManager.mUnityNotificationManager.performNotificationScheduling(notificationId, notificationBuilder);
                return notifications.put(ID, notificationBuilder) == null;
            } finally {
                // if failed to schedule or replace, remove from settings and cache, so the status is correctly reported
                if (!notifications.containsKey(notificationId)) {
                    UnityNotificationManager.deleteExpiredNotificationIntent(context, id);
                    UnityNotificationManager.removeScheduledNotification(ID);
                }
            }
        }
    }

    private static class CancelNotificationTask extends Task {
        private int notificationId;

        public CancelNotificationTask(int id) {
            notificationId = id;
        }

        @Override
        public boolean run(Context context, ConcurrentHashMap<Integer, Notification.Builder> notifications) {
            UnityNotificationManager.cancelPendingNotificationIntent(context, notificationId);
            if (notifications.remove(notificationId) != null) {
                UnityNotificationManager.deleteExpiredNotificationIntent(context, String.valueOf(notificationId));
                return true;
            }

            return false;
        }
    }

    private static class CancelAllNotificationsTask extends Task {
        @Override
        public boolean run(Context context, ConcurrentHashMap<Integer, Notification.Builder> notifications) {
            if (notifications.isEmpty())
                return false;

            Enumeration<Integer> ids = notifications.keys();
            while (ids.hasMoreElements()) {
                Integer notificationId = ids.nextElement();
                UnityNotificationManager.cancelPendingNotificationIntent(context, notificationId);
                UnityNotificationManager.deleteExpiredNotificationIntent(context, String.valueOf(notificationId));
            }

            notifications.clear();
            return true;
        }
    }

    private static class HousekeepingTask extends Task {
        UnityNotificationBackgroundThread thread;

        public HousekeepingTask(UnityNotificationBackgroundThread th) {
            thread = th;
        }

        @Override
        public boolean run(Context context, ConcurrentHashMap<Integer, Notification.Builder> notifications) {
            HashSet<String> notificationIds = new HashSet<>();
            Enumeration<Integer> ids = notifications.keys();
            while (ids.hasMoreElements()) {
                notificationIds.add(String.valueOf(ids.nextElement()));
            }
            thread.performHousekeeping(context, notificationIds);
            return false;
        }
    }

    private static final int TASKS_FOR_HOUSEKEEPING = 50;
    private LinkedTransferQueue<Task> mTasks = new LinkedTransferQueue();
    private ConcurrentHashMap<Integer, Notification.Builder> mScheduledNotifications = new ConcurrentHashMap();
    private static Context mContext;
    private int mTasksSinceHousekeeping = TASKS_FOR_HOUSEKEEPING;  // we want hoursekeeping at the start

    public UnityNotificationBackgroundThread(Context context) {
        mContext = context;
        loadNotifications();
    }

    public void enqueueNotification(int id, Notification.Builder notificationBuilder) {
        mTasks.add(new UnityNotificationBackgroundThread.ScheduleNotificationTask(id, notificationBuilder));
    }

    public void enqueueCancelNotification(int id) {
        mTasks.add(new CancelNotificationTask(id));
    }

    public void enqueueCancelAllNotifications() {
        mTasks.add(new CancelAllNotificationsTask());
    }

    private void enqueueHousekeeping() {
        mTasks.add(new HousekeepingTask(this));
    }

    @Override
    public void run() {
        boolean haveChanges = false;
        while (true) {
            try {
                Task task = mTasks.take();
                haveChanges |= executeTask(mContext, task, mScheduledNotifications);
                if (!(task instanceof HousekeepingTask))
                    ++mTasksSinceHousekeeping;
                if (mTasks.size() == 0 && haveChanges) {
                    haveChanges = false;
                    enqueueHousekeeping();
                }
            } catch (InterruptedException e) {
                if (mTasks.isEmpty())
                    break;
            }
        }
    }

    private boolean executeTask(Context context, Task task, ConcurrentHashMap<Integer, Notification.Builder> notifications) {
        try {
            return task.run(context, notifications);
        } catch (Exception e) {
            Log.e(TAG_UNITY, "Exception executing notification task", e);
            return false;
        }
    }

    private void performHousekeeping(Context context, Set<String> notificationIds) {
        // don't do housekeeping if last task we did was housekeeping (other=1)
        boolean performHousekeeping = mTasksSinceHousekeeping >= TASKS_FOR_HOUSEKEEPING;
        mTasksSinceHousekeeping = 0;
        if (performHousekeeping)
            UnityNotificationManager.performNotificationHousekeeping(context, notificationIds);
        UnityNotificationManager.saveScheduledNotificationIDs(context, notificationIds);
    }

    private void loadNotifications() {
        List<Notification.Builder> notifications = UnityNotificationManager.loadSavedNotifications(mContext);
        for (Notification.Builder builder : notifications) {
            int id = builder.getExtras().getInt(KEY_ID, -1);
            mScheduledNotifications.put(id, builder);
        }
    }
}
