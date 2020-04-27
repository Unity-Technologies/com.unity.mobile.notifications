//
//  UnityAppController+Notifications.h
//  iOS.notifications
//

#if TARGET_OS_IOS
#import "UnityAppController.h"

#include "Classes/PluginBase/LifeCycleListener.h"
#include "Classes/PluginBase/AppDelegateListener.h"

@interface UnityAppController (Notifications)

@end

@interface UnityNotificationLifeCycleManager : NSObject

+ (instancetype)sharedInstance;

@end

#endif
