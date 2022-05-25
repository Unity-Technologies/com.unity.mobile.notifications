package com.unity.androidnotifications;

import android.app.Activity;
import android.app.NotificationChannel;
import android.content.Context;
import android.os.Build;

import java.util.ArrayList;
import java.util.List;

public class UnityNotificationManagerOreo extends UnityNotificationManager {
    public UnityNotificationManagerOreo(Context context, Activity activity) {
        super(context, activity);
    }
}
