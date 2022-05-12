package com.unity.androidnotifications;

import android.content.Context;

import java.util.concurrent.LinkedTransferQueue;
import java.util.Set;

public class UnityNotificationBackgroundThread extends Thread {
    private LinkedTransferQueue<Runnable> mTasks = new LinkedTransferQueue();

    public UnityNotificationBackgroundThread() {
        enqueueHousekeeping();
    }

    public void enqueueTask(Runnable task) {
        mTasks.add(task);
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

    public void enqueueHousekeeping() {
        mTasks.add(() -> { performHousekeeping(); });
    }

    @Override
    public void run() {
        while (true) {
            try {
                Runnable task = mTasks.take();
                task.run();
                android.util.Log.d("Unity", "Notification background task done");
            } catch (InterruptedException e) {
                if (mTasks.isEmpty())
                    break;
            }
        }

        android.util.Log.d("Unity", "Notification background thread exited");
    }

    private void performHousekeeping() {
        Context context = UnityNotificationManager.mUnityNotificationManager.mContext;
        Set<String> notificationIds = UnityNotificationManager.getScheduledNotificationIDs(context);
        UnityNotificationManager.performNotificationHousekeeping(context, notificationIds);
    }
}
