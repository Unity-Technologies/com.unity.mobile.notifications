//
//  UnityNotificationManager.m
//  iOS.notifications
//
//  Created by Paulius on 24/07/2018.
//  Copyright © 2018 Unity Technologies. All rights reserved.
//

#import "UnityNotificationManager.h"

#if defined(UNITY_USES_LOCATION) && UNITY_USES_LOCATION
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
    
    if (!requestRejected && self.needRemoteNotifications)
        if (!self.remoteNotificationsRegistered)
            return;
    
    if (self.authorizationRequestFinished && self.onAuthorizationCompletionCallback != NULL && self.authData != NULL)
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

    registerRemote = true;
    
    self.authorizationRequestFinished = NO;
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    
    BOOL supportsPushNotification = [[[NSBundle mainBundle] objectForInfoDictionaryKey:@"UnityAddRemoteNotificationCapability"] boolValue] ;
    registerRemote = supportsPushNotification == YES ? registerRemote : NO ;
    
    self.needRemoteNotifications = registerRemote;
    [center requestAuthorizationWithOptions: authorizationOptions completionHandler:^(BOOL granted, NSError * _Nullable error)
    {
        struct iOSNotificationAuthorizationData* authData = (struct iOSNotificationAuthorizationData*)malloc(sizeof(*authData));
        authData -> finished = YES;
        authData -> granted = granted;
        authData -> error =  [[error localizedDescription]cStringUsingEncoding:NSUTF8StringEncoding];
        authData -> deviceToken = "";
        
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
            if (presentationOptions == 0)
                presentationOptions = kDefaultPresentationOptions;
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
-(void)userNotificationCenter:(UNUserNotificationCenter *)center didReceiveNotificationResponse:(UNNotificationResponse *)response withCompletionHandler:(nonnull void(^)(void))completionHandler
{    
        self.lastReceivedNotification = response.notification; 
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
    settingsData->alertStyle = (int)settings.alertStyle;
    
    if (@available(iOS 11.0, *)) {
        settingsData->showPreviewsSetting = (int)settings.showPreviewsSetting;
    } else {
        settingsData->showPreviewsSetting = 2;
    }
    return settingsData;
}

+ (struct iOSNotificationData*)UNNotificationRequestToiOSNotificationData : (UNNotificationRequest*) request
{
    struct iOSNotificationData* notificationData = (struct iOSNotificationData*)malloc(sizeof(*notificationData));
    [UnityNotificationManager InitiOSNotificationData : notificationData];

    UNNotificationContent* content = request.content;
    
    notificationData -> identifier = (char*) [request.identifier UTF8String];
    
    if (content.title != nil && content.title.length > 0)
        notificationData -> title = (char*) [content.title  UTF8String];
    
    if (content.body != nil && content.body.length > 0)
        notificationData -> body = (char*) [content.body UTF8String];
    
    notificationData -> badge = [content.badge intValue];
    
    if (content.subtitle != nil && content.subtitle.length > 0)
        notificationData -> subtitle = (char*) [content.subtitle  UTF8String];
    
    if (content.categoryIdentifier != nil && content.categoryIdentifier.length > 0)
        notificationData -> categoryIdentifier = (char*) [content.categoryIdentifier  UTF8String];

    if (content.threadIdentifier != nil && content.threadIdentifier.length > 0)
        notificationData -> threadIdentifier = (char*) [content.threadIdentifier  UTF8String];
    
    if ([ request.trigger isKindOfClass:[UNTimeIntervalNotificationTrigger class]])
    {
        notificationData -> triggerType = TIME_TRIGGER;
        
        UNTimeIntervalNotificationTrigger* timeTrigger = (UNTimeIntervalNotificationTrigger*) request.trigger;
        notificationData -> timeTriggerInterval = timeTrigger.timeInterval;
        notificationData -> repeats = timeTrigger.repeats;
    }
    else if ([ request.trigger isKindOfClass:[UNCalendarNotificationTrigger class]])
    {
        notificationData -> triggerType = CALENDAR_TRIGGER;
        
        UNCalendarNotificationTrigger* calendarTrigger = (UNCalendarNotificationTrigger*) request.trigger;
        NSDateComponents* date = calendarTrigger.dateComponents;
        
        notificationData->calendarTriggerYear = (int)date.year;
        notificationData->calendarTriggerMonth = (int)date.month;
        notificationData->calendarTriggerDay = (int)date.day;
        notificationData->calendarTriggerHour = (int)date.hour;
        notificationData->calendarTriggerMinute = (int)date.minute;
        notificationData->calendarTriggerSecond = (int)date.second;
        
    }
    else if ([ request.trigger isKindOfClass:[UNLocationNotificationTrigger class]])
    {
#if defined(UNITY_USES_LOCATION) && UNITY_USES_LOCATION
        notificationData -> triggerType = LOCATION_TRIGGER;

        UNLocationNotificationTrigger* locationTrigger = (UNLocationNotificationTrigger*) request.trigger;
        CLCircularRegion *region = (CLCircularRegion*)locationTrigger.region;

        notificationData -> locationTriggerCenterX = region.center.latitude ;
        notificationData -> locationTriggerCenterY = region.center.longitude;
        notificationData -> locationTriggerRadius = region.radius;
        notificationData -> locationTriggerNotifyOnExit = region.notifyOnEntry;
        notificationData -> locationTriggerNotifyOnEntry = region.notifyOnExit;
#endif
    }
    else if ([ request.trigger isKindOfClass:[UNPushNotificationTrigger class]])
    {
        notificationData -> triggerType = PUSH_TRIGGER;
    }
    
    if ([NSJSONSerialization isValidJSONObject: [request.content.userInfo objectForKey:@"data"]]) {
        NSError *error;
        NSData *data = [NSJSONSerialization dataWithJSONObject:[request.content.userInfo objectForKey:@"data"]
                                                       options:NSJSONWritingPrettyPrinted
                                                         error:&error];
        if (! data) {
            NSLog(@"Failed parsing notification userInfo[\"data\"]: %@", error);
        } else {
            notificationData -> data = (char*)[[[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding] UTF8String];
        }
    }
    else
    {
        if ([[request.content.userInfo valueForKey:@"data"] isKindOfClass:[NSNumber class]] )
        {
            NSNumber *value = (NSNumber*)[request.content.userInfo valueForKey:@"data"];
            
            if (CFBooleanGetTypeID() == CFGetTypeID((__bridge CFTypeRef)(value))) {
                notificationData -> data = (value == 1) ? "true" : "false";
            }
            else {
                notificationData -> data = (char*)[[value description] UTF8String];
            }
        }
        else {
            notificationData -> data = (char*) [[[request.content.userInfo objectForKey:@"data"]description] UTF8String];
        }
    }
    
    return notificationData;
}

+ (struct iOSNotificationData*)UNNotificationToiOSNotificationData : (UNNotification*) notification
{
    UNNotificationRequest* request = notification.request;
    return [UnityNotificationManager UNNotificationRequestToiOSNotificationData : request];
}

+ (void)InitiOSNotificationData : (iOSNotificationData*) notificationData;
{
    notificationData -> title = " ";
    notificationData -> body = " ";
    notificationData -> badge = 0;
    notificationData -> subtitle = " ";
    notificationData -> categoryIdentifier = " ";
    notificationData -> threadIdentifier = " ";
    notificationData -> triggerType = PUSH_TRIGGER;
    notificationData -> data = " ";
}

@end
