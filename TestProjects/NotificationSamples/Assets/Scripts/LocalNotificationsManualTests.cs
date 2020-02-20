#if PLATFORM_IOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Unity.Notifications.iOS;
using UnityEngine;
using UnityEngine.iOS;
using UnityEngine.UI;


public class LocalNotificationsManualTests : MonoBehaviour
{
    private Transform notificationTestButton;
    private Transform notificationPanel;

    private List<Transform> currentNotifications;
    // Use this for initialization

    void Start()
    {
        currentNotifications = new List<Transform>();
        notificationPanel = transform.Find("TestPanel");
        notificationTestButton = notificationPanel.Find("TemplateButton");

        var tests = new OrderedDictionary();

        tests["Request Authorization"] = new Action(() => { StartCoroutine(RequestAuthorization()); });
        tests["Request Authorization Sound+Badge"] = new Action(() => { StartCoroutine(RequestAuthorization(AuthorizationOption.Sound | AuthorizationOption.Badge)); });
        tests["Send Simple Notification 4 Seconds"] = new Action(() => {
            var p = PresentationOption.Sound | PresentationOption.Badge  | PresentationOption.Alert;
            SendSimpleNotification4Seconds(foregroundOption: p, badge: 3);
        });
        tests["Send Notification 4 Seconds Foregound Only Sound"] = new Action(() =>
        {
            var p = PresentationOption.Sound;
            SendSimpleNotification4Seconds(title: "Send Notification 4 Seconds Foregound Only Sound", foregroundOption: p);
        });
        tests["Send Notification No Title Body Subtitle"] = new Action(() => { SendSimpleNotification4SecondsNoTitleBodySubtitle(); });
        tests["Send Notification No Body"] = new Action(() => { SendSimpleNotification4SecondsNoBody(); });
        tests["Send Simple Notification No Title With Body"] = new Action(() => { SendSimpleNotification4SecondsNoTitleWithBody(); });
        tests["Schedule Calendar Notification In 1 Minute"] = new Action(() => { ScheduleCalendarNotificationIn1Minute(); });
        tests["Send Notification 4 Seconds Repeatable"] = new Action(() => { SendSimpleNotification60SecondsRepeatable(); });
        tests["Send Two Notification To Separate Threads"] = new Action(() =>
        {
            SendSimpleNotification4Seconds(title: "A thread Notification", thread: "Athread");
            SendSimpleNotification4Seconds(title: "B thread Notification", thread: "Bthread");
        });
        tests["Cancel All Delivered Notifications"] = new Action(() =>
        {
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
        });
        tests["Cancel All Scheduled Notifications"] = new Action(() =>
        {
            iOSNotificationCenter.RemoveAllScheduledNotifications();
        });
        tests["List All Scheduled Notifications"] = new Action(() => { StartCoroutine(UpdateScheduledNotificationList()); });

        tests["Get User Notification Settings"] = new Action(() => { GetUserNotificationSettings(); });
        tests["Set Application Badge Test 45"] = new Action(() => { SetApplicationBadgeTest(45); });
        tests["Set Application Badge Test 0"] = new Action(() => { SetApplicationBadgeTest(0); });
        tests["Send Notification 4 Seconds and Cancel After 1 Second"] = new Action(() => { StartCoroutine(SendSimpleNotification4SecondsAndCancelAfter1Second()); });
        tests["Get Last Notification"] = new Action(() => { GetLastSelectedNotification(); });
        tests["Send Notification in 4 Seconds and Get Callback"] = new Action(() => { SendSimpleNotificationIn4SecondsAndGetCallback(); });
        tests["Set Location Trigger"] = new Action(() => { SetLocationTrigger(); });


        foreach (DictionaryEntry t in tests)
        {
            var button = GameObject.Instantiate(notificationTestButton);
            button.SetParent(notificationPanel);

            var text = button.gameObject.GetComponentInChildren<Text>();
            text.text = t.Key.ToString();

            var action = (Action)t.Value;

            button.GetComponent<Button>().onClick.AddListener(delegate
            {
                action.Invoke();
            });
        }

        notificationTestButton.gameObject.SetActive(false);
    }

    //Request authorization if it is not enabled in Editor UI
    IEnumerator RequestAuthorization(AuthorizationOption options = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound)
    {
        var request = new AuthorizationRequest(
            options, true);
        yield return request;


        Debug.Log("RequestAuthorization");
        using (var req = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge, true))
        {
            while (!req.IsFinished)
            {
                yield return null;
            }

            ;

            string res = "\n\n\n RequestAuthorization: \n";
            res += "\n finished: " + req.IsFinished;
            res += "\n granted :  " + req.Granted;
            res += "\n error:  " + req.Error;
            res += "\n deviceToken:  " + req.DeviceToken;

            res += "\n\n\n";
            Debug.Log(res);
        }


        //This part is needed if one wants to always get the callback of notifications
        /*
        iOSNotificationCenter.OnNotificationReceived += notification =>
            {
                Debug.Log("Notification received: ");
                Debug.Log(".Title: " + notification.Title);
                Debug.Log(".Identifier: " + notification.Identifier);
                Debug.Log(".Badge: " + notification.Badge);
                Debug.Log(".Body: " + notification.Body);
                Debug.Log(".CategoryIdentifier: " + notification.CategoryIdentifier);
                Debug.Log(".Subtitle: " + notification.Subtitle);
            };
            */
    }

    //Send Simple Notification
    string SendSimpleNotification4Seconds(string title = "SendSimpleNotification4Seconds", string thread = "thread_1", bool repeat = false, int seconds = 4, int minutes = 0, PresentationOption foregroundOption = PresentationOption.Alert, int badge = -1)
    {
        Debug.Log("SendSimpleNotification4Seconds");
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, minutes, seconds),
            Repeats = repeat
        };

        iOSNotification  n = new iOSNotification()
        {
            Title = title,
            Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 5 seconds",
            Subtitle = "This is a subtitle, something, something important...",
            ShowInForeground = true,
            ForegroundPresentationOption = foregroundOption,
            CategoryIdentifier = "category_a",
            ThreadIdentifier = thread,
            Trigger = timeTrigger,
            Data = "data_SendSimpleNotification4Seconds",
        };

        /* if (badge >= 0)
            n.Badge = badge + 1;*/

        iOSNotificationCenter.ScheduleNotification(n);

        return n.Identifier;
    }

    void SendSimpleNotification4SecondsNoTitleBodySubtitle()
    {
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, 5),
            Repeats = false
        };

        iOSNotification  n = new iOSNotification()
        {
//          Title = title,
//          Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 5 seconds",
//          Subtitle = "This is a subtitle, something, something important...",
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
//          CategoryIdentifier = "category_a",
//          ThreadIdentifier = thread,
            Data = "data_SendSimpleNotification4SecondsNoTitle",
            Trigger = timeTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(n);
    }

    void SendSimpleNotification4SecondsNoBody()
    {
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, 5),
            Repeats = false
        };

        iOSNotification  n = new iOSNotification()
        {
            Title = "Notification4SecondsNoBody",
//          Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 5 seconds",
            Subtitle = "This is a subtitle, something, something important...",
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
//          CategoryIdentifier = "category_a",
//          ThreadIdentifier = thread,
            Data = "data_SendSimpleNotification4SecondsNoBody",
            Trigger = timeTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(n);
    }

    void SendSimpleNotification4SecondsNoTitleWithBody()
    {
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, 5),
            Repeats = false
        };

        iOSNotification  n = new iOSNotification()
        {
//          Title = title,
            Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 5 seconds",
            Subtitle = "This is a subtitle, something, something important...",
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
//          CategoryIdentifier = "category_a",
//          ThreadIdentifier = thread,
            Trigger = timeTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(n);
    }

//Scheduling Calendar Notification
    void ScheduleCalendarNotificationIn1Minute()
    {
        Debug.Log("ScheduleCalendarNotificationIn1Minute");

        var now = DateTime.Now;

        now = now.AddMinutes(1);

        var calendarTrigger = new iOSNotificationCalendarTrigger()
        {
            Year = now.Year,
            Month = now.Month,
            Day = now.Day,
            Hour = now.Hour,
            Minute = now.Minute,
            Second = now.Second
        };


        iOSNotification  n = new iOSNotification()
        {
            Title = "ScheduleCalendarNotificationIn1Minute",
            Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 1 minute",
            Subtitle = "This is a calendar notification something smth...",
            ShowInForeground = true,
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread_1",
            Trigger = calendarTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(n);
    }

//Schedule repeatable
    void SendSimpleNotification60SecondsRepeatable()
    {
        Debug.Log("SendSimpleNotification60SecondsRepeatable");
        SendSimpleNotification4Seconds("SendSimpleNotification60SecondsRepeatable", repeat: true, seconds: 1, minutes: 1);
    }

//List all scheduled notifications
    IEnumerator UpdateScheduledNotificationList()
    {
        Debug.Log("UpdateScheduledNotificationList");

        var all = iOSNotificationCenter.GetScheduledNotifications();

        var o = "\n\n\n\n Currently scheduled:  " + all.Length;
        o += "\n";
        foreach (var n in all)
        {
            o += string.Format("  - {0} : {1} \n", n.Title, n.Body);
        }

        o += "\n";
        Debug.Log(o);


        yield return new WaitForSeconds(1.0f);
    }

//Getting user notification settings
    void GetUserNotificationSettings()
    {
        var s = iOSNotificationCenter.GetNotificationSettings();

        var msg = "iOS Notification settings : \n";
        msg += "\n AlertSetting: " + s.AlertSetting.ToString();
        msg += "\n AuthorizationStatus: " + s.AuthorizationStatus.ToString();
        msg += "\n BadgeSetting: " + s.BadgeSetting.ToString();
        msg += "\n CarPlaySetting: " + s.CarPlaySetting.ToString();
        msg += "\n LockScreenSetting: " + s.LockScreenSetting.ToString();
        msg += "\n NotificationCenterSetting: " + s.NotificationCenterSetting.ToString();
        msg += "\n SoundSetting: " + s.SoundSetting.ToString();
        msg += "\n AlertStyle: " + s.AlertStyle.ToString();
        msg += "\n ShowPreviewsSetting: " + s.ShowPreviewsSetting.ToString();

        Debug.Log(msg);
    }

//Send and cancel notification
    IEnumerator SendSimpleNotification4SecondsAndCancelAfter1Second()
    {
        Debug.Log("SendSimpleNotification4SecondsAndCancelAfter1Second");
        var id = SendSimpleNotification4Seconds("SendSimpleNotification4SecondsAndCancelIn2Seconds", repeat: false);
        yield return new WaitForSeconds(1.0f);

        iOSNotificationCenter.RemoveScheduledNotification(id);
    }

//Test badge
    public void SetApplicationBadgeTest(int badge)
    {
        iOSNotificationCenter.ApplicationBadge = badge;
        Debug.Log("Badge set to : " + iOSNotificationCenter.ApplicationBadge);
    }

//Getting las selected notification, which opened the app
    void GetLastSelectedNotification()
    {
        var n = iOSNotificationCenter.GetLastRespondedNotification();
        if (n != null)
        {
            var msg = "- - - Last Received Notification : " + n.Identifier + "\n";
            msg += "\n - - -  Notification received: ";
            msg += "\n - - -  .Title: " + n.Title;
            msg += "\n - - -  .Badge: " + n.Badge;
            msg += "\n - - -  .Body: " + n.Body;
            msg += "\n - - -  .CategoryIdentifier: " + n.CategoryIdentifier;
            msg += "\n - - -  .Subtitle: " + n.Subtitle;
            msg += "\n - - -  .Data: " + n.Data;
            msg += "\n - - - ------ \n\n";
            Debug.Log(msg);
        }
        else
        {
            Debug.Log("no received notification found!!!");
        }
    }

//send notification and get callback in the console
    void SendSimpleNotificationIn4SecondsAndGetCallback()
    {
        Debug.Log("SendSimpleNotificationIn4SecondsAndGetCallback");
        var id = SendSimpleNotification4Seconds("SendSimpleNotificationIn4SecondsAndGetCallback", badge: 7);

        iOSNotificationCenter.OnNotificationReceived += notification =>
        {
            var msg = "Notification received : " + notification.Identifier + "\n";
            msg += "\n Notification received: ";
            msg += "\n .Title: " + notification.Title;
            msg += "\n .Badge: " + notification.Badge;
            msg += "\n .Body: " + notification.Body;
            msg += "\n .CategoryIdentifier: " + notification.CategoryIdentifier;
            msg += "\n .Subtitle: " + notification.Subtitle;
            msg += "\n .Data: " + notification.Data;

            msg += "\n ------ \n\n";
            Debug.Log(msg);
        };
    }

//Sets location trigger
    public void SetLocationTrigger()
    {
        Input.location.Start();

        var locationTrigger = new iOSNotificationLocationTrigger()
        {
            Center = new Vector2(37.7749f, 122.4194f),
            Radius = 5500f,
            NotifyOnEntry = true,
            NotifyOnExit = true,
        };


        iOSNotification  n = new iOSNotification()
        {
            Title = "Location notification ",
            Body =  "Center: " + locationTrigger.Center.ToString() + "  Radius: " + locationTrigger.Radius.ToString(),
            Subtitle =  "I'ma subtitle!",
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert | PresentationOption.Badge,
            CategoryIdentifier = "CategeoryX",
            ThreadIdentifier = "locationNotifications",
            Trigger = locationTrigger,
            Badge = 13,
        };

        iOSNotificationCenter.ScheduleNotification(n);

        Debug.Log("Rescheduled remote notifications with id: " + n.Identifier);
    }
}
#endif
