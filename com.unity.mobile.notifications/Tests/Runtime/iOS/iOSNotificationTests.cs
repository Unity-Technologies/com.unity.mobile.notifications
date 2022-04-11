using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Unity.Notifications.iOS;
#if UNITY_EDITOR
using Unity.Notifications;
using UnityEditor;
#endif

class iOSNotificationTests
    : IPrebuildSetup, IPostBuildCleanup
{
    private static int receivedNotificationCount = 0;
    private static iOSNotification lastReceivedNotification = null;
#if UNITY_EDITOR
    private static iOSSdkVersion originaliOSSDK;
    private static bool originalRequestAuthorizationOnAppLaunch;
    private static PresentationOption originalAuthorizationOptions;
    private static bool originalAddRemoteNotificationCapability;
    private static bool originalRequestRemoteOnLaunch;
#endif

    public void Setup()
    {
#if UNITY_EDITOR
        originaliOSSDK = PlayerSettings.iOS.sdkVersion;
        originalRequestAuthorizationOnAppLaunch = NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch;
        originalAuthorizationOptions = NotificationSettings.iOSSettings.DefaultAuthorizationOptions;
        originalAddRemoteNotificationCapability = NotificationSettings.iOSSettings.AddRemoteNotificationCapability;
        originalRequestRemoteOnLaunch = NotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch;

        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
        NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = true;
        NotificationSettings.iOSSettings.DefaultAuthorizationOptions = originalAuthorizationOptions;
        NotificationSettings.iOSSettings.AddRemoteNotificationCapability = false;
        NotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch = false;
#endif
    }

    public void Cleanup()
    {
#if UNITY_EDITOR
        PlayerSettings.iOS.sdkVersion = originaliOSSDK;
        NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = originalRequestAuthorizationOnAppLaunch;
        NotificationSettings.iOSSettings.DefaultAuthorizationOptions = originalAuthorizationOptions;
        NotificationSettings.iOSSettings.AddRemoteNotificationCapability = originalAddRemoteNotificationCapability;
        NotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch = originalRequestRemoteOnLaunch;
#endif
    }

    [OneTimeSetUp]
    public void BeforeTests()
    {
        iOSNotificationCenter.OnNotificationReceived += receivedNotification =>
        {
            receivedNotificationCount += 1;
            lastReceivedNotification = receivedNotification;
            var msg = "Notification received : " + receivedNotification.Identifier + "\n";
            msg += "\n Notification received: ";
            msg += "\n .Title: " + receivedNotification.Title;
            msg += "\n .Badge: " + receivedNotification.Badge;
            msg += "\n .Body: " + receivedNotification.Body;
            msg += "\n .CategoryIdentifier: " + receivedNotification.CategoryIdentifier;
            msg += "\n .Subtitle: " + receivedNotification.Subtitle;
            Debug.Log(msg);
        };
    }

    [TearDown]
    public void AfterEachTest()
    {
        receivedNotificationCount = 0;
        lastReceivedNotification = null;
    }

    [UnityTest]
    [UnityPlatform(RuntimePlatform.IPhonePlayer)]
    public IEnumerator SendSimpleNotification_NotificationIsReceived()
    {
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, 5),
            Repeats = false
        };

        // You can optionally specify a custom Identifier which can later be
        // used to cancel the notification, if you don't set one, an unique
        // string will be generated automatically.
        var notification = new iOSNotification()
        {
            Identifier = "_notification_01",
            Title = "SendSimpleNotification_NotificationIsReceived",
            Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 5 seconds",
            Subtitle = "This is a subtitle, something, something important...",
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert |
                PresentationOption.Sound),
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread1",
            Trigger = timeTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(notification);

        yield return new WaitForSeconds(6.0f);
        Assert.AreEqual(1, receivedNotificationCount);
    }

    [UnityTest]
    [UnityPlatform(RuntimePlatform.IPhonePlayer)]
    public IEnumerator SendNotificationWithUserInfo_NotificationIsReceivedWithSameUserInfo()
    {
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, 5),
            Repeats = false
        };

        var notification = new iOSNotification()
        {
            Identifier = "_notification_02",
            Title = "SendNotificationWithUserInfo_NotificationIsReceivedWithSameUserInfo",
            Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 5 seconds",
            Subtitle = "This is a subtitle, something, something important...",
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread1",
            Trigger = timeTrigger,
        };

        notification.UserInfo.Add("key1", "value1");

        iOSNotificationCenter.ScheduleNotification(notification);

        yield return new WaitForSeconds(6.0f);
        Assert.AreEqual(1, receivedNotificationCount);
        Assert.IsNotNull(lastReceivedNotification);
        Assert.IsTrue(lastReceivedNotification.UserInfo.ContainsKey("key1"));
        Assert.AreEqual("value1", lastReceivedNotification.UserInfo["key1"]);
    }
}
