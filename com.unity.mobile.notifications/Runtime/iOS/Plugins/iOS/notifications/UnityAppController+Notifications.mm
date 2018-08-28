//
//  UnityAppController+Notifications.m
//  Unity-iPhone
//
//  Created by Paulius on 07/08/2018.
//

#import <objc/runtime.h>

#import "UnityNotificationManager.h"
#import "UnityAppController+Notifications.h"

@implementation UnityNotificationLifeCycleManager


+ (void)load {
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        [UnityNotificationLifeCycleManager sharedInstance];
    });
}

+ (instancetype)sharedInstance;
{
    static UnityNotificationLifeCycleManager *sharedInstance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedInstance = [[UnityNotificationLifeCycleManager alloc] init];
        NSNotificationCenter *nc = [NSNotificationCenter defaultCenter];
        
        [nc addObserverForName:UIApplicationDidFinishLaunchingNotification
                        object:nil
                         queue:[NSOperationQueue mainQueue]
                    usingBlock:^(NSNotification *notification) {
                        
                        BOOL authorizeOnLaunch = [[[NSBundle mainBundle] objectForInfoDictionaryKey:@"UnityNotificationRequestAuthorizationOnAppLaunch"] boolValue] ;
                        
                        BOOL registerRemoteOnLaunch = [[[NSBundle mainBundle] objectForInfoDictionaryKey:@"UnityNotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch"] boolValue] ;
                        
                        NSInteger remoteForegroundPresentationOptions = [[[NSBundle mainBundle] objectForInfoDictionaryKey:@"UnityRemoteNotificationForegroundPresentationOptions"] integerValue] ;
                        
                        NSInteger defaultPresentationOptions = [[[NSBundle mainBundle] objectForInfoDictionaryKey:@"UnityNotificationDefaultPresentationOptions"] integerValue] ;
                        
                        if (authorizeOnLaunch)
                        {
                            UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
                            [manager requestAuthorization:defaultPresentationOptions : registerRemoteOnLaunch];
                            manager.remoteNotificationForegroundPresentationOptions = remoteForegroundPresentationOptions;
                        }

                    }];
        
        [nc addObserverForName:kUnityDidRegisterForRemoteNotificationsWithDeviceToken
                        object:nil
                         queue:[NSOperationQueue mainQueue]
                    usingBlock:^(NSNotification *notification) {
                        NSLog(@"didRegisterForRemoteNotificationsWithDeviceToken");
                        UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
                        manager.remoteNotificationsRegistered = UNAuthorizationStatusDenied;
                        manager.deviceToken = [NSString stringWithFormat:@"%@",notification.userInfo];
                        [manager checkAuthorizationFinished];

                    }];

        [nc addObserverForName:kUnityDidFailToRegisterForRemoteNotificationsWithError
                        object:nil
                         queue:[NSOperationQueue mainQueue]
                    usingBlock:^(NSNotification *notification) {
                        NSLog(@"didFailToRegisterForRemoteNotificationsWithError");
                        UnityNotificationManager* manager = [UnityNotificationManager sharedInstance];
                        manager.remoteNotificationsRegistered = UNAuthorizationStatusAuthorized;
                        [manager checkAuthorizationFinished];

                    }];


    });
    return sharedInstance;
}
@end
