package com.unity.androidnotifications;

import static com.unity.androidnotifications.UnityNotificationManager.TAG_UNITY;

import android.app.Notification;
import android.content.Context;
import android.util.Log;

import java.util.concurrent.LinkedTransferQueue;
import java.util.HashSet;
import java.util.Set;

public class UnityNotificationBackgroundThread extends Thread {
    private static abstract class Task {
        // returns true if notificationIds was modified (needs to be saved)
        public abstract boolean run(Context context, Set<String> notificationIds);
    }

    private static class ScheduleNotificationTask extends Task {
        private int notificationId;
        private Notification.Builder notificationBuilder;

        public ScheduleNotificationTask(int id, Notification.Builder builder) {
            notificationId = id;
            notificationBuilder = builder;
        }

        @Override
        public boolean run(Context context, Set<String> notificationIds) {
            String id = String.valueOf(notificationId);
            UnityNotificationManager.mUnityNotificationManager.performNotificationScheduling(notificationId, notificationBuilder);
            return notificationIds.add(id);
        }
    }

    private static class CancelNotificationTask extends Task {
        private int notificationId;

        public CancelNotificationTask(int id) {
            notificationId = id;
        }

        @Override
        public boolean run(Context context, Set<String> notificationIds) {
            UnityNotificationManager.cancelPendingNotificationIntent(context, notificationId);
            String id = String.valueOf(notificationId);
            if (notificationIds.remove(id)) {
                UnityNotificationManager.deleteExpiredNotificationIntent(context, id);
                return true;
            }

            return false;
        }
    }

    private static class CancelAllNotificationsTask extends Task {
        @Override
        public boolean run(Context context, Set<String> notificationIds) {
            if (notificationIds.isEmpty())
                return false;

            for (String id : notificationIds) {
                UnityNotificationManager.cancelPendingNotificationIntent(context, Integer.valueOf(id));
                UnityNotificationManager.deleteExpiredNotificationIntent(context, id);
            }

            notificationIds.clear();
            return true;
        }
    }

    private static class HousekeepingTask extends Task {
        UnityNotificationBackgroundThread thread;

        public HousekeepingTask(UnityNotificationBackgroundThread th) {
            thread = th;
        }

        @Override
        public boolean run(Context context, Set<String> notificationIds) {
            thread.performHousekeeping(context, notificationIds);
            return false;
        }
    }

    private static final int TASKS_FOR_HOUSEKEEPING = 50;
    private LinkedTransferQueue<Task> mTasks = new LinkedTransferQueue();
    private int mTasksSinceHousekeeping = TASKS_FOR_HOUSEKEEPING;  // we want hoursekeeping at the start

    public UnityNotificationBackgroundThread() {
        enqueueHousekeeping();
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
        Context context = UnityNotificationManager.mUnityNotificationManager.mContext;
        HashSet<String> notificationIds = new HashSet(UnityNotificationManager.getScheduledNotificationIDs(context));
        boolean haveChanges = false;
        while (true) {
            try {
                Task task = mTasks.take();
                haveChanges |= executeTask(context, task, notificationIds);
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

    private boolean executeTask(Context context, Task task, Set<String> notificationIds) {
        try {
            return task.run(context, notificationIds);
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
}
