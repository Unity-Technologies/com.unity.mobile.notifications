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
