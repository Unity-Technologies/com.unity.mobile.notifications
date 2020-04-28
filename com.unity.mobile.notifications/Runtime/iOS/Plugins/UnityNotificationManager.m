//
//  UnityNotificationManager.m
//  iOS.notifications
//

#if TARGET_OS_IOS

#import "UnityNotificationManager.h"

const int kDefaultPresentationOptions = -1;

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

//Called when a notification is delivered to a foreground app.
- (void)userNotificationCenter:(UNUserNotificationCenter *)center willPresentNotification:(UNNotification *)notification withCompletionHandler:(void (^)(UNNotificationPresentationOptions options))completionHandler
{
    if (self.onNotificationReceivedCallback != NULL)
        self.onNotificationReceivedCallback(UNNotificationRequestToiOSNotificationData(notification.request));

    BOOL showInForeground;
    NSInteger presentationOptions;

    if ([notification.request.trigger isKindOfClass: [UNPushNotificationTrigger class]])
    {
        if (self.onRemoteNotificationReceivedCallback != NULL)
        {
            showInForeground = NO;
            self.onRemoteNotificationReceivedCallback(UNNotificationRequestToiOSNotificationData(notification.request));
        }
        else
        {
            showInForeground = YES;
            presentationOptions = self.remoteNotificationForegroundPresentationOptions;
            if (presentationOptions == 0)
                presentationOptions = kDefaultPresentationOptions;
        }
    }
    else
    {
        presentationOptions = [[notification.request.content.userInfo objectForKey: @"showInForegroundPresentationOptions"] intValue];
        showInForeground = [[notification.request.content.userInfo objectForKey: @"showInForeground"] boolValue];
    }
    if (showInForeground)
        completionHandler(presentationOptions);
    else
        completionHandler(UNNotificationPresentationOptionNone);

    [[UnityNotificationManager sharedInstance] updateDeliveredNotificationList];
}

//Called to let your app know which action was selected by the user for a given notification.
- (void)userNotificationCenter:(UNUserNotificationCenter *)center didReceiveNotificationResponse:(UNNotificationResponse *)response withCompletionHandler:(nonnull void(^)(void))completionHandler
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

@end
#endif
