package com.unity.androidnotifications;

import static com.unity.androidnotifications.UnityNotificationManager.TAG_UNITY;

import android.app.Notification;
import android.content.Context;
import android.util.Log;

import java.util.concurrent.LinkedTransferQueue;
import java.util.Set;

public class UnityNotificationBackgroundThread extends Thread {
    private static class ScheduleNotificationTask implements Runnable {
        private int notificationId;
        private Notification.Builder notificationBuilder;

        public ScheduleNotificationTask(int id, Notification.Builder builder) {
            notificationId = id;
            notificationBuilder = builder;
        }

        @Override
        public void run() {
            UnityNotificationManager.mUnityNotificationManager.performNotificationScheduling(notificationId, notificationBuilder);
        }
    }

    private LinkedTransferQueue<Runnable> mTasks = new LinkedTransferQueue();
    private int mSentNotificationsSinceHousekeeping = 0;
    private int mOtherTasksSinceHousekeeping = 0;

    public UnityNotificationBackgroundThread() {
        // force housekeeping
        mOtherTasksSinceHousekeeping = 2;
        enqueueHousekeeping();
    }

    public void enqueueNotification(int id, Notification.Builder notificationBuilder) {
        mTasks.add(new UnityNotificationBackgroundThread.ScheduleNotificationTask(id, notificationBuilder));
    }

    public void enqueueCancelNotification(int id) {
        mTasks.add(() -> {
            UnityNotificationManager.cancelPendingNotificationIntent(UnityNotificationManager.mUnityNotificationManager.mContext, id);
        });
    }

    public void enqueueCancelAllNotifications() {
        mTasks.add(() -> {
            Context context = UnityNotificationManager.mUnityNotificationManager.mContext;
            Set<String> ids = UnityNotificationManager.getScheduledNotificationIDs(context);

            if (ids.size() > 0) {
                for (String id : ids) {
                    UnityNotificationManager.cancelPendingNotificationIntent(context, Integer.valueOf(id));
                    UnityNotificationManager.deleteExpiredNotificationIntent(context, id);
                }
            }
        });
    }

    private void enqueueHousekeeping() {
        mTasks.add(() -> { performHousekeeping(); });
    }

    @Override
    public void run() {
        while (true) {
            try {
                Runnable task = mTasks.take();
                executeTask(task);
            } catch (InterruptedException e) {
                if (mTasks.isEmpty())
                    break;
            }
        }
    }

    private void executeTask(Runnable task) {
        try {
            ScheduleNotificationTask scheduleTask = null;
            if (task instanceof ScheduleNotificationTask)
                scheduleTask = (ScheduleNotificationTask)task;

            task.run();

            if (scheduleTask != null)
                ++mSentNotificationsSinceHousekeeping;
            else
                ++mOtherTasksSinceHousekeeping;
            if (mSentNotificationsSinceHousekeeping >= 50)
                enqueueHousekeeping();
        } catch (Exception e) {
            Log.e(TAG_UNITY, "Exception executing notification task", e);
        }
    }

    private void performHousekeeping() {
        // don't do housekeeping if last task we did was housekeeping (other=1)
        boolean performHousekeeping = mSentNotificationsSinceHousekeeping > 0 && mOtherTasksSinceHousekeeping > 1;
        mSentNotificationsSinceHousekeeping = 0;
        mOtherTasksSinceHousekeeping = 0;
        if (!performHousekeeping)
            return;
        Context context = UnityNotificationManager.mUnityNotificationManager.mContext;
        Set<String> notificationIds = UnityNotificationManager.getScheduledNotificationIDs(context);
        UnityNotificationManager.performNotificationHousekeeping(context, notificationIds);
    }
}
