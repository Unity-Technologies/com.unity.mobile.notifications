package com.unity.androidnotifications;

// Provide a wrapper for NotificationChannel.
// Create this wrapper for all Android versions as NotificationChannel is only available for Android O or above.
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
