//
//  UnityNotificationWrapper.m
//  iOS.notifications
//
//  Copyright © 2018 Unity Technologies. All rights reserved.
//

#import <Foundation/Foundation.h>

#if defined(UNITY_USES_LOCATION) && UNITY_USES_LOCATION
    #import <CoreLocation/CoreLocation.h>
#endif

#import "UnityNotificationManager.h"
#import "UnityNotificationWrapper.h"

AuthorizationRequestResponse req_callback;
DATA_CALLBACK g_notificationReceivedCallback;
DATA_CALLBACK g_remoteNotificationCallback;

void _FreeUnmanagedStruct(void* ptr)
{
    if (ptr != NULL)
    {
        free(ptr);
        ptr = NULL;
    }
}

void onNotificationReceived(struct iOSNotificationData* data)
{
    if (g_notificationReceivedCallback != NULL)
    {
        g_notificationReceivedCallback(data);
        _FreeUnmanagedStruct(data);
    }
}

void onRemoteNotificationReceived(struct iOSNotificationData* data)
{
    if (g_remoteNotificationCallback != NULL)
    {
        g_remoteNotificationCallback(data);
        _FreeUnmanagedStruct(data);
    }
}

void _SetAuthorizationRequestReceivedDelegate(AUTHORIZATION_CALBACK callback)
{
    req_callback = callback;
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    manager.onAuthorizationCompletionCallback = req_callback;
}

void _SetNotificationReceivedDelegate(DATA_CALLBACK callback)
{
    g_notificationReceivedCallback = callback;
    
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    manager.onNotificationReceivedCallback = &onNotificationReceived;
}

void _SetRemoteNotificationReceivedDelegate(DATA_CALLBACK callback)
{
    g_remoteNotificationCallback = callback;
    
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    manager.onCatchReceivedRemoteNotificationCallback = &onRemoteNotificationReceived;
}


void _RequestAuthorization(int options, BOOL registerRemote)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    [manager requestAuthorization:(options) : registerRemote];
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    center.delegate = manager;
}

void _ScheduleLocalNotification(struct iOSNotificationData* data)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    
    UNAuthorizationStatus authorizationStatus = manager.cachedNotificationSettings.authorizationStatus;
    
    bool canSendNotifications = authorizationStatus != UNAuthorizationStatusAuthorized;
    if (@available(iOS 12.0, *)) {
        if (!canSendNotifications)
            canSendNotifications = authorizationStatus == UNAuthorizationStatusProvisional;
    }
    if (canSendNotifications)
    {
        NSLog(@"Attempting to schedule a local notification without authorization, please call RequestAuthorization before attempting to schedule any notifications.");
        return;
    }
        assert(manager.onNotificationReceivedCallback != NULL);
    
    NSDictionary *userInfo = @{
                                 @"showInForeground" : @(data->showInForeground),
                                 @"showInForegroundPresentationOptions" : [NSNumber numberWithInteger:data->showInForegroundPresentationOptions],
                                 @"data" : @(data->data),
                                 };

    UNMutableNotificationContent* content = [[UNMutableNotificationContent alloc] init];
    
    NSString *title = [NSString localizedUserNotificationStringForKey: [NSString stringWithUTF8String: data->title] arguments:nil];
    NSString *body = [NSString localizedUserNotificationStringForKey: [NSString stringWithUTF8String: data->body] arguments:nil];
    
    // iOS 10 does not show notifications with an empty body or title fields. Since this works fine on iOS 11+ we'll add assign a string
    // with a space to maintain consistent behaviour.
    if (@available(iOS 11.0, *)) {
    }
    else {
        if (title.length == 0)
            title = @" ";
        if (body.length == 0)
            body = @" ";
    }
    
    content.title = title;
    content.body = body;
    
    content.userInfo = userInfo;
    
    if (data->badge >= 0)
    {
        content.badge = [NSNumber numberWithInt:data->badge];
    }
    
    if (data->subtitle != NULL)
        content.subtitle = [NSString localizedUserNotificationStringForKey: [NSString stringWithUTF8String: data->subtitle] arguments:nil];
    
    if (data->categoryIdentifier != NULL)
        content.categoryIdentifier = [NSString stringWithUTF8String:data->categoryIdentifier];
    
    if (data->threadIdentifier != NULL)
        content.threadIdentifier = [NSString stringWithUTF8String:data->threadIdentifier];
    
    // TODO add a way to specify custom sounds.
    content.sound = [UNNotificationSound defaultSound];
    
    UNNotificationTrigger* trigger;
    
    if ( data->triggerType == TIME_TRIGGER)
    {
        trigger = [UNTimeIntervalNotificationTrigger triggerWithTimeInterval:data->timeTriggerInterval repeats: data -> repeats];
    }
    else if ( data->triggerType == CALENDAR_TRIGGER)
    {
        NSDateComponents* date = [[NSDateComponents alloc] init];
        if ( data->calendarTriggerYear >= 0)
            date.year = data->calendarTriggerYear;
        if (data->calendarTriggerMonth >= 0)
            date.month = data->calendarTriggerMonth;
        if (data->calendarTriggerDay >= 0)
            date.day = data->calendarTriggerDay;
        if (data->calendarTriggerHour >= 0)
            date.hour = data->calendarTriggerHour;
        if (data->calendarTriggerMinute >= 0)
            date.minute = data->calendarTriggerMinute;
        if (data->calendarTriggerSecond >= 0) 
            date.second = data->calendarTriggerSecond;
        
        trigger = [UNCalendarNotificationTrigger triggerWithDateMatchingComponents:date repeats:data->repeats];
    }
    else if ( data->triggerType == LOCATION_TRIGGER)
    {
#if defined(UNITY_USES_LOCATION) && UNITY_USES_LOCATION
        CLLocationCoordinate2D center = CLLocationCoordinate2DMake(data->locationTriggerCenterX, data->locationTriggerCenterY);
            
        CLCircularRegion* region = [[CLCircularRegion alloc] initWithCenter:center
                                                                         radius:data->locationTriggerRadius identifier:@"Headquarters"];
        region.notifyOnEntry = data->locationTriggerNotifyOnEntry;
        region.notifyOnExit = data->locationTriggerNotifyOnExit;
            
        trigger = [UNLocationNotificationTrigger triggerWithRegion:region repeats:NO];
#else
        return;
#endif
    }
    else
    {
        return;
    }

    UNNotificationRequest* request = [UNNotificationRequest requestWithIdentifier:
                                      [NSString stringWithUTF8String:data->identifier] content:content trigger:trigger];
    
    // Schedule the notification.
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center addNotificationRequest:request withCompletionHandler:^(NSError * _Nullable error) {
        
        if (error != NULL)
            NSLog(@"%@",[error localizedDescription]);
        
        [manager updateScheduledNotificationList];

    }];
}

NotificationSettingsData* _GetNotificationSettings()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    return [UnityNotificationManager UNNotificationSettingsToNotificationSettingsData:[manager cachedNotificationSettings]];
}

int _GetScheduledNotificationDataCount()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    return (int)[manager.cachedPendingNotificationRequests count];
}
iOSNotificationData* _GetScheduledNotificationDataAt(int index)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];

    if (index >= [manager.cachedPendingNotificationRequests count])
        return NULL;
    
    UNNotificationRequest * request = manager.cachedPendingNotificationRequests[index];
    
    
    return [UnityNotificationManager UNNotificationRequestToiOSNotificationData : request];
}

int _GetDeliveredNotificationDataCount()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    return [manager.cachedDeliveredNotifications count];
}
iOSNotificationData* _GetDeliveredNotificationDataAt(int index)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    
    if (index >= [manager.cachedDeliveredNotifications count])
        return NULL;
    
    UNNotification * notification = manager.cachedDeliveredNotifications[index];
    
    return [UnityNotificationManager UNNotificationToiOSNotificationData: notification];
}


void _RemoveScheduledNotification(const char* identifier)
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center removePendingNotificationRequestsWithIdentifiers:@[[NSString stringWithUTF8String:identifier]]];
    [[UnityNotificationManager sharedInstance] updateScheduledNotificationList];
}


void _RemoveAllScheduledNotifications()
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center removeAllPendingNotificationRequests];
    [[UnityNotificationManager sharedInstance] updateScheduledNotificationList];
}

void _RemoveDeliveredNotification(const char* identifier)
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center removeDeliveredNotificationsWithIdentifiers:@[[NSString stringWithUTF8String:identifier]]];
    [[UnityNotificationManager sharedInstance] updateDeliveredNotificationList];
}

void _RemoveAllDeliveredNotifications()
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center removeAllDeliveredNotifications];
    [[UnityNotificationManager sharedInstance] updateDeliveredNotificationList];
}

void _SetApplicationBadge(long badge)
{
    [[UIApplication sharedApplication] setApplicationIconBadgeNumber:badge];
}

long _GetApplicationBadge()
{
    return[UIApplication sharedApplication].applicationIconBadgeNumber;
}

bool _GetAppOpenedUsingNotification()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    return manager.lastReceivedNotification != NULL;
}


iOSNotificationData* _GetLastNotificationData()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    iOSNotificationData* data = [UnityNotificationManager UNNotificationRequestToiOSNotificationData : manager.lastReceivedNotification.request];
    return data;
}

