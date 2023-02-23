//
//  UnityNotificationData.m
//  iOS.notifications
//

#if TARGET_OS_IOS

#import <Foundation/Foundation.h>
#import <UserNotifications/UserNotifications.h>
#if UNITY_USES_LOCATION
#import <CoreLocation/CoreLocation.h>
#endif

#import "UnityNotificationData.h"


static NSString* ParseNotificationDataObject(id obj)
{
    if ([obj isKindOfClass: [NSString class]])
        return obj;
    else if ([obj isKindOfClass: [NSNumber class]])
    {
        NSNumber* numberVal = obj;
        if (CFBooleanGetTypeID() == CFGetTypeID((__bridge CFTypeRef)(obj)))
            return numberVal.boolValue ? @"true" : @"false";
        return numberVal.stringValue;
    }
    else if ([NSJSONSerialization isValidJSONObject: obj])
    {
        NSError* error;
        NSData* data = [NSJSONSerialization dataWithJSONObject: obj options: NSJSONWritingPrettyPrinted error: &error];
        if (data)
        {
            NSString* v = [[NSString alloc] initWithData: data encoding: NSUTF8StringEncoding];
            return v;
        }
        else
        {
            NSLog(@"Failed parsing notification userInfo value: %@", error);
        }
    }
    else
        NSLog(@"Failed parsing notification userInfo value");

    NSObject* o = obj;
    return o.description;
}

NotificationSettingsData UNNotificationSettingsToNotificationSettingsData(UNNotificationSettings* settings)
{
    NotificationSettingsData settingsData;

    settingsData.alertSetting = (int)settings.alertSetting;
    settingsData.authorizationStatus = (int)settings.authorizationStatus;
    settingsData.badgeSetting = (int)settings.badgeSetting;
    settingsData.carPlaySetting = (int)settings.carPlaySetting;
    settingsData.lockScreenSetting = (int)settings.lockScreenSetting;
    settingsData.notificationCenterSetting = (int)settings.notificationCenterSetting;
    settingsData.soundSetting = (int)settings.soundSetting;
    settingsData.alertStyle = (int)settings.alertStyle;

    if (@available(iOS 11.0, *))
    {
        settingsData.showPreviewsSetting = (int)settings.showPreviewsSetting;
    }
    else
    {
        settingsData.showPreviewsSetting = 2;
    }
    return settingsData;
}

void initiOSNotificationData(iOSNotificationData* notificationData)
{
    notificationData->title = NULL;
    notificationData->body = NULL;
    notificationData->badge = 0;
    notificationData->subtitle = NULL;
    notificationData->categoryIdentifier = NULL;
    notificationData->threadIdentifier = NULL;
    notificationData->soundType = kSoundTypeDefault;
    notificationData->soundVolume = -1.0f;
    notificationData->soundName = NULL;
    notificationData->interruptionLevel = kInterruptionLevelActive;
    notificationData->relevanceScore = 0;
    notificationData->triggerType = PUSH_TRIGGER;
    notificationData->userInfo = NULL;
}

static enum UnityNotificationInterruptionLevel InterruptionLevelToUnity(UNNotificationInterruptionLevel level)
API_AVAILABLE(ios(15.0))
{
    switch (level)
    {
        case UNNotificationInterruptionLevelActive:
        default:
            return kInterruptionLevelActive;
        case UNNotificationInterruptionLevelCritical:
            return kInterruptionLevelCritical;
        case UNNotificationInterruptionLevelPassive:
            return kInterruptionLevelPassive;
        case UNNotificationInterruptionLevelTimeSensitive:
            return kInterruptionLevelTimeSensitive;
    }
}

static void parseCustomizedData(iOSNotificationData* notificationData, UNNotificationRequest* request)
{
    NSDictionary* userInfo = request.content.userInfo;
    NSObject* customizedData = [userInfo objectForKey: @"data"];

    // For local notifications, the customzied data is always a string.
    if (notificationData->triggerType == TIME_TRIGGER || notificationData->triggerType == CALENDAR_TRIGGER || customizedData == nil)
    {
        notificationData->userInfo = (__bridge_retained void*)userInfo;
        return;
    }

    // For push notifications, we have to handle more cases.
    NSString* strData = ParseNotificationDataObject(customizedData);
    if (strData == nil)
        NSLog(@"Failed parsing notification userInfo[\"data\"]");

    NSMutableDictionary* parsedUserInfo = [NSMutableDictionary dictionaryWithDictionary: userInfo];
    [parsedUserInfo setValue: strData forKey: @"data"];
    notificationData->userInfo = (__bridge_retained void*)parsedUserInfo;
}

iOSNotificationData UNNotificationRequestToiOSNotificationData(UNNotificationRequest* request)
{
    iOSNotificationData notificationData;
    initiOSNotificationData(&notificationData);

    UNNotificationContent* content = request.content;

    notificationData.identifier = strdup([request.identifier UTF8String]);

    if (content.title != nil && content.title.length > 0)
        notificationData.title = strdup([content.title UTF8String]);

    if (content.body != nil && content.body.length > 0)
        notificationData.body = strdup([content.body UTF8String]);

    notificationData.badge = [content.badge intValue];

    if (content.subtitle != nil && content.subtitle.length > 0)
        notificationData.subtitle = strdup([content.subtitle UTF8String]);

    if (content.categoryIdentifier != nil && content.categoryIdentifier.length > 0)
        notificationData.categoryIdentifier = strdup([content.categoryIdentifier UTF8String]);

    if (content.threadIdentifier != nil && content.threadIdentifier.length > 0)
        notificationData.threadIdentifier = strdup([content.threadIdentifier UTF8String]);

    if (@available(iOS 15.0, *))
    {
        notificationData.interruptionLevel = InterruptionLevelToUnity(content.interruptionLevel);
        notificationData.relevanceScore = content.relevanceScore;
    }
    else
    {
        notificationData.interruptionLevel = kInterruptionLevelActive;
        notificationData.relevanceScore = 0;
    }

    if ([request.trigger isKindOfClass: [UNTimeIntervalNotificationTrigger class]])
    {
        notificationData.triggerType = TIME_TRIGGER;

        UNTimeIntervalNotificationTrigger* timeTrigger = (UNTimeIntervalNotificationTrigger*)request.trigger;
        notificationData.trigger.timeInterval.interval = timeTrigger.timeInterval;
        notificationData.trigger.timeInterval.repeats = timeTrigger.repeats;
    }
    else if ([request.trigger isKindOfClass: [UNCalendarNotificationTrigger class]])
    {
        notificationData.triggerType = CALENDAR_TRIGGER;

        UNCalendarNotificationTrigger* calendarTrigger = (UNCalendarNotificationTrigger*)request.trigger;
        NSDateComponents* date = calendarTrigger.dateComponents;

        notificationData.trigger.calendar.year = (int)date.year;
        notificationData.trigger.calendar.month = (int)date.month;
        notificationData.trigger.calendar.day = (int)date.day;
        notificationData.trigger.calendar.hour = (int)date.hour;
        notificationData.trigger.calendar.minute = (int)date.minute;
        notificationData.trigger.calendar.second = (int)date.second;
        notificationData.trigger.calendar.repeats = (int)calendarTrigger.repeats;
    }
    else if ([request.trigger isKindOfClass: [UNLocationNotificationTrigger class]])
    {
#if UNITY_USES_LOCATION
        notificationData.triggerType = LOCATION_TRIGGER;

        UNLocationNotificationTrigger* locationTrigger = (UNLocationNotificationTrigger*)request.trigger;
        CLCircularRegion *region = (CLCircularRegion*)locationTrigger.region;

        notificationData.trigger.location.latitude = region.center.latitude;
        notificationData.trigger.location.longitude = region.center.longitude;
        notificationData.trigger.location.radius = region.radius;
        notificationData.trigger.location.notifyOnExit = region.notifyOnEntry;
        notificationData.trigger.location.notifyOnEntry = region.notifyOnExit;
        notificationData.trigger.location.repeats = locationTrigger.repeats;
#endif
    }
    else if ([request.trigger isKindOfClass: [UNPushNotificationTrigger class]])
    {
        notificationData.triggerType = PUSH_TRIGGER;
    }
    else
        notificationData.triggerType = UNKNOWN_TRIGGER;

    parseCustomizedData(&notificationData, request);
    notificationData.attachments = (__bridge_retained void*)request.content.attachments;

    return notificationData;
}

void freeiOSNotificationData(iOSNotificationData* notificationData)
{
    if (notificationData->identifier != NULL)
        free(notificationData->identifier);

    if (notificationData->title != NULL)
        free(notificationData->title);

    if (notificationData->body != NULL)
        free(notificationData->body);

    if (notificationData->subtitle != NULL)
        free(notificationData->subtitle);

    if (notificationData->categoryIdentifier != NULL)
        free(notificationData->categoryIdentifier);

    if (notificationData->threadIdentifier != NULL)
        free(notificationData->threadIdentifier);

    if (notificationData->soundName != NULL)
        free(notificationData->soundName);

    if (notificationData->userInfo != NULL)
    {
        NSDictionary* userInfo = (__bridge_transfer NSDictionary*)notificationData->userInfo;
        userInfo = nil;
    }
}

void* _AddItemToNSDictionary(void* dict, const char* key, const char* value)
{
    NSDictionary* dictionary;
    if (dict != NULL)
        dictionary = (__bridge NSDictionary*)dict;
    else
    {
        dictionary = [[NSMutableDictionary alloc] init];
        dict = (__bridge_retained void*)dictionary;
    }

    NSString* k = [NSString stringWithUTF8String: key];
    NSString* v = value ? [NSString stringWithUTF8String: value] : @"";
    [dictionary setValue: v forKey: k];
    return dict;
}

void* _AddAttachmentToNSArray(void* arr, const char* attId, const char* url, void** outError)
{
    *outError = NULL;
    NSString* attachmentId = nil;
    if (attId != NULL)
        attachmentId = [NSString stringWithUTF8String: attId];
    NSURL* uri = [NSURL URLWithString: [NSString stringWithUTF8String: url]];
    NSError* error = nil;
    UNNotificationAttachment* attachment = [UNNotificationAttachment attachmentWithIdentifier: attachmentId URL: uri options: nil error: &error];
    if (attachment != nil)
    {
        NSMutableArray* array;
        if (arr != NULL)
            array = (__bridge NSMutableArray*)arr;
        else
        {
            array = [[NSMutableArray alloc] init];
            arr = (__bridge_retained void*)array;
        }

        [array addObject: attachment];
        return arr;
    }

    if (error != nil)
        *outError = (__bridge_retained void*)error;
    return NULL;
}

void _ReadNSDictionary(void* csDict, void* nsDict, void (*callback)(void* csDcit, const char*, const char*))
{
    NSDictionary* dict = (__bridge NSDictionary*)nsDict;
    [dict enumerateKeysAndObjectsUsingBlock:^(id  _Nonnull key, id  _Nonnull obj, BOOL * _Nonnull stop) {
        NSString* k = key;
        NSString* v = ParseNotificationDataObject(obj);
        if (v != nil)
            callback(csDict, k.UTF8String, v.UTF8String);
        else
            NSLog(@"Failed to parse value for key '%@'", key);
    }];
}

void _ReadAttachmentsNSArray(void* csList, void* nsArray, void (*callback)(void*, const char*, const char*))
{
    NSArray<UNNotificationAttachment*>* attachments = (__bridge_transfer NSArray<UNNotificationAttachment*>*)nsArray;
    [attachments enumerateObjectsUsingBlock:^(UNNotificationAttachment * _Nonnull obj, NSUInteger idx, BOOL * _Nonnull stop) {
        NSString* idr = obj.identifier;
        NSString* url = obj.URL.absoluteString;
        callback(csList, idr.UTF8String, url.UTF8String);
    }];
}

#endif
