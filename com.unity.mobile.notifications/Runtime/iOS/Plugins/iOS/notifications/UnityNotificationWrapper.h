//
//  UnityNotificationWrapper.h
//  iOS.notifications
//
//  Created by Paulius on 26/07/2018.
//  Copyright © 2018 Unity Technologies. All rights reserved.
//

#ifndef UnityNotificationWrapper_h
#define UnityNotificationWrapper_h

typedef void (AUTHORIZATION_CALBACK)(struct iOSNotificationAuthorizationData* data);
typedef void (*DATA_CALLBACK)(struct iOSNotificationData* data);

void _SetNotificationReceivedDelegate(DATA_CALLBACK callback);

void _ScheduleLocalNotification(struct iOSNotificationData* data);
void _RequestAuthorization(int options, BOOL registerRemote);

int _GetScheduledNotificationDataCount();
iOSNotificationData* _GetScheduledNotificationDataAt(int index);


#endif /* UnityNotificationWrapper_h */
