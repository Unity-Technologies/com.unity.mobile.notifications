package com.unity.androidnotifications;

import static com.unity.androidnotifications.UnityNotificationManager.KEY_FIRE_TIME;
import static com.unity.androidnotifications.UnityNotificationManager.KEY_ID;
import static com.unity.androidnotifications.UnityNotificationManager.TAG_UNITY;

import android.app.Notification;
import android.os.Bundle;
import android.util.Log;

import java.util.Calendar;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.LinkedTransferQueue;
import java.util.Enumeration;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

public class UnityNotificationBackgroundThread extends Thread {
    private static abstract class Task {
        // returns true if notificationIds was modified (needs to be saved)
        public abstract boolean run(UnityNotificationManager manager, ConcurrentHashMap<Integer, Notification.Builder> notifications);
    }

    private static class ScheduleNotificationTask extends Task {
        private int notificationId;
        private Notification.Builder notificationBuilder;
        private boolean isCustomized;
        private boolean isNew;

        public ScheduleNotificationTask(int id, Notification.Builder builder, boolean customized, boolean addedNew) {
            notificationId = id;
            notificationBuilder = builder;
            isCustomized = customized;
            isNew = addedNew;
        }

        @Override
        public boolean run(UnityNotificationManager manager, ConcurrentHashMap<Integer, Notification.Builder> notifications) {
            String id = String.valueOf(notificationId);
            Integer ID = Integer.valueOf(notificationId);
            boolean didSchedule = false;
            try {
                UnityNotificationManager.mUnityNotificationManager.performNotificationScheduling(notificationId, notificationBuilder, isCustomized);
                didSchedule = true;
            } finally {
                // if failed to schedule or replace, remove
                if (!didSchedule) {
                    notifications.remove(notificationId);
                    manager.cancelPendingNotificationIntent(notificationId);
                    manager.deleteExpiredNotificationIntent(id);
                }
            }

            return isNew;
        }
    }

    private static class CancelNotificationTask extends Task {
        private int notificationId;

        public CancelNotificationTask(int id) {
            notificationId = id;
        }

        @Override
        public boolean run(UnityNotificationManager manager, ConcurrentHashMap<Integer, Notification.Builder> notifications) {
            manager.cancelPendingNotificationIntent(notificationId);
            if (notifications.remove(notificationId) != null) {
                manager.deleteExpiredNotificationIntent(String.valueOf(notificationId));
                return true;
            }

            return false;
        }
    }

    private static class CancelAllNotificationsTask extends Task {
        @Override
        public boolean run(UnityNotificationManager manager, ConcurrentHashMap<Integer, Notification.Builder> notifications) {
            if (notifications.isEmpty())
                return false;

            Enumeration<Integer> ids = notifications.keys();
            while (ids.hasMoreElements()) {
                Integer notificationId = ids.nextElement();
                manager.cancelPendingNotificationIntent(notificationId);
                manager.deleteExpiredNotificationIntent(String.valueOf(notificationId));
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
        public boolean run(UnityNotificationManager manager, ConcurrentHashMap<Integer, Notification.Builder> notifications) {
            HashSet<String> notificationIds = new HashSet<>();
            Enumeration<Integer> ids = notifications.keys();
            while (ids.hasMoreElements()) {
                notificationIds.add(String.valueOf(ids.nextElement()));
            }
            thread.performHousekeeping(notificationIds);
            return false;
        }
    }

    private static final int TASKS_FOR_HOUSEKEEPING = 50;
    private LinkedTransferQueue<Task> mTasks = new LinkedTransferQueue();
    private ConcurrentHashMap<Integer, Notification.Builder> mScheduledNotifications;
    private UnityNotificationManager mManager;
    private int mTasksSinceHousekeeping = TASKS_FOR_HOUSEKEEPING;  // we want hoursekeeping at the start

    public UnityNotificationBackgroundThread(UnityNotificationManager manager, ConcurrentHashMap<Integer, Notification.Builder> scheduledNotifications) {
        mManager = manager;
        mScheduledNotifications = scheduledNotifications;
        // rescheduling after reboot may have loaded, otherwise load here
        if (mScheduledNotifications.size() == 0)
            loadNotifications();
    }

    public void enqueueNotification(int id, Notification.Builder notificationBuilder, boolean customized, boolean addedNew) {
        mTasks.add(new UnityNotificationBackgroundThread.ScheduleNotificationTask(id, notificationBuilder, customized, addedNew));
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
                haveChanges |= executeTask(mManager, task, mScheduledNotifications);
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

    private boolean executeTask(UnityNotificationManager manager, Task task, ConcurrentHashMap<Integer, Notification.Builder> notifications) {
        try {
            return task.run(manager, notifications);
        } catch (Exception e) {
            Log.e(TAG_UNITY, "Exception executing notification task", e);
            return false;
        }
    }

    private void performHousekeeping(Set<String> notificationIds) {
        // don't do housekeeping if last task we did was housekeeping (other=1)
        boolean performHousekeeping = mTasksSinceHousekeeping >= TASKS_FOR_HOUSEKEEPING;
        mTasksSinceHousekeeping = 0;
        if (performHousekeeping)
            mManager.performNotificationHousekeeping(notificationIds);
        mManager.saveScheduledNotificationIDs(notificationIds);
    }

    private void loadNotifications() {
        List<Notification.Builder> notifications = mManager.loadSavedNotifications();
        if (notifications == null || notifications.size() == 0)
            return;
        final long currentTime = Calendar.getInstance().getTime().getTime();
        boolean needHousekeeping = false;
        for (Notification.Builder builder : notifications) {
            Bundle extras = builder.getExtras();
            int id = extras.getInt(KEY_ID, -1);
            long fireTime = extras.getLong(KEY_FIRE_TIME, -1);
            boolean inFuture = fireTime - currentTime > 0;
            if (inFuture)
                mScheduledNotifications.put(id, builder);
            else
                needHousekeeping = true;
        }

        if (needHousekeeping)
            enqueueHousekeeping();
    }
}
