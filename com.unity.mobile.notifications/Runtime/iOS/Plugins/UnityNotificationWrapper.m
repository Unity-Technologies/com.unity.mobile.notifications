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
    UNNotification* notification = manager.lastReceivedNotification;
    if (notification == nil)
        return NULL;
    UNNotificationRequest* request = notification.request;
    if (request == nil)
        return NULL;
    iOSNotificationData* ret = (iOSNotificationData*)malloc(sizeof(iOSNotificationData));
    *ret = UNNotificationRequestToiOSNotificationData(request);
    return ret;
}

const char* _GetLastRespondedNotificationAction()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    NSString* action = manager.lastRespondedNotificationAction;
    if (action == nil)
        return NULL;
    return strdup(action.UTF8String);
}

const char* _GetLastRespondedNotificationUserText()
{
    UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
    NSString* userText = manager.lastRespondedNotificationUserText;
    if (userText == nil)
        return NULL;
    return strdup(userText.UTF8String);
}

void* _CreateUNNotificationAction(const char* identifier, const char* title, int options)
{
    UNNotificationActionOptions opts = (UNNotificationActionOptions)options;
    NSString* idr = [NSString stringWithUTF8String: identifier];
    NSString* titl = [NSString stringWithUTF8String: title];
    UNNotificationAction* action = [UNNotificationAction actionWithIdentifier: idr title: titl options: opts];
    return (__bridge_retained void*)action;
}

void* _CreateUNTextInputNotificationAction(const char* identifier, const char* title, int options, const char* buttonTitle, const char* placeholder)
{
    UNNotificationActionOptions opts = (UNNotificationActionOptions)options;
    NSString* idr = [NSString stringWithUTF8String: identifier];
    NSString* titl = [NSString stringWithUTF8String: title];
    NSString* btnTitle = [NSString stringWithUTF8String: buttonTitle];
    NSString* placeHolder = placeholder ? [NSString stringWithUTF8String: placeholder] : NULL;
    UNTextInputNotificationAction* action = [UNTextInputNotificationAction actionWithIdentifier: idr title: titl options: opts textInputButtonTitle: btnTitle textInputPlaceholder: placeHolder];
    return (__bridge_retained void*)action;
}

void _ReleaseNSObject(void* obj)
{
    NSObject* a = (__bridge_transfer NSObject*)obj;
    a = nil;
}

const char* _NSErrorToMessage(void* error)
{
    NSError* e = (__bridge_transfer NSError*)error;
    NSString* msg = e.localizedDescription;
    return strdup(msg.UTF8String);
}

void* _AddActionToNSArray(void* actions, void* action, int capacity)
{
    NSMutableArray<UNNotificationAction*>* array;
    void* ret = actions;
    if (actions == NULL)
    {
        array = [NSMutableArray arrayWithCapacity: capacity];
        ret = (__bridge_retained void*)array;
    }
    else
        array = (__bridge NSMutableArray<UNNotificationAction*>*)actions;
    UNNotificationAction* a = (__bridge UNNotificationAction*)action;
    [array addObject: a];
    return ret;
}

void* _AddStringToNSArray(void* array, const char* str, int capacity)
{
    NSMutableArray<NSString*>* arr;
    void* ret = array;
    if (array == NULL)
    {
        arr = [NSMutableArray arrayWithCapacity: capacity];
        ret = (__bridge_retained void*)arr;
    }
    else
        arr = (__bridge NSMutableArray<NSString*>*)array;
    NSString* s = [NSString stringWithUTF8String: str];
    [arr addObject: s];
    return ret;
}

void* _CreateUNNotificationCategory(const char* identifier, const char* hiddenPreviewsBodyPlaceholder, const char* summaryFormat,
    int options, void* actions, void* intentIdentifiers)
{
    NSString* idr = [NSString stringWithUTF8String: identifier];
    NSString* placeholder = hiddenPreviewsBodyPlaceholder ? [NSString stringWithUTF8String: hiddenPreviewsBodyPlaceholder] : nil;
    NSString* summary = summaryFormat ? [NSString stringWithUTF8String: summaryFormat] : nil;
    NSArray<UNNotificationAction*>* acts = (__bridge_transfer NSArray<UNNotificationAction*>*)actions;
    NSArray<NSString*>* intents = (__bridge_transfer NSArray<NSString*>*)intentIdentifiers;
    UNNotificationCategoryOptions opts = (UNNotificationCategoryOptions)options;

    UNNotificationCategory* category;
    if (@available(iOS 12.0, *))
    {
        category = [UNNotificationCategory categoryWithIdentifier: idr actions: acts intentIdentifiers: intents hiddenPreviewsBodyPlaceholder: placeholder categorySummaryFormat: summary options: opts];
    }
    else if (@available(iOS 11.0, *))
    {
        category = [UNNotificationCategory categoryWithIdentifier: idr actions: acts intentIdentifiers: intents hiddenPreviewsBodyPlaceholder: placeholder options: opts];
    }
    else
    {
        category = [UNNotificationCategory categoryWithIdentifier: idr actions: acts intentIdentifiers: intents options: opts];
    }
    return (__bridge_retained void*)category;
}

void* _AddCategoryToCategorySet(void* categorySet, void* category)
{
    UNNotificationCategory* cat = (__bridge_transfer UNNotificationCategory*)category;
    NSMutableSet<UNNotificationCategory*>* categories;
    if (categorySet == NULL)
    {
        categories = [NSMutableSet setWithObject: cat];
        return (__bridge_retained void*)categories;
    }

    categories = (__bridge NSMutableSet<UNNotificationCategory*>*)categorySet;
    [categories addObject: cat];
    return categorySet;
}

void _SetNotificationCategories(void* categorySet)
{
    NSMutableSet<UNNotificationCategory*>* categories = (__bridge_transfer NSMutableSet<UNNotificationCategory*>*)categorySet;
    [UNUserNotificationCenter.currentNotificationCenter setNotificationCategories: categories];
}

void _OpenNotificationSettings()
{
    NSURL* url = [NSURL URLWithString: UIApplicationOpenSettingsURLString];
    UIApplication* app = [UIApplication sharedApplication];
    if ([app canOpenURL: url])
        [app openURL: url options: @{} completionHandler: nil];
}

#endif
