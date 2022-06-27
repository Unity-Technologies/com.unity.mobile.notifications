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
    UNKNOWN_TRIGGER = -1,
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

    void* userInfo;
    void* attachments;

    // Trigger
    int triggerType;  //0 - time, 1 - calendar, 2 - location, 3 - push.
    union
    {
        struct
        {
            int interval;
            unsigned char repeats;
        } timeInterval;

        struct
        {
            int year;
            int month;
            int day;
            int hour;
            int minute;
            int second;
            unsigned char repeats;
        } calendar;

        struct
        {
            float centerX;
            float centerY;
            float radius;
            unsigned char notifyOnEntry;
            unsigned char notifyOnExit;
        } location;
    } trigger;
} iOSNotificationData;

typedef struct iOSNotificationAuthorizationData
{
    int granted;
    const char* error;
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

typedef void (*NotificationDataReceivedResponse)(iOSNotificationData data);
typedef void (*AuthorizationRequestResponse) (void* request, struct iOSNotificationAuthorizationData data);

NotificationSettingsData UNNotificationSettingsToNotificationSettingsData(UNNotificationSettings* settings);
iOSNotificationData UNNotificationRequestToiOSNotificationData(UNNotificationRequest* request);
void freeiOSNotificationData(iOSNotificationData* notificationData);

#endif /* UnityNotificationData_h */

#endif
