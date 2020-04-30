package com.unity.androidnotifications;

import android.content.Intent;

public interface NotificationCallback {
    void onSentNotification(Intent intent);
}
