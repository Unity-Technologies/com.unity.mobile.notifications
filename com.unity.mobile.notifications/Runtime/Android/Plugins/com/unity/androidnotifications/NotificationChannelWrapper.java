package com.unity.androidnotifications;

import android.support.annotation.Keep;

// Provide a wrapper for NotificationChannel.
// create this wrapper for all Android versions as NotificationChannel is only available for Android O or above.
@Keep
public class NotificationChannelWrapper {

    public String id;
    public String name;
    public int importance;
    public String description;
    public boolean enableLights;
    public boolean enableVibration;
    public boolean canBypassDnd;
    public boolean canShowBadge;
    public long[] vibrationPattern;
    public int lockscreenVisibility;
}
