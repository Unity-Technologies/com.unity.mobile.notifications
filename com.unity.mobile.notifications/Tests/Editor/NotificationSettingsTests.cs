using System.IO;
using NUnit.Framework;
using Unity.Notifications.iOS;

namespace Unity.Notifications.Tests
{
    internal class NotificationSettingsTests
    {
        [OneTimeSetUp]
        public void ResetSettings()
        {
            if (File.Exists(NotificationSettingsManager.k_SettingsPath))
            {
                File.Delete(NotificationSettingsManager.k_SettingsPath);
            }
        }

        [Test]
        public void SetAndroidNotifcationSettings_Works()
        {
            NotificationSettings.AndroidSettings.CustomActivityString = "com.test.dummy.activity";
            NotificationSettings.AndroidSettings.UseCustomActivity = true;
            NotificationSettings.AndroidSettings.RescheduleOnDeviceRestart = true;

            Assert.AreEqual("com.test.dummy.activity", NotificationSettings.AndroidSettings.CustomActivityString);
            Assert.IsTrue(NotificationSettings.AndroidSettings.UseCustomActivity);
            Assert.IsTrue(NotificationSettings.AndroidSettings.RescheduleOnDeviceRestart);
        }

        [Test]
        public void SetiOSNotifcationSettings_Works()
        {
            NotificationSettings.iOSSettings.AddRemoteNotificationCapability = true;
            NotificationSettings.iOSSettings.DefaultAuthorizationOptions = PresentationOption.Alert;
            NotificationSettings.iOSSettings.UseLocationNotificationTrigger = true;
            NotificationSettings.iOSSettings.UseAPSReleaseEnvironment = true;
            NotificationSettings.iOSSettings.RemoteNotificationForegroundPresentationOptions = PresentationOption.Alert;
            NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = true;
            NotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch = true;

            Assert.IsTrue(NotificationSettings.iOSSettings.AddRemoteNotificationCapability);
            Assert.IsTrue(NotificationSettings.iOSSettings.UseLocationNotificationTrigger);
            Assert.IsTrue(NotificationSettings.iOSSettings.UseAPSReleaseEnvironment);
            Assert.IsTrue(NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch);
            Assert.IsTrue(NotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch);

            Assert.AreEqual(PresentationOption.Alert, NotificationSettings.iOSSettings.RemoteNotificationForegroundPresentationOptions);
            Assert.AreEqual(PresentationOption.Alert, NotificationSettings.iOSSettings.DefaultAuthorizationOptions);
        }
    }
}
