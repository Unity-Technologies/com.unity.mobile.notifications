//
//  UnityAppController+Notifications.m
//  iOS.notifications
//

#if TARGET_OS_IOS

#import <objc/runtime.h>

#import "UnityNotificationManager.h"

#if UNITY_XCODE_PROJECT_TYPE_SWIFT
extern "C"
{
    NSNotificationName UnityMobileNotifications_applicationWillFinishLaunchingName();
    NSNotificationName UnityMobileNotifications_applicationDidRegisterForRemoteNotificationsName();
    NSNotificationName UnityMobileNotifications_applicationDidFailToRegisterForRemoteNotificationsName();
    NSString* UnityMobileNotifications_remoteNotificationsDeviceTokenKey();
}
#define kUnityWillFinishLaunchingWithOptions UnityMobileNotifications_applicationWillFinishLaunchingName()
#define kUnityDidRegisterForRemoteNotificationsWithDeviceToken UnityMobileNotifications_applicationDidRegisterForRemoteNotificationsName()
#define kUnityDidFailToRegisterForRemoteNotificationsWithError UnityMobileNotifications_applicationDidFailToRegisterForRemoteNotificationsName()
#else
#import "Classes/PluginBase/AppDelegateListener.h"
#endif

@interface UnityNotificationLifeCycleManager : NSObject

@end

@implementation UnityNotificationLifeCycleManager

+ (void)load
{
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        NSNotificationCenter *nc = [NSNotificationCenter defaultCenter];

        [nc addObserverForName: UIApplicationDidBecomeActiveNotification
         object: nil
         queue: [NSOperationQueue mainQueue]
         usingBlock:^(NSNotification *notification) {
             UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
             [manager updateScheduledNotificationList];
             [manager updateDeliveredNotificationList];
             [manager updateNotificationSettings];
         }];

        [nc addObserverForName: UIApplicationDidEnterBackgroundNotification
         object: nil
         queue: [NSOperationQueue mainQueue]
         usingBlock:^(NSNotification *notification) {
             UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
             manager.launchedWithNotification = NO;
             manager.lastReceivedNotification = NULL;
         }];

        [nc addObserverForName: kUnityWillFinishLaunchingWithOptions
         object: nil
         queue: [NSOperationQueue mainQueue]
         usingBlock:^(NSNotification *notification) {
             UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
             [UNUserNotificationCenter currentNotificationCenter].delegate = manager;
             NSBundle* mainBundle = [NSBundle mainBundle];
             BOOL authorizeOnLaunch = [[mainBundle objectForInfoDictionaryKey: @"UnityNotificationRequestAuthorizationOnAppLaunch"] boolValue];
             BOOL supportsPushNotification = [[mainBundle objectForInfoDictionaryKey: @"UnityAddRemoteNotificationCapability"] boolValue];
             BOOL registerRemoteOnLaunch = supportsPushNotification == YES ?
                 [[mainBundle objectForInfoDictionaryKey: @"UnityNotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch"] boolValue] : NO;

             NSInteger defaultAuthorizationOptions = [[mainBundle objectForInfoDictionaryKey: @"UnityNotificationDefaultAuthorizationOptions"] integerValue];

             if (defaultAuthorizationOptions <= 0)
                 defaultAuthorizationOptions = (UNAuthorizationOptionSound + UNAuthorizationOptionAlert + UNAuthorizationOptionBadge);

             if (notification != nil && notification.userInfo != nil)
                 for (NSString* key in notification.userInfo)
                     if ([key isEqual: @"UIApplicationLaunchOptionsLocalNotificationKey"]
                         || [key isEqual: @"UIApplicationLaunchOptionsRemoteNotificationKey"])
                     {
                         manager.launchedWithNotification = YES;
                         break;
                     }

             if (authorizeOnLaunch)
                 [manager requestAuthorization: defaultAuthorizationOptions withRegisterRemote: registerRemoteOnLaunch forRequest: NULL];
         }];

        [nc addObserverForName: kUnityDidRegisterForRemoteNotificationsWithDeviceToken
         object: nil
         queue: [NSOperationQueue mainQueue]
         usingBlock:^(NSNotification *notification) {
             NSLog(@"didRegisterForRemoteNotificationsWithDeviceToken");
             UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
             [manager finishRemoteNotificationRegistration: UNAuthorizationStatusAuthorized deviceToken: [UnityNotificationLifeCycleManager deviceTokenFromNotification: notification]];
         }];

        [nc addObserverForName: kUnityDidFailToRegisterForRemoteNotificationsWithError
         object: nil
         queue: [NSOperationQueue mainQueue]
         usingBlock:^(NSNotification *notification) {
             NSLog(@"didFailToRegisterForRemoteNotificationsWithError");
             UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
             [manager finishRemoteNotificationRegistration: UNAuthorizationStatusDenied deviceToken: nil];
         }];
    });
}

+ (NSData*)deviceTokenFromNotification:(NSNotification*)notification
{
    NSData* deviceToken = nil;

#if UNITY_XCODE_PROJECT_TYPE_SWIFT
    id token = [notification.userInfo objectForKey: UnityMobileNotifications_remoteNotificationsDeviceTokenKey()];
#else
    id token = notification.userInfo;
#endif

    if ([token isKindOfClass: NSData.class])
        deviceToken = token;

    return deviceToken;
}

@end
#endif
