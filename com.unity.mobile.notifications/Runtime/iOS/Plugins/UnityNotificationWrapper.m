//
//  UnityNotificationWrapper.m
//  iOS.notifications
//

#if TARGET_OS_IOS
#import <Foundation/Foundation.h>

#import "UnityNotificationManager.h"


int _NativeSizeof_iOSNotificationAuthorizationData()
{
    return sizeof(iOSNotificationAuthorizationData);
}

int _NativeSizeof_iOSNotificationData()
{
    return sizeof(iOSNotificationData);
}

int _NativeSizeof_NotificationSettingsData()
{
    return sizeof(NotificationSettingsData);
}

void _FreeUnmanagediOSNotificationDataArray(iOSNotificationData* ptr, int count)
{
    for (int i = 0; i < count; ++i)
        freeiOSNotificationData(&ptr[i]);
    free(ptr);
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

void _RequestAuthorization(void* request, int options, BOOL registerRemote)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    [manager requestAuthorization: options withRegisterRemote: registerRemote forRequest: request];
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    center.delegate = manager;
}

void _ScheduleLocalNotification(iOSNotificationData data)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    [manager scheduleLocalNotification: &data];
}

NotificationSettingsData _GetNotificationSettings()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    return UNNotificationSettingsToNotificationSettingsData(manager.cachedNotificationSettings);
}

iOSNotificationData* _GetScheduledNotificationDataArray(int* count)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    NSArray<UNNotificationRequest*>* pendingNotificationRequests = manager.cachedPendingNotificationRequests;
    if (pendingNotificationRequests == nil)
    {
        *count = 0;
        return NULL;
    }
    *count = (int)pendingNotificationRequests.count;
    if (*count == 0)
        return NULL;

    iOSNotificationData* ret = (iOSNotificationData*)malloc(*count * sizeof(iOSNotificationData));
    for (int i = 0; i < *count; ++i)
    {
        UNNotificationRequest *request = pendingNotificationRequests[i];
        ret[i] = UNNotificationRequestToiOSNotificationData(request);
    }

    return ret;
}

iOSNotificationData* _GetDeliveredNotificationDataArray(int* count)
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    NSArray<UNNotification*>* deliveredNotifications = manager.cachedDeliveredNotifications;
    if (deliveredNotifications == nil)
    {
        *count = 0;
        return NULL;
    }
    *count = (int)deliveredNotifications.count;
    if (*count == 0)
        return NULL;

    iOSNotificationData* ret = (iOSNotificationData*)malloc(*count * sizeof(iOSNotificationData));
    for (int i = 0; i < *count; ++i)
    {
        UNNotification* notification = deliveredNotifications[i];
        ret[i] = UNNotificationRequestToiOSNotificationData(notification.request);
    }

    return ret;
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
    iOSNotificationData* ret = (iOSNotificationData*)malloc(sizeof(iOSNotificationData));
    *ret = UNNotificationRequestToiOSNotificationData(manager.lastReceivedNotification.request);
    return ret;
}

#endif
