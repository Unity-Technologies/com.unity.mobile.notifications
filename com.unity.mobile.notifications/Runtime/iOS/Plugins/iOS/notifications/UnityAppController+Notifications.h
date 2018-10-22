//
//  UnityAppController+Notifications.h
//  Unity-iPhone
//
//  Copyright © 2018 Unity Technologies. All rights reserved.
//

#import "UnityAppController.h"

#include "Classes/PluginBase/LifeCycleListener.h"
#include "Classes/PluginBase/AppDelegateListener.h"


@interface UnityAppController (Notifications)

@end

@interface UnityNotificationLifeCycleManager  : NSObject

+ (instancetype)sharedInstance;

@end

