package com.unity.androidnotifications;
import android.content.Intent;
import android.support.annotation.Keep;

@Keep
public interface NotificationCallback {
    void onSentNotification(Intent intent);
}