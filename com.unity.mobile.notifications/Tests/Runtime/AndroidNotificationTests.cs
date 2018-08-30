using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Unity.Notifications.Android;

class AndroidNotificationTests
{
    private static int m_receivedNotificationCount = 0;

    [Test]
    public void CreateNotificationChannel_NotificationChannelIsCreated()
    {
        var testChannelId = "default_test_channel_10";
        AndroidNotificationManager.DeleteNotificationChannel(testChannelId);
        Assert.AreNotEqual("default_test_channel_10", AndroidNotificationManager.GetNotificationChannel(testChannelId).Id);

        var newChannel = new AndroidNotificationChannel();
        newChannel.Id = testChannelId;
        newChannel.Name = "Default Channel";
        newChannel.Description = "Generic spam";

        var currentChannelCount = AndroidNotificationManager.GetNotificationChannels().Length;
        AndroidNotificationManager.RegisterNotificationChannel(newChannel);
        currentChannelCount++;

        Assert.AreEqual(currentChannelCount, AndroidNotificationManager.GetNotificationChannels().Length);
    }

    [Test]
    public void DeleteNotificationChannels_NotificationChannelsAreDeleted()
    {
        if (AndroidNotificationManager.GetNotificationChannels().Length < 1)
        {
            var ch = new AndroidNotificationChannel();
            ch.Id = "default_test_channel_0";
            ch.Name = "Default Channel";
            ch.Description = "Generic spam";
            ch.Importance = Importance.Default;
        }

        foreach (var ch in AndroidNotificationManager.GetNotificationChannels())
        {
            AndroidNotificationManager.DeleteNotificationChannel(ch.Id);
        }

        Assert.AreEqual(0, AndroidNotificationManager.GetNotificationChannels().Length);
    }

    [UnityTest]
    public IEnumerator SendNotification_NotificationIsReceived()
    {
        AndroidNotificationManager.CancelAllNotifications();

        yield return new WaitForSeconds(3.0f);

        var n = new AndroidNotification();
        n.Title = "SendNotification_NotificationIsReceived";
        n.Text = "SendNotification_NotificationIsReceived Text";
        n.FireTime = System.DateTime.Now.AddSeconds(2.0f);    
        
        
        var current_time = DateTime.Now;

        Debug.LogWarning(string.Format("SendNotification_NotificationIsReceived:::FireTime::: {0}  -> {1}", current_time.ToString(), n.FireTime.ToString()));


        var c = new AndroidNotificationChannel();
        c.Id = "default_test_channel_5";
        c.Name = "Default Channel 5";
        c.Description = "test_channel 5";
        c.Importance = Importance.High;
        
        AndroidNotificationManager.RegisterNotificationChannel(c);
        int originalId = AndroidNotificationManager.SendNotification(n, "default_test_channel_5");

        
        AndroidNotificationManager.NotificationReceivedCallback receivedNotificationHandler = 
            delegate(int id, AndroidNotification notification, string channel)
            {
                m_receivedNotificationCount += 1;
                Assert.AreEqual(originalId, id);
            };
        
        
        AndroidNotificationManager.OnNotificationReceived += receivedNotificationHandler;
        
        yield return new WaitForSeconds(8.0f);

        Debug.LogWarning("SendNotification_NotificationIsReceived:::   Assert.AreEqual(1, m_receivedNotificationCount) m_receivedNotificationCount: "  + m_receivedNotificationCount.ToString()) ;

        Assert.AreEqual(1, m_receivedNotificationCount);

        AndroidNotificationManager.OnNotificationReceived -= receivedNotificationHandler;
        
        AndroidNotificationManager.CancelAllNotifications();
        m_receivedNotificationCount = 0;

    }
    
//    [UnityTest]
    public IEnumerator ScheduleRepeatableNotification_NotificationsAreReceived()
    {
        AndroidNotificationManager.CancelAllNotifications();
    
        yield return new WaitForSeconds(2.0f);
    
        var n = new AndroidNotification();
        n.Title = "Repeating Notification Title";
        n.Text = "Repeating Notification Text";
        n.FireTime = System.DateTime.Now.AddSeconds(0.5f);
        n.RepeatInterval = new System.TimeSpan(0, 0, 1);
    
        var c = new AndroidNotificationChannel();
        c.Id = "default_test_channel_2";
        c.Name = "Default Channel";
        c.Description = "test_channel";
        c.Importance = Importance.High;
    
        AndroidNotificationManager.RegisterNotificationChannel(c);
        int originalId = AndroidNotificationManager.SendNotification(n, "default_test_channel_2");
    
        AndroidNotificationManager.NotificationReceivedCallback receivedNotificationHandler = 
            delegate(int id, AndroidNotification notification, string channel)
            {
                m_receivedNotificationCount += 1;
                Assert.AreEqual(originalId, id);
            };

        AndroidNotificationManager.OnNotificationReceived += receivedNotificationHandler;

        //The notification should be repeated every second, so we should get atleast 3.
        yield return new WaitForSeconds(5.0f);
            
        Assert.GreaterOrEqual(3, m_receivedNotificationCount);
        m_receivedNotificationCount = 0;
            
        AndroidNotificationManager.OnNotificationReceived -= receivedNotificationHandler;

        AndroidNotificationManager.CancelAllNotifications();
    }

    [Test]
    public void SetNotificationFireTime_TimeIsConvertedToUnixTimeAndBack()
    {
        var n = new AndroidNotification();
        var fireTime = new DateTime(2018, 5, 24, 12, 41, 30, 122);
        n.FireTime = new DateTime(2018, 5, 24, 12, 41, 30, 122);

        Assert.AreEqual(fireTime, n.FireTime);
    }

    [Test]
    public void SetNotificationRepeatInterval_TimeIsConvertedToUnixTimeAndBack()
    {
        var n = new AndroidNotification();
        var repeatInterval = TimeSpan.FromSeconds(666);
        n.RepeatInterval = repeatInterval;

        Assert.AreEqual(repeatInterval, n.RepeatInterval);
    }

    // TODO FIX
    //    [UnityTest]
    public IEnumerator NotificationIsScheduled_NotificationStatusIsCorrectlyReported()
    {
        var n = new AndroidNotification();
        n.Title = "NotificationStatusIsCorrectlyReported";
        n.Text = "NotificationStatusIsCorrectlyReported";
        n.FireTime = System.DateTime.Now.AddSeconds(2f);

        var c = new AndroidNotificationChannel();
        c.Id = "status_test_channel_0";
        c.Name = "status test channel";
        c.Description = "NotificationIsScheduled_NotificationStatusIsCorrectlyReported";
        c.Importance = Importance.High;

        AndroidNotificationManager.RegisterNotificationChannel(c);


        yield return new WaitForSeconds(3.0f);
        int originalId = AndroidNotificationManager.SendNotification(n, "status_test_channel");
        yield return new WaitForSeconds(0.1f);

        var status = AndroidNotificationManager.CheckScheduledNotificationStatus(originalId);
        Assert.AreEqual(NotificationStatus.Scheduled, status);

        yield return new WaitForSeconds(5f);

        status = AndroidNotificationManager.CheckScheduledNotificationStatus(originalId);
        Assert.AreEqual(NotificationStatus.Delivered, status);

        AndroidNotificationManager.CancelNotification(originalId);
        yield return new WaitForSeconds(1.5f);

        status = AndroidNotificationManager.CheckScheduledNotificationStatus(originalId);
        Assert.AreEqual(NotificationStatus.Unknown, status);
    }

    [Test]
    public void CreateNotificationChannelWithInitializedSettings_ChannelSettingsAreSaved()
    {
        var chOrig = new AndroidNotificationChannel();
        chOrig.Id = "test_channel_settings_are_saved_0";
        chOrig.Name = "spam Channel";
        chOrig.Description = "Generic spam";
        chOrig.Importance = Importance.High;
        chOrig.CanBypassDnd = true;
        chOrig.CanShowBadge = true;
        chOrig.EnableLights = true;
        chOrig.EnableVibration = false;
        chOrig.LockScreenVisibility = LockScreenVisibility.Private;

        AndroidNotificationManager.RegisterNotificationChannel(chOrig);

        var ch = AndroidNotificationManager.GetNotificationChannel(chOrig.Id);

        Assert.AreEqual(chOrig.Id, ch.Id);
        Assert.AreEqual(chOrig.Name, ch.Name);
        Assert.AreEqual(chOrig.Description, ch.Description);
        Assert.AreEqual(chOrig.Importance, ch.Importance);
        Assert.AreEqual(chOrig.EnableLights, ch.EnableLights);
        Assert.AreEqual(chOrig.EnableVibration, ch.EnableVibration);
        Assert.AreEqual(chOrig.LockScreenVisibility, ch.LockScreenVisibility);
    }
}