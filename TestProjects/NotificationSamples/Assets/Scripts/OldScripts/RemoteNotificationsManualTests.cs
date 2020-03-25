#if UNITY_IOS
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


public class RemoteNotificationsManualTests : MonoBehaviour
{
    private Transform notificationTestButton;
    private Transform notificationPanel;

    private List<Transform> currentNotifications;
    private AuthorizationRequest request;


    void Start()
    {
        currentNotifications = new List<Transform>();

        notificationPanel = transform.Find("TestPanel");
        notificationTestButton = notificationPanel.Find("TemplateButton");

        var tests = new OrderedDictionary();


        tests["Request Authorization"] = new Action(() => { StartCoroutine(RequestAuthorization()); });
        tests["Cancel All Delivered Notifications"] = new Action(() =>
        {
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
        });
        tests["Intercept All Received Remote Notifications"] = new Action(() => { InterceptAllReceivedRemoteNotifications(); });


        //Creating test buttons
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

    IEnumerator RequestAuthorization(AuthorizationOption options = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound)
    {
        var request = new AuthorizationRequest(
            options, true);
        yield return request;


        Debug.Log("RequestAuthorization");
        using (var req = new AuthorizationRequest(options, true))
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
    }

    public void InterceptAllReceivedRemoteNotifications()
    {
        iOSNotificationCenter.OnRemoteNotificationReceived += notification =>
        {
            Debug.Log("Just got a remote notification will reschedule it locally in 3 seconds!");

            var msg = "Just got a remote notification will reschedule it locally in 3 seconds! \n it's data and id:" + notification.Identifier + "\n";
            msg += "\n Notification received: ";
            msg += "\n .Title: " + notification.Title;
            msg += "\n .Badge: " + notification.Badge;
            msg += "\n .Body: " + notification.Body;
            msg += "\n .CategoryIdentifier: " + notification.CategoryIdentifier;
            msg += "\n .Subtitle: " + notification.Subtitle;
            msg += "\n ------ \n\n";
            Debug.Log(msg);


            var timeTrigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = new TimeSpan(0, 0, 3),
                Repeats = false
            };

            iOSNotification  n = new iOSNotification()
            {
                Title = "RE : " + notification.Title,
                Body =  "RE : " + notification.Body,
                Subtitle =  "RE : " + notification.Subtitle,
                ShowInForeground = true,
                ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert | PresentationOption.Badge,
                CategoryIdentifier = notification.CategoryIdentifier,
                ThreadIdentifier = notification.ThreadIdentifier,
                Trigger = timeTrigger,
                Badge = notification.Badge,
            };

            iOSNotificationCenter.ScheduleNotification(n);

            Debug.Log("Rescheduled remote notifications with id: " + notification.Identifier);
        };
    }
}
#endif
