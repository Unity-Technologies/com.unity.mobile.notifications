package com.unity.androidnotifications;

import android.app.Notification;

public interface NotificationCallback {
    void onSentNotification(Notification notification);
}
