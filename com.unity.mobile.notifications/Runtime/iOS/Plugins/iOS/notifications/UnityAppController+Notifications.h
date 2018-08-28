//
//  UnityAppController+Notifications.h
//  Unity-iPhone
//
//  Created by Paulius on 07/08/2018.
//

#import "UnityAppController.h"

#include "Classes/PluginBase/LifeCycleListener.h"
#include "Classes/PluginBase/AppDelegateListener.h"


@interface UnityAppController (Notifications)

@end

@interface UnityNotificationLifeCycleManager  : NSObject//<LifeCycleListener, AppDelegateListener>

+ (instancetype)sharedInstance;

@end

