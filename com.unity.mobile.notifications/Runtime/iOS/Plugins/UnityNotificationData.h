//
//  UnityNotificationData.h
//  iOS.notifications
//

#if TARGET_OS_IOS

#ifndef UnityNotificationData_h
#define UnityNotificationData_h

enum triggerType
{
    TIME_TRIGGER = 0,
    CALENDAR_TRIGGER = 10,
    LOCATION_TRIGGER = 20,
    PUSH_TRIGGER = 3,
};

typedef struct iOSNotificationData
{
    char* identifier;
    char* title;
    char* body;
    int badge;
    char* subtitle;
    char* categoryIdentifier;
    char* threadIdentifier;

    //Custom data
    char* data;
    BOOL showInForeground;
    int showInForegroundPresentationOptions;

    // Trigger
    int triggerType;  //0 - time, 1 - calendar, 2 - location, 3 - push.
    BOOL repeats;

    //Time trigger
    int timeTriggerInterval;

    //Calendar trigger
    int calendarTriggerYear;
    int calendarTriggerMonth;
    int calendarTriggerDay;
    int calendarTriggerHour;
    int calendarTriggerMinute;
    int calendarTriggerSecond;

    //Location trigger
    float locationTriggerCenterX;
    float locationTriggerCenterY;
    float locationTriggerRadius;
    bool locationTriggerNotifyOnEntry;
    bool locationTriggerNotifyOnExit;
} iOSNotificationData;

typedef struct iOSNotificationAuthorizationData
{
    bool granted;
    const char* error;
    bool finished;
    const char* deviceToken;
} iOSNotificationAuthorizationData;

typedef struct NotificationSettingsData
{
    int authorizationStatus;
    int notificationCenterSetting;
    int lockScreenSetting;
    int carPlaySetting;
    int alertSetting;
    int badgeSetting;
    int soundSetting;
    int alertStyle;
    int showPreviewsSetting;
} NotificationSettingsData;

typedef void (*NotificationDataReceivedResponse)(struct iOSNotificationData* data);
typedef void (*AuthorizationRequestResponse) (struct iOSNotificationAuthorizationData* data);

// Who calls these two below methods should be responsible for freeing the returned memory.
NotificationSettingsData* UNNotificationSettingsToNotificationSettingsData(UNNotificationSettings* settings);
iOSNotificationData* UNNotificationRequestToiOSNotificationData(UNNotificationRequest* request);
void freeiOSNotificationData(iOSNotificationData* notificationData);

#endif /* UnityNotificationData_h */

#endif
