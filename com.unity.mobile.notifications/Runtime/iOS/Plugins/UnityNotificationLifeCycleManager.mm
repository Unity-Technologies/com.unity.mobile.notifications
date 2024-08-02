//
//  UnityAppController+Notifications.m
//  iOS.notifications
//

#if TARGET_OS_IOS

#import <objc/runtime.h>

#import "UnityNotificationManager.h"
#import "Classes/PluginBase/AppDelegateListener.h"

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
             [manager finishRemoteNotificationRegistration: UNAuthorizationStatusAuthorized notification: notification];
         }];

        [nc addObserverForName: kUnityDidFailToRegisterForRemoteNotificationsWithError
         object: nil
         queue: [NSOperationQueue mainQueue]
         usingBlock:^(NSNotification *notification) {
             NSLog(@"didFailToRegisterForRemoteNotificationsWithError");
             UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
             [manager finishRemoteNotificationRegistration: UNAuthorizationStatusDenied notification: notification];
         }];
    });
}

@end
#endif
