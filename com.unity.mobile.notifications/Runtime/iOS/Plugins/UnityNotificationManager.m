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

- (void)checkAuthorizationFinished
{
    bool requestRejected = self.authorizationRequestFinished && !self.authorized;

    if (!requestRejected && self.needRemoteNotifications && self.remoteNotificationsRegistered == UNAuthorizationStatusNotDetermined)
        return;

    if (self.authorizationRequestFinished && self.onAuthorizationCompletionCallback != NULL && self.authData != NULL)
    {
        self.authData->deviceToken = [self.deviceToken UTF8String];
        self.onAuthorizationCompletionCallback(self.authData);

        free(self.authData);
        self.authData = NULL;
    }
}

- (void)requestAuthorization:(NSInteger)authorizationOptions withRegisterRemote:(BOOL)registerRemote
{
    if (!SYSTEM_VERSION_10_OR_ABOVE)
        return;

    // TODO: Why we need this parameter as we always set it to YES here?
    registerRemote = YES;

    self.authorizationRequestFinished = NO;
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];

    BOOL supportsPushNotification = [[[NSBundle mainBundle] objectForInfoDictionaryKey: @"UnityAddRemoteNotificationCapability"] boolValue];
    registerRemote = supportsPushNotification == YES ? registerRemote : NO;

    self.needRemoteNotifications = registerRemote;
    [center requestAuthorizationWithOptions: authorizationOptions completionHandler:^(BOOL granted, NSError * _Nullable error)
    {
        struct iOSNotificationAuthorizationData* authData = (struct iOSNotificationAuthorizationData*)malloc(sizeof(*authData));
        authData->finished = YES;
        authData->granted = granted;
        authData->error =  [[error localizedDescription]cStringUsingEncoding: NSUTF8StringEncoding];
        authData->deviceToken = "";

        self.authData = authData;
        self.authorized = granted;
        if (self.authorized)
        {
            if (registerRemote)
            {
                dispatch_async(dispatch_get_main_queue(), ^{
                    [[UIApplication sharedApplication] registerForRemoteNotifications];
                    self.authorizationRequestFinished = YES;
                });
            }
            else
            {
                self.authorizationRequestFinished = YES;
            }
        }
        else
        {
            self.authorizationRequestFinished = YES;
            NSLog(@"Requesting notification authorization failed with: %@", error);
        }

        [self checkAuthorizationFinished];
        [self updateNotificationSettings];
    }];
}

- (void)setDeviceTokenFromNSData:(NSData *)deviceTokenData
{
    NSUInteger len = deviceTokenData.length;
    if (len == 0)
        return;

    const unsigned char *buffer = deviceTokenData.bytes;
    NSMutableString *str  = [NSMutableString stringWithCapacity: (len * 2)];
    for (int i = 0; i < len; ++i)
    {
        [str appendFormat: @"%02x", buffer[i]];
    }
    self.deviceToken = [str copy];
}

// Called when a notification is delivered to a foreground app.
- (void)userNotificationCenter:(UNUserNotificationCenter *)center willPresentNotification:(UNNotification *)notification
    withCompletionHandler:(void (^)(UNNotificationPresentationOptions options))completionHandler
{
    iOSNotificationData* notificationData = NULL;
    if (self.onNotificationReceivedCallback != NULL)
    {
        notificationData = UNNotificationRequestToiOSNotificationData(notification.request);
        self.onNotificationReceivedCallback(notificationData);
    }

    BOOL showInForeground;
    NSInteger presentationOptions;

    if ([notification.request.trigger isKindOfClass: [UNPushNotificationTrigger class]])
    {
        if (self.onRemoteNotificationReceivedCallback != NULL)
        {
            if (notificationData == NULL)
                notificationData = UNNotificationRequestToiOSNotificationData(notification.request);

            showInForeground = NO;
            self.onRemoteNotificationReceivedCallback(notificationData);
        }
        else
        {
            showInForeground = YES;
            presentationOptions = self.remoteNotificationForegroundPresentationOptions;
        }
    }
    else
    {
        presentationOptions = [[notification.request.content.userInfo objectForKey: @"showInForegroundPresentationOptions"] intValue];
        showInForeground = [[notification.request.content.userInfo objectForKey: @"showInForeground"] boolValue];
    }

    freeiOSNotificationData(notificationData);
    notificationData = NULL;

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

    if (@available(iOS 12.0, *))
    {
        if (authorizationStatus == UNAuthorizationStatusProvisional)
            return true;
    }

    NSLog(@"Attempting to schedule a local notification without authorization, please call RequestAuthorization first.");
    return false;
}

- (void)scheduleLocalNotification:(iOSNotificationData*)data
{
    if (!validateAuthorizationStatus(self))
        return;

    assert(self.onNotificationReceivedCallback != NULL);

    NSDictionary* userInfo = @{
        @"showInForeground": @(data->showInForeground),
        @"showInForegroundPresentationOptions": [NSNumber numberWithInteger: data->showInForegroundPresentationOptions],
        @"data": @(data->data ? data->data : ""),
    };

    // Convert from iOSNotificationData to UNMutableNotificationContent.
    UNMutableNotificationContent* content = [[UNMutableNotificationContent alloc] init];

    // iOS 10 does not show notifications with an empty body or title fields.
    // Since this works fine on iOS 11+ we'll add assign a string with a space to maintain consistent behaviour.
    NSString *dataTitle, *dataBody;
    if (@available(iOS 11.0, *))
    {
        dataTitle = data->title ? [NSString stringWithUTF8String: data->title] : [NSString string];
        dataBody  = data->body  ? [NSString stringWithUTF8String: data->body]  : [NSString string];
    }
    else
    {
        dataTitle = data->title && data->title[0] ? [NSString stringWithUTF8String: data->title] : @" ";
        dataBody  = data->body  && data->body[0]  ? [NSString stringWithUTF8String: data->body]  : @" ";
    }

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

    // TODO add a way to specify custom sounds.
    content.sound = [UNNotificationSound defaultSound];

    // Generate UNNotificationTrigger from iOSNotificationData.
    UNNotificationTrigger* trigger;
    if (data->triggerType == TIME_TRIGGER)
    {
        trigger = [UNTimeIntervalNotificationTrigger triggerWithTimeInterval: data->timeTriggerInterval repeats: data->repeats];
    }
    else if (data->triggerType == CALENDAR_TRIGGER)
    {
        NSDateComponents* date = [[NSDateComponents alloc] init];
        if (data->calendarTriggerYear >= 0)
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

        trigger = [UNCalendarNotificationTrigger triggerWithDateMatchingComponents: date repeats: data->repeats];
    }
    else if (data->triggerType == LOCATION_TRIGGER)
    {
#if UNITY_USES_LOCATION
        CLLocationCoordinate2D center = CLLocationCoordinate2DMake(data->locationTriggerCenterX, data->locationTriggerCenterY);

        CLCircularRegion* region = [[CLCircularRegion alloc] initWithCenter: center
                                    radius: data->locationTriggerRadius identifier: @"Headquarters"];
        region.notifyOnEntry = data->locationTriggerNotifyOnEntry;
        region.notifyOnExit = data->locationTriggerNotifyOnExit;

        trigger = [UNLocationNotificationTrigger triggerWithRegion: region repeats: NO];
#else
        return;
#endif
    }
    else
    {
        return;
    }

    NSString* identifier = [NSString stringWithUTF8String: data->identifier];
    UNNotificationRequest* request = [UNNotificationRequest requestWithIdentifier: identifier content: content trigger: trigger];

    // Schedule the notification.
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center addNotificationRequest: request withCompletionHandler:^(NSError * _Nullable error) {
        if (error != NULL)
            NSLog(@"%@", [error localizedDescription]);

        [self updateScheduledNotificationList];
    }];
}

@end
#endif
