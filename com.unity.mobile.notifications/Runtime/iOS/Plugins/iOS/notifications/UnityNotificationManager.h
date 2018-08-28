//
//  UnityNotificationManager.h
//  iOS.notifications
//
//  Created by Paulius on 24/07/2018.
//  Copyright © 2018 Unity Technologies. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <UserNotifications/UserNotifications.h>
//
#define SYSTEM_VERSION_10_OR_ABOVE  ([[[UIDevice currentDevice] systemVersion] compare:@"10.0" options:NSNumericSearch] != NSOrderedAscending)

enum triggerType {
    TIME_TRIGGER = 0,
    CALENDAR_TRIGGER = 10,
    LOCATION_TRIGGER = 20,
    PUSH_TRIGGER = 3,
};

typedef struct iOSNotificationData
{
    char* identifier;
    char* title;
    char* body;
    int badge;
    char* subtitle;
    char* categoryIdentifier;
    char* threadIdentifier;
    
    //Custom data
    BOOL showInForeground;
    int showInForegroundPresentationOptions;
    
    // Trigger
    int triggerType;  //0 - time, 1 - calendar, 2 - location, 3 - push.
    BOOL repeats;
    
    //Time trigger
    int timeTriggerInterval;

    //Location trigger
    float locationTriggerCenterX;
    float locationTriggerCenterY;
    float locationTriggerRadius;
    BOOL locationTriggerNotifyOnEntry;
    BOOL locationTriggerNotifyOnExit;
    
    //Calendar trigger
    int calendarTriggerYear;
    int calendarTriggerMonth;
    int calendarTriggerDay;
    int calendarTriggerHour;
    int calendarTriggerMinute;
    int calendarTriggerSecond;

} iOSNotificationData;


typedef struct iOSNotificationAuthorizationData
{
    bool granted;
    char* error;
    bool finished;
    char* deviceToken;
} iOSNotificationAuthorizationData;


typedef void (*NotificationDataReceivedResponse)(struct iOSNotificationData* data);
typedef void (*AuthorizationRequestResponse) (struct iOSNotificationAuthorizationData* data);

typedef struct NotificationSettingsData {
    //    UNAuthorizationStatusNotDetermined
    //    UNAuthorizationStatusDenied
    //    UNAuthorizationStatusAuthorized
    //    UNAuthorizationStatusProvisional
    int authorizationStatus;
    
    // Applies to all :
    //    UNNotificationSettingNotSupported
    //    UNNotificationSettingDisabled
    //    UNNotificationSettingEnabled
    int notificationCenterSetting;
    int lockScreenSetting;
    int carPlaySetting;
    int alertSetting;
    int badgeSetting;
    int soundSetting;
    
} NotificationSettingsData;


@interface UnityNotificationManager : NSObject <UNUserNotificationCenterDelegate>

@property UNNotificationSettings* cachedNotificationSettings;
@property struct iOSNotificationAuthorizationData* authData;

@property NotificationDataReceivedResponse onNotificationReceivedCallback;
@property NotificationDataReceivedResponse onCatchReceivedRemoteNotificationCallback;
@property AuthorizationRequestResponse onAuthorizationCompletionCallback;

@property NSArray<UNNotificationRequest *> * cachedPendingNotificationRequests;
@property NSArray<UNNotification *> * cachedDeliveredNotifications;

@property BOOL authorized;
@property BOOL needRemoteNotifications;
@property NSString* deviceToken;
@property UNAuthorizationStatus remoteNotificationsRegistered;

@property UNNotificationPresentationOptions remoteNotificationForegroundPresentationOptions;


+ (instancetype)sharedInstance;

+ (struct iOSNotificationData*)UNNotificationRequestToiOSNotificationData : (UNNotificationRequest*) request;
+ (struct iOSNotificationData*)UNNotificationToiOSNotificationData : (UNNotification*) notification;
+ (struct NotificationSettingsData*)UNNotificationSettingsToNotificationSettingsData : (UNNotificationSettings*) settings;

- (void)checkAuthorizationFinished;
- (void)updateScheduledNotificationList;
- (void)updateDeliveredNotificationList;
- (void)updateNotificationSettings;
- (void)requestAuthorization: (NSInteger)authorizationOptions : (BOOL) registerRemote;
//- (void)scheduleLocalNotification: (UNMutableNotificationContent*) content;

// UNAuthorizationOptionBadge   = 0
// UNAuthorizationOptionSound   = 1
// UNAuthorizationOptionAlert   = 2
// UNAuthorizationOptionCarPlay = 3
//- (BOOL)hasAuthorizationForNotificationType: (int) type;


@end
