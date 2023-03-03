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

enum UnitySoundType
{
    kSoundTypeDefault = 0,
    kSoundTypeCritical = 1,
    kSoundTypeRingtone = 2,
    kSoundTypeNone = 4,
};

enum UnityNotificationInterruptionLevel
{
    kInterruptionLevelActive = 0,
    kInterruptionLevelCritical = 1,
    kInterruptionLevelPassive = 2,
    kInterruptionLevelTimeSensitive = 3,
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
    int soundType;
    float soundVolume;
    char* soundName;
    int interruptionLevel;
    double relevanceScore;

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
            double latitude;
            double longitude;
            float radius;
            unsigned char notifyOnEntry;
            unsigned char notifyOnExit;
            unsigned char repeats;
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
