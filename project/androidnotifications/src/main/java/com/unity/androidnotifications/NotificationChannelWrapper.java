package com.unity.androidnotifications;

import android.support.annotation.Keep;

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