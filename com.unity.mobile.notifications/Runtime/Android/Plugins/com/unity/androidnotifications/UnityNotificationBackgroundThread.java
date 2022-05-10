package com.unity.androidnotifications;

import java.util.concurrent.LinkedTransferQueue;

public class UnityNotificationBackgroundThread extends Thread {
    private LinkedTransferQueue<Runnable> mTasks = new LinkedTransferQueue();

    public void enqueueTask(Runnable task) {
        mTasks.add(task);
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
}
