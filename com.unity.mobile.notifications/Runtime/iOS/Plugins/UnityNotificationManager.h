//
//  UnityNotificationManager.h
//  iOS.notifications
//

#if TARGET_OS_IOS

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <UserNotifications/UserNotifications.h>
#import "UnityNotificationData.h"

#define SYSTEM_VERSION_10_OR_ABOVE  ([[[UIDevice currentDevice] systemVersion] compare:@"10.0" options:NSNumericSearch] != NSOrderedAscending)

@interface UnityNotificationManager : NSObject<UNUserNotificationCenterDelegate>

@property UNNotificationSettings* cachedNotificationSettings;
@property struct iOSNotificationAuthorizationData* authData;

@property NotificationDataReceivedResponse onNotificationReceivedCallback;
@property NotificationDataReceivedResponse onCatchReceivedRemoteNotificationCallback;
@property AuthorizationRequestResponse onAuthorizationCompletionCallback;

@property NSArray<UNNotificationRequest *> * cachedPendingNotificationRequests;
@property NSArray<UNNotification *> * cachedDeliveredNotifications;

@property (nonatomic) UNNotification* lastReceivedNotification;

@property BOOL authorized;
@property BOOL authorizationRequestFinished;
@property BOOL needRemoteNotifications;
@property NSString* deviceToken;
@property UNAuthorizationStatus remoteNotificationsRegistered;

@property UNNotificationPresentationOptions remoteNotificationForegroundPresentationOptions;

+ (instancetype)sharedInstance;

+ (struct iOSNotificationData*)UNNotificationRequestToiOSNotificationData:(UNNotificationRequest*)request;
+ (struct iOSNotificationData*)UNNotificationToiOSNotificationData:(UNNotification*)notification;
+ (struct NotificationSettingsData*)UNNotificationSettingsToNotificationSettingsData:(UNNotificationSettings*)settings;

- (void)checkAuthorizationFinished;
- (void)updateScheduledNotificationList;
- (void)updateDeliveredNotificationList;
- (void)updateNotificationSettings;
- (void)requestAuthorization:(NSInteger)authorizationOptions withRegisterRemote:(BOOL)registerRemote;
- (void)setDeviceTokenFromNSData:(NSData*)deviceToken;
@end

#endif
