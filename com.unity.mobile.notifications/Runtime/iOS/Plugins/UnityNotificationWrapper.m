//
//  UnityNotificationWrapper.m
//  iOS.notifications
//

#if TARGET_OS_IOS
#import <Foundation/Foundation.h>

#import "UnityNotificationManager.h"

void _FreeUnmanagedMemory(void* ptr)
{
    if (ptr != NULL)
    {
        free(ptr);
        ptr = NULL;
    }
}

void _FreeUnmanagediOSNotificationData(iOSNotificationData* ptr)
{
    freeiOSNotificationData(ptr);
    ptr = NULL;
}

void _SetAuthorizationRequestReceivedDelegate(AuthorizationRequestResponse callback)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    manager.onAuthorizationCompletionCallback = callback;
}

void _SetNotificationReceivedDelegate(NotificationDataReceivedResponse callback)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    manager.onNotificationReceivedCallback = callback;
}

void _SetRemoteNotificationReceivedDelegate(NotificationDataReceivedResponse callback)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    manager.onRemoteNotificationReceivedCallback = callback;
}

void _RequestAuthorization(int options, BOOL registerRemote)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    [manager requestAuthorization: options withRegisterRemote: registerRemote];
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    center.delegate = manager;
}

void _ScheduleLocalNotification(struct iOSNotificationData* data)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    [manager scheduleLocalNotification: data];
}

NotificationSettingsData* _GetNotificationSettings()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    // The native NotificationSettingsData pointer will be freed by GetNotificationSettings() in the Runtime/iOS/iOSNotificationsWrapper.cs.
    return UNNotificationSettingsToNotificationSettingsData(manager.cachedNotificationSettings);
}

int _GetScheduledNotificationDataCount()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    return (int)manager.cachedPendingNotificationRequests.count;
}

iOSNotificationData* _GetScheduledNotificationDataAt(int index)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    if (index >= manager.cachedPendingNotificationRequests.count)
        return NULL;

    UNNotificationRequest * request = manager.cachedPendingNotificationRequests[index];
    return UNNotificationRequestToiOSNotificationData(request);
}

int _GetDeliveredNotificationDataCount()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    return (int)manager.cachedDeliveredNotifications.count;
}

iOSNotificationData* _GetDeliveredNotificationDataAt(int index)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    if (index >= manager.cachedDeliveredNotifications.count)
        return NULL;

    UNNotification* notification = manager.cachedDeliveredNotifications[index];
    return UNNotificationRequestToiOSNotificationData(notification.request);
}

void _RemoveScheduledNotification(const char* identifier)
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center removePendingNotificationRequestsWithIdentifiers: @[[NSString stringWithUTF8String: identifier]]];
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
    [center removeDeliveredNotificationsWithIdentifiers: @[[NSString stringWithUTF8String: identifier]]];
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
    [[UIApplication sharedApplication] setApplicationIconBadgeNumber: badge];
}

long _GetApplicationBadge()
{
    return [UIApplication sharedApplication].applicationIconBadgeNumber;
}

bool _GetAppOpenedUsingNotification()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    return manager.lastReceivedNotification != NULL;
}

iOSNotificationData* _GetLastNotificationData()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    return UNNotificationRequestToiOSNotificationData(manager.lastReceivedNotification.request);
}

#endif
