//
//  UnityNotificationData.m
//  iOS.notifications
//

#if TARGET_OS_IOS

#import <Foundation/Foundation.h>
#import <UserNotifications/UserNotifications.h>

#import "UnityNotificationData.h"

NotificationSettingsData* UNNotificationSettingsToNotificationSettingsData(UNNotificationSettings* settings)
{
    struct NotificationSettingsData* settingsData = (struct NotificationSettingsData*)malloc(sizeof(*settingsData));

    settingsData->alertSetting = (int)settings.alertSetting;
    settingsData->authorizationStatus = (int)settings.authorizationStatus;
    settingsData->badgeSetting = (int)settings.badgeSetting;
    settingsData->carPlaySetting = (int)settings.carPlaySetting;
    settingsData->lockScreenSetting = (int)settings.lockScreenSetting;
    settingsData->notificationCenterSetting = (int)settings.notificationCenterSetting;
    settingsData->soundSetting = (int)settings.soundSetting;
    settingsData->alertStyle = (int)settings.alertStyle;

    if (@available(iOS 11.0, *))
    {
        settingsData->showPreviewsSetting = (int)settings.showPreviewsSetting;
    }
    else
    {
        settingsData->showPreviewsSetting = 2;
    }
    return settingsData;
}

#endif
