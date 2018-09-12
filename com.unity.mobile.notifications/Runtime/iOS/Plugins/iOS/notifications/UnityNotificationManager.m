//
//  UnityNotificationManager.m
//  iOS.notifications
//
//  Created by Paulius on 24/07/2018.
//  Copyright © 2018 Unity Technologies. All rights reserved.
//

#import "UnityNotificationManager.h"

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
    
    if (self.needRemoteNotifications)
        if (!self.remoteNotificationsRegistered)
            return;
    
    if (self.authorized && self.onAuthorizationCompletionCallback != NULL && self.authData != NULL)
    {
        self.authData -> deviceToken = [self.deviceToken UTF8String];
        self.onAuthorizationCompletionCallback(self.authData);
        
        free(self.authData);
        self.authData = NULL;
    }
}

- (void)requestAuthorization: (NSInteger)authorizationOptions : (BOOL) registerRemote
{
    if ( !SYSTEM_VERSION_10_OR_ABOVE)
        return;

    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    center.delegate = self;
    
    self.needRemoteNotifications = registerRemote;
    
    [center requestAuthorizationWithOptions: authorizationOptions                                                                                                                   completionHandler:^(BOOL granted, NSError * _Nullable error) {                                                                                                                        // Enable or disable features based on authorization.
        struct iOSNotificationAuthorizationData* authData = (struct iOSNotificationAuthorizationData*)malloc(sizeof(*authData));
        authData -> finished = YES;
        authData -> granted = granted;
        authData -> error =  [[error localizedDescription]cStringUsingEncoding:NSUTF8StringEncoding];
        authData -> deviceToken = "";
        
        self.authData = authData;

        if (granted)
        {
            self.authorized = TRUE;
            if (registerRemote)
            {
                dispatch_async(dispatch_get_main_queue(), ^{
                    [[UIApplication sharedApplication] registerForRemoteNotifications];
                });
            }
        }
        [self checkAuthorizationFinished];
        [self updateNotificationSettings];
    }];
}

//Called when a notification is delivered to a foreground app.
-(void)userNotificationCenter:(UNUserNotificationCenter *)center willPresentNotification:(UNNotification *)notification withCompletionHandler:(void (^)(UNNotificationPresentationOptions options))completionHandler{
    
    if (self.onNotificationReceivedCallback != NULL )
        self.onNotificationReceivedCallback([UnityNotificationManager UNNotificationToiOSNotificationData:notification]);
        
    BOOL showInForeground;
    NSInteger presentationOptions;
    
    if ( [notification.request.trigger isKindOfClass:[UNPushNotificationTrigger class]])
    {
        if (self.onCatchReceivedRemoteNotificationCallback != NULL)
        {
            showInForeground = NO;
            self.onCatchReceivedRemoteNotificationCallback([UnityNotificationManager UNNotificationToiOSNotificationData:notification]);
        }
        else
        {
            showInForeground = YES;
            presentationOptions = self.remoteNotificationForegroundPresentationOptions;
        }
    }
    else
    {
        presentationOptions = [[notification.request.content.userInfo objectForKey:@"showInForegroundPresentationOptions"] intValue];
        showInForeground = [[notification.request.content.userInfo objectForKey:@"showInForeground"] boolValue];

    }
    if (showInForeground)
    {
        completionHandler(presentationOptions);
    }
    else
        completionHandler(UNNotificationPresentationOptionNone);
    
    [[UnityNotificationManager sharedInstance] updateDeliveredNotificationList];
}

//Called to let your app know which action was selected by the user for a given notification.
-(void)userNotificationCenter:(UNUserNotificationCenter *)center didReceiveNotificationResponse:(UNNotificationResponse *)response withCompletionHandler:(void(^)())completionHandler{
    
    completionHandler();
    [[UnityNotificationManager sharedInstance] updateDeliveredNotificationList];
    }

-(void)updateScheduledNotificationList
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center getPendingNotificationRequestsWithCompletionHandler:^(NSArray<UNNotificationRequest *> * _Nonnull requests) {
        self.cachedPendingNotificationRequests = requests;
    }];
}

-(void)updateDeliveredNotificationList
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

//  Helper stuff

+ (struct NotificationSettingsData*)UNNotificationSettingsToNotificationSettingsData : (UNNotificationSettings*) settings
{
    struct NotificationSettingsData* settingsData = (struct NotificationSettingsData*)malloc(sizeof(*settingsData));
    settingsData->alertSetting = (int) settings.alertSetting;
    settingsData->authorizationStatus = (int) settings.authorizationStatus;
    settingsData->badgeSetting = (int) settings.badgeSetting;
    settingsData->carPlaySetting = (int) settings.carPlaySetting;
    settingsData->lockScreenSetting = (int) settings.lockScreenSetting;
    settingsData->notificationCenterSetting = (int) settings.notificationCenterSetting;
    settingsData->soundSetting = (int) settings.soundSetting;
    return settingsData;
}

+ (struct iOSNotificationData*)UNNotificationRequestToiOSNotificationData : (UNNotificationRequest*) request
{
    struct iOSNotificationData* notificationData = (struct iOSNotificationData*)malloc(sizeof(*notificationData));

    UNNotificationContent* content = request.content;
    
    notificationData -> identifier = [request.identifier UTF8String];
    
    if (content.title != nil && content.title.length > 0)
        notificationData -> title = [content.title  UTF8String];
    else
        notificationData -> title = " ";
    
    if (content.body.length != nil && content.body.length > 0)
        notificationData -> body = [content.body UTF8String];
    else
        notificationData -> body = " ";
    
    notificationData -> badge = content.badge;
    
    if (content.subtitle != nil && content.subtitle.length > 0)
        notificationData -> subtitle = [content.subtitle  UTF8String];
    else
        notificationData -> subtitle = " ";
    
    if (content.categoryIdentifier != nil && content.categoryIdentifier.length > 0)
        notificationData -> categoryIdentifier = [content.categoryIdentifier  UTF8String];
    else
        notificationData -> categoryIdentifier = " ";

    if (content.threadIdentifier != nil && content.threadIdentifier.length > 0)
        notificationData -> threadIdentifier = [content.threadIdentifier  UTF8String];
    else
        notificationData -> threadIdentifier = " ";
    
     //0 - time, 1 - calendar, 2 - location, 3 - push.
    if ([ request.trigger isKindOfClass:[UNTimeIntervalNotificationTrigger class]])
    {
        notificationData -> triggerType = TIME_TRIGGER;
        notificationData -> timeTriggerInterval = 0;
        notificationData -> repeats = request.trigger.repeats;
    }
    else if ([ request.trigger isKindOfClass:[UNCalendarNotificationTrigger class]])
    {
        UNLocationNotificationTrigger* locTrigger = (UNLocationNotificationTrigger*) request.trigger;
        notificationData -> triggerType = CALENDAR_TRIGGER;
        notificationData -> locationTriggerCenterX = 0;
        notificationData -> locationTriggerCenterY = 0;
        notificationData -> locationTriggerRadius = 0;
        notificationData -> locationTriggerNotifyOnExit = 0;
        notificationData -> locationTriggerNotifyOnEntry = 0;
    }
    else if ([ request.trigger isKindOfClass:[UNLocationNotificationTrigger class]])
    {
        notificationData -> triggerType = LOCATION_TRIGGER;
    }
    else if ([ request.trigger isKindOfClass:[UNPushNotificationTrigger class]])
    {
        notificationData -> triggerType = PUSH_TRIGGER;
    }
              
    return notificationData;
}

+ (struct iOSNotificationData*)UNNotificationToiOSNotificationData : (UNNotification*) notification
{
    UNNotificationRequest* request = notification.request;
    
    return [UnityNotificationManager UNNotificationRequestToiOSNotificationData : request];
}
@end
