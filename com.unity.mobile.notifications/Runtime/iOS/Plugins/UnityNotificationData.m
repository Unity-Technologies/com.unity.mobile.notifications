//
//  UnityNotificationData.m
//  iOS.notifications
//

#if TARGET_OS_IOS

#import <Foundation/Foundation.h>
#import <UserNotifications/UserNotifications.h>
#if defined(UNITY_USES_LOCATION) && UNITY_USES_LOCATION
#import <CoreLocation/CoreLocation.h>
#endif

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

void initiOSNotificationData(iOSNotificationData* notificationData)
{
    notificationData->title = " ";
    notificationData->body = " ";
    notificationData->badge = 0;
    notificationData->subtitle = " ";
    notificationData->categoryIdentifier = " ";
    notificationData->threadIdentifier = " ";
    notificationData->triggerType = PUSH_TRIGGER;
    notificationData->data = " ";
}

iOSNotificationData* UNNotificationRequestToiOSNotificationData(UNNotificationRequest* request)
{
    struct iOSNotificationData* notificationData = malloc(sizeof(*notificationData));
    initiOSNotificationData(notificationData);

    UNNotificationContent* content = request.content;

    notificationData->identifier = (char*)[request.identifier UTF8String];

    if (content.title != nil && content.title.length > 0)
        notificationData->title = (char*)[content.title  UTF8String];

    if (content.body != nil && content.body.length > 0)
        notificationData->body = (char*)[content.body UTF8String];

    notificationData->badge = [content.badge intValue];

    if (content.subtitle != nil && content.subtitle.length > 0)
        notificationData->subtitle = (char*)[content.subtitle  UTF8String];

    if (content.categoryIdentifier != nil && content.categoryIdentifier.length > 0)
        notificationData->categoryIdentifier = (char*)[content.categoryIdentifier  UTF8String];

    if (content.threadIdentifier != nil && content.threadIdentifier.length > 0)
        notificationData->threadIdentifier = (char*)[content.threadIdentifier  UTF8String];

    if ([request.trigger isKindOfClass: [UNTimeIntervalNotificationTrigger class]])
    {
        notificationData->triggerType = TIME_TRIGGER;

        UNTimeIntervalNotificationTrigger* timeTrigger = (UNTimeIntervalNotificationTrigger*)request.trigger;
        notificationData->timeTriggerInterval = timeTrigger.timeInterval;
        notificationData->repeats = timeTrigger.repeats;
    }
    else if ([request.trigger isKindOfClass: [UNCalendarNotificationTrigger class]])
    {
        notificationData->triggerType = CALENDAR_TRIGGER;

        UNCalendarNotificationTrigger* calendarTrigger = (UNCalendarNotificationTrigger*)request.trigger;
        NSDateComponents* date = calendarTrigger.dateComponents;

        notificationData->calendarTriggerYear = (int)date.year;
        notificationData->calendarTriggerMonth = (int)date.month;
        notificationData->calendarTriggerDay = (int)date.day;
        notificationData->calendarTriggerHour = (int)date.hour;
        notificationData->calendarTriggerMinute = (int)date.minute;
        notificationData->calendarTriggerSecond = (int)date.second;
    }
    else if ([request.trigger isKindOfClass: [UNLocationNotificationTrigger class]])
    {
#if defined(UNITY_USES_LOCATION) && UNITY_USES_LOCATION
        notificationData->triggerType = LOCATION_TRIGGER;

        UNLocationNotificationTrigger* locationTrigger = (UNLocationNotificationTrigger*)request.trigger;
        CLCircularRegion *region = (CLCircularRegion*)locationTrigger.region;

        notificationData->locationTriggerCenterX = region.center.latitude;
        notificationData->locationTriggerCenterY = region.center.longitude;
        notificationData->locationTriggerRadius = region.radius;
        notificationData->locationTriggerNotifyOnExit = region.notifyOnEntry;
        notificationData->locationTriggerNotifyOnEntry = region.notifyOnExit;
#endif
    }
    else if ([request.trigger isKindOfClass: [UNPushNotificationTrigger class]])
    {
        notificationData->triggerType = PUSH_TRIGGER;
    }

    if ([NSJSONSerialization isValidJSONObject: [request.content.userInfo objectForKey: @"data"]])
    {
        NSError *error;
        NSData *data = [NSJSONSerialization dataWithJSONObject: [request.content.userInfo objectForKey: @"data"]
                        options: NSJSONWritingPrettyPrinted
                        error: &error];
        if (!data)
        {
            NSLog(@"Failed parsing notification userInfo[\"data\"]: %@", error);
        }
        else
        {
            notificationData->data = (char*)[[[NSString alloc] initWithData: data encoding: NSUTF8StringEncoding] UTF8String];
        }
    }
    else
    {
        if ([[request.content.userInfo valueForKey: @"data"] isKindOfClass: [NSNumber class]])
        {
            NSNumber *value = (NSNumber*)[request.content.userInfo valueForKey: @"data"];

            if (CFBooleanGetTypeID() == CFGetTypeID((__bridge CFTypeRef)(value)))
            {
                notificationData->data = (value.intValue == 1) ? "true" : "false";
            }
            else
            {
                notificationData->data = (char*)[[value description] UTF8String];
            }
        }
        else
        {
            notificationData->data = (char*)[[[request.content.userInfo objectForKey: @"data"]description] UTF8String];
        }
    }

    return notificationData;
}

#endif
