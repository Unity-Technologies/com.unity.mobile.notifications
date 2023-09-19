//
//  UnityNotificationManager.m
//  iOS.notifications
//

#if TARGET_OS_IOS

#import "UnityNotificationManager.h"

#if UNITY_USES_LOCATION
#import <CoreLocation/CoreLocation.h>
#endif

@implementation UnityNotificationManager
{
    NSLock* _lock;
    UNAuthorizationStatus _remoteNotificationsRegistered;
    NSInteger _remoteNotificationForegroundPresentationOptions;
    NSString* _deviceToken;
    NSPointerArray* _pendingRemoteAuthRequests;
}

+ (instancetype)sharedInstance
{
    static UnityNotificationManager *sharedInstance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedInstance = [[UnityNotificationManager alloc] init];
    });

    [sharedInstance updateNotificationSettings];
    [sharedInstance updateScheduledNotificationList];

    return sharedInstance;
}

- (id)init
{
    _lock = [[NSLock alloc] init];
    _remoteNotificationsRegistered = UNAuthorizationStatusNotDetermined;
    _deviceToken = nil;
    _pendingRemoteAuthRequests = nil;
    _remoteNotificationForegroundPresentationOptions = [[[NSBundle mainBundle] objectForInfoDictionaryKey: @"UnityRemoteNotificationForegroundPresentationOptions"] integerValue];
    return self;
}

- (void)finishAuthorization:(struct iOSNotificationAuthorizationData*)authData forRequest:(void*)request
{
    if (self.onAuthorizationCompletionCallback != NULL && request)
        self.onAuthorizationCompletionCallback(request, *authData);
}

- (void)finishRemoteNotificationRegistration:(UNAuthorizationStatus)status notification:(NSNotification*)notification
{
    struct iOSNotificationAuthorizationData authData;
    authData.granted = status == UNAuthorizationStatusAuthorized;
    authData.error = NULL;
    authData.deviceToken = NULL;
    NSString* deviceToken = nil;
    if (authData.granted)
    {
        deviceToken = [UnityNotificationManager deviceTokenFromNotification: notification];
        authData.deviceToken = [deviceToken UTF8String];
    }

    [_lock lock];
    _remoteNotificationsRegistered = status;
    _deviceToken = deviceToken;
    NSPointerArray* pointers = _pendingRemoteAuthRequests;
    _pendingRemoteAuthRequests = nil;
    [_lock unlock];

    while (pointers.count > 0)
    {
        unsigned long idx = pointers.count - 1;
        void* request = [pointers pointerAtIndex: idx];
        [pointers removePointerAtIndex: idx];
        [self finishAuthorization: &authData forRequest: request];
    }
}

- (void)requestAuthorization:(NSInteger)authorizationOptions withRegisterRemote:(BOOL)registerRemote forRequest:(void*)request
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];

    BOOL supportsPushNotification = [[[NSBundle mainBundle] objectForInfoDictionaryKey: @"UnityAddRemoteNotificationCapability"] boolValue];
    registerRemote = registerRemote && supportsPushNotification;

    [center requestAuthorizationWithOptions: authorizationOptions completionHandler:^(BOOL granted, NSError * _Nullable error)
    {
        BOOL authorizationRequestFinished = YES;
        struct iOSNotificationAuthorizationData authData;
        authData.granted = granted;
        authData.error =  [[error localizedDescription]cStringUsingEncoding: NSUTF8StringEncoding];
        authData.deviceToken = "";

        if (granted)
        {
            [_lock lock];
            if (registerRemote && _remoteNotificationsRegistered == UNAuthorizationStatusNotDetermined)
            {
                authorizationRequestFinished = NO;
                if (request)
                {
                    if (_pendingRemoteAuthRequests == nil)
                        _pendingRemoteAuthRequests = [NSPointerArray pointerArrayWithOptions: NSPointerFunctionsOpaqueMemory];
                    [_pendingRemoteAuthRequests addPointer: request];
                }
                dispatch_async(dispatch_get_main_queue(), ^{
                    [[UIApplication sharedApplication] registerForRemoteNotifications];
                });
            }
            else
                authData.deviceToken = [_deviceToken UTF8String];
            [_lock unlock];
        }
        else
            NSLog(@"Requesting notification authorization failed with: %@", error);

        if (authorizationRequestFinished)
            [self finishAuthorization: &authData forRequest: request];
        [self updateNotificationSettings];
    }];
}

- (void)unregisterForRemoteNotifications
{
    [[UIApplication sharedApplication] unregisterForRemoteNotifications];
    _remoteNotificationsRegistered = UNAuthorizationStatusNotDetermined;
}

+ (NSString*)deviceTokenFromNotification:(NSNotification*)notification
{
    NSData* deviceTokenData;
    if ([notification.userInfo isKindOfClass: [NSData class]])
        deviceTokenData = (NSData*)notification.userInfo;
    else
        return nil;

    NSUInteger len = deviceTokenData.length;
    if (len == 0)
        return nil;

    const unsigned char *buffer = deviceTokenData.bytes;
    NSMutableString *str  = [NSMutableString stringWithCapacity: (len * 2)];
    for (int i = 0; i < len; ++i)
        [str appendFormat: @"%02x", buffer[i]];

    return str;
}

// Called when a notification is delivered to a foreground app.
- (void)userNotificationCenter:(UNUserNotificationCenter *)center willPresentNotification:(UNNotification *)notification
    withCompletionHandler:(void (^)(UNNotificationPresentationOptions options))completionHandler
{
    iOSNotificationData notificationData;
    BOOL haveNotificationData = NO;
    if (self.onNotificationReceivedCallback != NULL)
    {
        notificationData = UNNotificationRequestToiOSNotificationData(notification.request);
        haveNotificationData = YES;
        self.onNotificationReceivedCallback(notificationData);
    }

    BOOL showInForeground;
    NSInteger presentationOptions;

    showInForeground = [[notification.request.content.userInfo objectForKey: @"showInForeground"] boolValue];
    if ([notification.request.trigger isKindOfClass: [UNPushNotificationTrigger class]])
    {
        presentationOptions = _remoteNotificationForegroundPresentationOptions;
        if (self.onRemoteNotificationReceivedCallback != NULL)
        {
            if (!haveNotificationData)
            {
                notificationData = UNNotificationRequestToiOSNotificationData(notification.request);
                haveNotificationData = YES;
            }

            self.onRemoteNotificationReceivedCallback(notificationData);
        }
        else
        {
            showInForeground = YES;
        }
    }
    else
    {
        presentationOptions = [[notification.request.content.userInfo objectForKey: @"showInForegroundPresentationOptions"] intValue];
    }

    if (haveNotificationData)
        freeiOSNotificationData(&notificationData);

    if (showInForeground)
        completionHandler(presentationOptions);
    else
        completionHandler(UNNotificationPresentationOptionNone);

    [[UnityNotificationManager sharedInstance] updateDeliveredNotificationList];
}

// Called to let your app know which action was selected by the user for a given notification.
- (void)userNotificationCenter:(UNUserNotificationCenter *)center didReceiveNotificationResponse:(UNNotificationResponse *)response
    withCompletionHandler:(nonnull void(^)(void))completionHandler
{
    self.lastReceivedNotification = response.notification;
    self.lastRespondedNotificationAction = response.actionIdentifier;
    if ([response isKindOfClass: UNTextInputNotificationResponse.class])
    {
        UNTextInputNotificationResponse* resp = (UNTextInputNotificationResponse*)response;
        self.lastRespondedNotificationUserText = resp.userText;
    }
    else
        self.lastRespondedNotificationUserText = nil;
    completionHandler();
    [[UnityNotificationManager sharedInstance] updateDeliveredNotificationList];
}

- (void)updateScheduledNotificationList
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center getPendingNotificationRequestsWithCompletionHandler:^(NSArray<UNNotificationRequest *> * _Nonnull requests) {
        self.cachedPendingNotificationRequests = requests;
    }];
}

- (void)updateDeliveredNotificationList
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center getDeliveredNotificationsWithCompletionHandler:^(NSArray<UNNotification *> * _Nonnull notifications) {
        self.cachedDeliveredNotifications = notifications;
    }];
}

- (void)updateNotificationSettings
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];

    [center getNotificationSettingsWithCompletionHandler:^(UNNotificationSettings * _Nonnull settings) {
        self.cachedNotificationSettings = settings;
    }];
}

bool validateAuthorizationStatus(UnityNotificationManager* manager)
{
    UNAuthorizationStatus authorizationStatus = manager.cachedNotificationSettings.authorizationStatus;

    if (authorizationStatus == UNAuthorizationStatusAuthorized)
        return true;

    if (authorizationStatus == UNAuthorizationStatusProvisional)
        return true;

    NSLog(@"Attempting to schedule a local notification without authorization, please call RequestAuthorization first.");
    return false;
}

- (void)scheduleLocalNotification:(iOSNotificationData*)data
{
    if (!validateAuthorizationStatus(self))
        return;

    assert(self.onNotificationReceivedCallback != NULL);

    NSDictionary* userInfo = (__bridge_transfer NSDictionary*)data->userInfo;
    data->userInfo = NULL;

    // Convert from iOSNotificationData to UNMutableNotificationContent.
    UNMutableNotificationContent* content = [[UNMutableNotificationContent alloc] init];

    NSString* dataTitle = data->title ? [NSString stringWithUTF8String: data->title] : [NSString string];
    NSString* dataBody  = data->body  ? [NSString stringWithUTF8String: data->body]  : [NSString string];

    content.title = [NSString localizedUserNotificationStringForKey: dataTitle arguments: nil];
    content.body  = [NSString localizedUserNotificationStringForKey: dataBody arguments: nil];
    content.userInfo = userInfo;

    if (data->badge >= 0)
        content.badge = [NSNumber numberWithInt: data->badge];

    if (data->subtitle != NULL)
        content.subtitle = [NSString localizedUserNotificationStringForKey: [NSString stringWithUTF8String: data->subtitle] arguments: nil];

    if (data->categoryIdentifier != NULL)
        content.categoryIdentifier = [NSString stringWithUTF8String: data->categoryIdentifier];

    if (data->threadIdentifier != NULL)
        content.threadIdentifier = [NSString stringWithUTF8String: data->threadIdentifier];

    UNNotificationSound* sound = [self soundForNotification: data];
    if (sound != nil)
        content.sound = sound;
    if (@available(iOS 15.0, *))
    {
        content.interruptionLevel = [self unityInterruptionLevelToIos: data->interruptionLevel];
        content.relevanceScore = data->relevanceScore;
    }

    content.attachments = (__bridge_transfer NSArray<UNNotificationAttachment*>*)data->attachments;
    data->attachments = NULL;

    NSString* identifier = [NSString stringWithUTF8String: data->identifier];
    // Generate UNNotificationTrigger from iOSNotificationData.
    UNNotificationTrigger* trigger;
    if (data->triggerType == TIME_TRIGGER)
    {
        trigger = [UNTimeIntervalNotificationTrigger triggerWithTimeInterval: data->trigger.timeInterval.interval repeats: data->trigger.timeInterval.repeats];
    }
    else if (data->triggerType == CALENDAR_TRIGGER)
    {
        NSDateComponents* date = [[NSDateComponents alloc] init];
        if (data->trigger.calendar.year >= 0)
            date.year = data->trigger.calendar.year;
        if (data->trigger.calendar.month >= 0)
            date.month = data->trigger.calendar.month;
        if (data->trigger.calendar.day >= 0)
            date.day = data->trigger.calendar.day;
        if (data->trigger.calendar.hour >= 0)
            date.hour = data->trigger.calendar.hour;
        if (data->trigger.calendar.minute >= 0)
            date.minute = data->trigger.calendar.minute;
        if (data->trigger.calendar.second >= 0)
            date.second = data->trigger.calendar.second;
        // From C# we get UTC time
        date.calendar = [NSCalendar calendarWithIdentifier: NSCalendarIdentifierGregorian];
        date.timeZone = [NSTimeZone timeZoneWithAbbreviation: @"UTC"];

        trigger = [UNCalendarNotificationTrigger triggerWithDateMatchingComponents: date repeats: data->trigger.calendar.repeats];
        NSLog(@"Notification will show after %f s.", ((UNCalendarNotificationTrigger*)trigger).nextTriggerDate.timeIntervalSinceNow);
    }
    else if (data->triggerType == LOCATION_TRIGGER)
    {
#if UNITY_USES_LOCATION
        CLLocationCoordinate2D center = CLLocationCoordinate2DMake(data->trigger.location.latitude, data->trigger.location.longitude);

        CLCircularRegion* region = [[CLCircularRegion alloc] initWithCenter: center
                                    radius: data->trigger.location.radius identifier: identifier];
        region.notifyOnEntry = data->trigger.location.notifyOnEntry;
        region.notifyOnExit = data->trigger.location.notifyOnExit;

        trigger = [UNLocationNotificationTrigger triggerWithRegion: region repeats: data->trigger.location.repeats];
#else
        return;
#endif
    }
    else
    {
        return;
    }

    UNNotificationRequest* request = [UNNotificationRequest requestWithIdentifier: identifier content: content trigger: trigger];

    // Schedule the notification.
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center addNotificationRequest: request withCompletionHandler:^(NSError * _Nullable error) {
        if (error != NULL)
            NSLog(@"%@", [error localizedDescription]);

        [self updateScheduledNotificationList];
    }];
}

- (UNNotificationSound*)soundForNotification:(const iOSNotificationData*)data
{
    NSString* soundName = nil;
    if (data->soundName != NULL)
        soundName = [NSString stringWithUTF8String: data->soundName];

    switch (data->soundType)
    {
        case kSoundTypeNone:
            return nil;
        case kSoundTypeCritical:
            if (soundName != nil)
            {
                if (data->soundVolume < 0)
                    return [UNNotificationSound criticalSoundNamed: soundName];
                return [UNNotificationSound criticalSoundNamed: soundName withAudioVolume: data->soundVolume];
            }
            if (data->soundVolume >= 0)
                return [UNNotificationSound defaultCriticalSoundWithAudioVolume: data->soundVolume];
            return UNNotificationSound.defaultCriticalSound;
        case kSoundTypeRingtone:
            if (@available(iOS 15.2, *))
            {
                if (soundName != nil)
                    return [UNNotificationSound ringtoneSoundNamed: soundName];
                return UNNotificationSound.defaultRingtoneSound;
            }
        // continue to default
        case kSoundTypeDefault:
        default:
            if (soundName != nil)
                return [UNNotificationSound soundNamed: soundName];
            return UNNotificationSound.defaultSound;
    }
}

- (UNNotificationInterruptionLevel)unityInterruptionLevelToIos:(int)level
    API_AVAILABLE(ios(15.0))
{
    switch (level)
    {
        case kInterruptionLevelActive:
            return UNNotificationInterruptionLevelActive;
        case kInterruptionLevelCritical:
            return UNNotificationInterruptionLevelCritical;
        case kInterruptionLevelPassive:
            return UNNotificationInterruptionLevelPassive;
        case kInterruptionLevelTimeSensitive:
            return UNNotificationInterruptionLevelTimeSensitive;
        default:
            return UNNotificationInterruptionLevelActive;
    }
}

@end
#endif
