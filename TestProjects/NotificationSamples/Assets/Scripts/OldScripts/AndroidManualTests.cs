#if PLATFORM_ANDROID

using System.Collections;
using System.Collections.Generic;
using Unity.Notifications.Android;
using UnityEngine;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine.UI;
using System;

public class AndroidManualTests : MonoBehaviour
{
    private Transform notificationTestButton;
    private Transform notificatonPanel;

    private List<Transform> currentNotifications;

    public void Start()
    {
        SetAllTestButtons();

        var c = new AndroidNotificationChannel()
        {
            Id = "channel_id",
            Name = "Default Channel",
            Importance = Importance.High,
            Description = "Generic notifications",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(c);

        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

        if (AndroidNotificationCenter.GetNotificationChannels() != null)
            foreach (var ch in AndroidNotificationCenter.GetNotificationChannels())
            {
                string outStr = "\n Channel:";

                outStr += string.Format(" ch.Name : {0}", ch.Name);
                outStr += string.Format(" ch.CanBypassDnd : {0}", ch.CanBypassDnd);
                outStr += string.Format(" ch.CanShowBadge : {0}", ch.CanShowBadge);
                outStr += string.Format(" ch.Description : {0}", ch.Description);
                outStr += string.Format(" ch.EnableLights : {0}", ch.EnableLights);
                outStr += string.Format(" ch.EnableVibration : {0}", ch.EnableVibration);
                outStr += string.Format(" ch.Importance : {0}", ch.Importance);
                outStr += string.Format(" ch.LockScreenVisibility : {0}", ch.LockScreenVisibility.ToString());

                outStr += "\n";
                Debug.Log(outStr);
            }
    }

    private void SetAllTestButtons()
    {
        currentNotifications = new List<Transform>();

        var testPanel = transform.Find("TestPanel");
        //notificatonPanel = transform.Find("NotificatonPanel");

        var templateTestButton = testPanel.Find("TemplateButton");
        //notificationTestButton =notificatonPanel.Find("TemplateButton");

        var tests = new OrderedDictionary();

        tests["Send Simple Notification in 5s Small icon"] = new Action(() => { TestSendSimpleNotification(5); });
        tests["Send Simple Notification in 10s No Icon"] = new Action(() => { TestSendSimpleNotificationNoIcon(10); });
        tests["Send Fancy Notification in 5s Large Icon"] = new Action(() => { TestSendFancyNotification(); });
        tests["Send Simple Notification in 60s for Restart"] = new Action(() => { TestSendSimpleNotification(60); });
        tests["Get Info Of Notification Which Opened The App"] = new Action(() => { GetInfoOfNotificationWhichOpenedTheApp(); });
        tests["Create Notification Channel"] = new Action(() => { CreateNotificationChannel_NotificationChannelIsCreated(); });
        tests["Delete Notification Channel"] = new Action(() => { DeleteNotificationChannels_NotificationChannelsAreDeleted(); });
        tests["Register Notification Channel (all tests are using)"] = new Action(() => { RegisterDeletedNotificationChannel(); });
        tests["Create Notification Channel With Initialized Settings"] = new Action(() => { CreateNotificationChannelWithInitializedSettings_ChannelSettingsAreSaved();});
        tests["Get All Notification Channels"] = new Action(() => { GetAllNotificationChannels(); });
        tests["Send Group Notification A"] = new Action(() => { SendGroupNotificationA(); });
        tests["Send Group Notification A Summary"] = new Action(() => { SendGroupNotificationASummary(); });
        tests["Cancel All Notifications"] = new Action(() => { CancelAll(); });
        tests["Cancel Displayed Notifications"] = new Action(() => { CancelDisplayed(); });
        tests["Cancel Pending Notifications"] = new Action(() => { CancelPending(); });
        tests["Receive Callback Of Newly Delivered Notification"] = new Action(() => {  ReceiveCallbackOfNewlyDeliveredNotification(); });


        foreach (DictionaryEntry t in tests)
        {
            var button = GameObject.Instantiate(templateTestButton);
            button.SetParent(testPanel);

            var text = button.gameObject.GetComponentInChildren<Text>();
            text.text = t.Key.ToString();

            var action = (Action)t.Value;

            button.GetComponent<Button>().onClick.AddListener(delegate
            {
                action.Invoke();
            });
        }

        templateTestButton.gameObject.SetActive(false);
    }

    public void TestSendSimpleNotification(int seconds)
    {
        var notification = new AndroidNotification();
        notification.Title = seconds.ToString() + " seconds";
        notification.Text = seconds.ToString() + " seconds Text";
        notification.FireTime = System.DateTime.Now.AddSeconds(seconds);
        notification.IntentData = seconds.ToString() + " seconds";
        notification.Color = Color.magenta;
        notification.SmallIcon = "icon_0";;

        AndroidNotificationCenter.SendNotification(notification, "channel_id");

        AndroidNotificationCenter.OnNotificationReceived += (id) => {
            Debug.Log("Received notification : " + id.ToString());
        };
    }

    public void TestSendSimpleNotificationNoIcon(int seconds)
    {
        var notification = new AndroidNotification();
        notification.Title = seconds.ToString() + " seconds";
        notification.Text = seconds.ToString() + " seconds Text";
        notification.FireTime = System.DateTime.Now.AddSeconds(seconds);
        notification.IntentData = seconds.ToString() + " seconds";
        notification.Color = Color.green;


        AndroidNotificationCenter.SendNotification(notification, "channel_id");

        AndroidNotificationCenter.OnNotificationReceived += (id) => {
            Debug.Log("Received notification : " + id.ToString());
        };
    }

    public void TestSendFancyNotification()
    {
        var n = new AndroidNotification();
        n.Title = "TestSendFancyNotification";
        n.Text = "Color Blue";
        n.FireTime = System.DateTime.Now.AddSeconds(5);

        n.Color = Color.blue;
        n.UsesStopwatch = true;
        n.Number = 4;
        n.SmallIcon = "icon_0";
        n.LargeIcon = "icon_1";

        AndroidNotificationCenter.SendNotification(n, "channel_id");
    }

    public string GetInfoOfNotificationWhichOpenedTheApp()
    {
        var notificationIntentData = AndroidNotificationCenter.GetLastNotificationIntent();

        if (notificationIntentData != null)
        {
            var msg = "- - - Last Received Notification : " + notificationIntentData.Id + "\n";
            msg += "\n - - -  Notification channel: " + notificationIntentData.Channel;
            msg += "\n - - - Notification data: " + notificationIntentData.Notification;
            Debug.Log(msg);

            var notification = notificationIntentData.Notification;
            Debug.Log("Notification intent Data: " + notification.IntentData);

            return notification.IntentData;
        }
        else
        {
            Debug.Log("no received notification found!!!");
            return "no notification";
        }
    }

    public void CreateNotificationChannel_NotificationChannelIsCreated()
    {
        var ch = new AndroidNotificationChannel()
        {
            Id = "default",
            Name = "Default Channel",
            Description = "Generic spam",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(ch);


        Debug.Log("Channels count: " + AndroidNotificationCenter.GetNotificationChannels().Length.ToString());
    }

    public void DeleteNotificationChannels_NotificationChannelsAreDeleted()
    {
        Debug.Log("DeleteNotificationChannels_NotificationChannelsAreDeleted: ");

        foreach (var ch in AndroidNotificationCenter.GetNotificationChannels())
        {
            AndroidNotificationCenter.DeleteNotificationChannel(ch.Id);
        }
    }

    public void RegisterDeletedNotificationChannel()
    {
        var c = new AndroidNotificationChannel()
        {
            Id = "channel_id",
            Name = "Default Channel",
            Importance = Importance.High,
            Description = "Generic notifications",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(c);
    }

    public void CreateNotificationChannelWithInitializedSettings_ChannelSettingsAreSaved()
    {
        var chOrig = new AndroidNotificationChannel(){
            Id = "UrgentSpamChannel",
            Name = "spam Channel",
            Description = "Generic spam",
            Importance = Importance.High,
            CanBypassDnd = true,
            CanShowBadge = true,
            EnableLights = true,
            EnableVibration = true,
            LockScreenVisibility = LockScreenVisibility.Secret,
            VibrationPattern = new long[] { 0L, 1L, 2L },
        };
        AndroidNotificationCenter.RegisterNotificationChannel(chOrig);


        var ch = AndroidNotificationCenter.GetNotificationChannel(chOrig.Id);


        Debug.Log(string.Format("ch.Id orig:{0} - real:{1}", chOrig.Id, ch.Id));
        Debug.Log(string.Format("ch.Name orig:{0} - real:{1}", chOrig.Name, ch.Name));
        Debug.Log(string.Format("ch.Description orig:{0} - real:{1}", chOrig.Description, ch.Description));
        Debug.Log(string.Format("ch.Importance orig:{0} - real:{1}", chOrig.Importance, ch.Importance));
        Debug.Log(string.Format("ch.CanBypassDnd orig:{0} - real:{1}", chOrig.CanBypassDnd, ch.CanBypassDnd));
        Debug.Log(string.Format("ch.CanShowBadge orig:{0} - real:{1}", chOrig.CanShowBadge, ch.CanShowBadge));
        Debug.Log(string.Format("ch.EnableLights orig:{0} - real:{1}", chOrig.EnableLights, ch.EnableLights));
        Debug.Log(string.Format("ch.EnableVibration orig:{0} - real:{1}", chOrig.EnableVibration, ch.EnableVibration));
        Debug.Log(string.Format("ch.LockScreenVisibility orig:{0} - real:{1}", chOrig.LockScreenVisibility, ch.LockScreenVisibility));
        Debug.Log(string.Format("ch.VibrationPattern orig:{0} - real:{1}", chOrig.VibrationPattern, ch.VibrationPattern));
    }

    public void GetAllNotificationChannels()
    {
        var channels = AndroidNotificationCenter.GetNotificationChannels();
        if (channels != null)
        {
            foreach (var ch in channels)
            {
                var desc = string.Format("id: {0} title: {1} \n m_importance: {2} m_lockscreenVisibility: {3} m_vibrationPattern.len: {4}",
                    ch.Id, ch.Name, (int)ch.Importance, (int)ch.LockScreenVisibility, 0);

                Debug.Log(desc);
            }
        }
        else
        {
            Debug.Log("No channels");
        }
    }

    public void SendGroupNotificationA()
    {
        var notification = new AndroidNotification();
        notification.Title = "10 seconds : Group A";
        notification.Text = string.Format("10 seconds Text : Group A {0}", UnityEngine.Random.Range(0, 100));
        notification.SmallIcon = "icon_0";;

        notification.Group = "com.android.example.GROUP_A";

        notification.FireTime = System.DateTime.Now.AddSeconds(10);
        notification.IntentData = "10 seconds";

        AndroidNotificationCenter.SendNotification(notification, "channel_id");
    }

    public void SendGroupNotificationASummary()
    {
        var notification = new AndroidNotification();
        notification.Title = "10 seconds : Group A";
        notification.Text = string.Format("10 seconds Text : Group A Summary {0}", UnityEngine.Random.Range(0, 100));
        notification.SmallIcon = "icon_0";;

        notification.Group = "com.android.example.GROUP_A";
        notification.GroupSummary = true;

        notification.GroupAlertBehaviour = GroupAlertBehaviours.GroupAlertAll;
        notification.SortKey = string.Format("{0}", UnityEngine.Random.Range(0, 100));

        notification.FireTime = System.DateTime.Now.AddSeconds(10);
        notification.IntentData = "10 seconds";

        AndroidNotificationCenter.SendNotification(notification, "channel_id");
    }

    public void CancelAll()
    {
        AndroidNotificationCenter.CancelAllNotifications();
    }

    public void CancelDisplayed()
    {
        AndroidNotificationCenter.CancelAllDisplayedNotifications();
    }

    public void CancelPending()
    {
        AndroidNotificationCenter.CancelAllScheduledNotifications();
    }

    public void ReceiveCallbackOfNewlyDeliveredNotification()
    {
        AndroidNotificationCenter.NotificationReceivedCallback receivedNotificationHandler =
            delegate(AndroidNotificationIntentData data)
        {
            var msg = "Notification received : " + data.Id + "\n";
            msg += "\n Notification received: ";
            msg += "\n .Title: " + data.Notification.Title;
            msg += "\n .Body: " + data.Notification.Text;
            msg += "\n .Channel: " + data.Channel;
            Debug.Log(msg);
        };

        AndroidNotificationCenter.OnNotificationReceived += receivedNotificationHandler;
    }
}

#endif
