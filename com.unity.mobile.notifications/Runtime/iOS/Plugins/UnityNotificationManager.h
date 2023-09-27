//
//  UnityNotificationManager.h
//  iOS.notifications
//

#if TARGET_OS_IOS

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <UserNotifications/UserNotifications.h>
#import "UnityNotificationData.h"

@interface UnityNotificationManager : NSObject<UNUserNotificationCenterDelegate>

@property UNNotificationSettings* cachedNotificationSettings;

@property NotificationDataReceivedResponse onNotificationReceivedCallback;
@property NotificationDataReceivedResponse onRemoteNotificationReceivedCallback;
@property AuthorizationRequestResponse onAuthorizationCompletionCallback;

@property NSArray<UNNotificationRequest *> * cachedPendingNotificationRequests;
@property NSArray<UNNotification *> * cachedDeliveredNotifications;

@property (nonatomic) UNNotification* lastReceivedNotification;
@property NSString* lastRespondedNotificationAction;
@property NSString* lastRespondedNotificationUserText;

+ (instancetype)sharedInstance;

- (id)init;
- (void)finishAuthorization:(struct iOSNotificationAuthorizationData*)authData forRequest:(void*)request;
- (void)finishRemoteNotificationRegistration:(UNAuthorizationStatus)status notification:(NSNotification*)notification;
- (void)updateScheduledNotificationList;
- (void)updateDeliveredNotificationList;
- (void)updateNotificationSettings;
- (void)requestAuthorization:(NSInteger)authorizationOptions withRegisterRemote:(BOOL)registerRemote forRequest:(void*)request;
- (void)unregisterForRemoteNotifications;
- (void)scheduleLocalNotification:(iOSNotificationData*)data;

@end

#endif
