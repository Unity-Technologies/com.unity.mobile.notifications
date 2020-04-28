//
//  UnityNotificationWrapper.h
//  iOS.notifications
//

#if TARGET_OS_IOS

#ifndef UnityNotificationWrapper_h
#define UnityNotificationWrapper_h

void _SetNotificationReceivedDelegate(NotificationDataReceivedResponse callback);

void _ScheduleLocalNotification(struct iOSNotificationData* data);
void _RequestAuthorization(int options, BOOL registerRemote);

int _GetScheduledNotificationDataCount();
iOSNotificationData* _GetScheduledNotificationDataAt(int index);

#endif /* UnityNotificationWrapper_h */
#endif
