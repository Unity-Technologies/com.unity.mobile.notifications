using System.Collections;
using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.TestTools;

using Unity.Notifications;
using Unity.Notifications.iOS;

namespace Unity.Notifications.Tests
{
    public class NotificationSettingsTests
    {
        [OneTimeSetUp]
        public void ResetSettings()
        {
            UnityNotificationEditorManager.DeleteSettings();
        }
        

        [Test]
        public void SetAndroidNotifcationSettings_Works()
        {
            UnityNotificationSettings.AndroidSettings.CustomActivityString = "com.test.dummy.activity";
            UnityNotificationSettings.AndroidSettings.UseCustomActivity = true;
            UnityNotificationSettings.AndroidSettings.RescheduleOnDeviceRestart = true;
            
            Assert.AreEqual("com.test.dummy.activity", UnityNotificationSettings.AndroidSettings.CustomActivityString);
            Assert.IsTrue(UnityNotificationSettings.AndroidSettings.UseCustomActivity);
            Assert.IsTrue(UnityNotificationSettings.AndroidSettings.RescheduleOnDeviceRestart);
        }
        
        [Test]
        public void SetiOSNotifcationSettings_Works()
        {
            UnityNotificationSettings.iOSSettings.AddRemoteNotificationCapability = true;
            UnityNotificationSettings.iOSSettings.DefaultAuthorizationOptions = PresentationOption.Alert;
            UnityNotificationSettings.iOSSettings.UseLocationNotificationTrigger = true;
            UnityNotificationSettings.iOSSettings.UseAPSReleaseEnvironment = true;
            UnityNotificationSettings.iOSSettings.RemoteNotificationForegroundPresentationOptions = PresentationOption.Alert;
            UnityNotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = true;
            UnityNotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch = true;

            Assert.IsTrue(UnityNotificationSettings.iOSSettings.AddRemoteNotificationCapability);
            Assert.IsTrue(UnityNotificationSettings.iOSSettings.UseLocationNotificationTrigger);
            Assert.IsTrue(UnityNotificationSettings.iOSSettings.UseAPSReleaseEnvironment);
            Assert.IsTrue(UnityNotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch);
            Assert.IsTrue(UnityNotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch);
            
            Assert.AreEqual(PresentationOption.Alert, UnityNotificationSettings.iOSSettings.RemoteNotificationForegroundPresentationOptions);
            Assert.AreEqual(PresentationOption.Alert, UnityNotificationSettings.iOSSettings.DefaultAuthorizationOptions);


        }

    }
}