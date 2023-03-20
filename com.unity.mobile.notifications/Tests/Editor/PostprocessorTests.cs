using System.Xml;
using NUnit.Framework;

namespace Unity.Notifications.Tests
{
    internal class PostprocessorTests
    {
        [Test]
        public void DummmyTest()
        {
            Assert.AreEqual(true, true);
        }

        const string kManifestTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"" package=""com.UnityTestRunner.UnityTestRunner"" xmlns:tools=""http://schemas.android.com/tools"" android:installLocation=""preferExternal"">
  <supports-screens android:smallScreens=""true"" android:normalScreens=""true"" android:largeScreens=""true"" android:xlargeScreens=""true"" android:anyDensity=""true"" />
  <application android:theme=""@style/UnityThemeSelector"" android:icon=""@mipmap/app_icon"" android:label=""@string/app_name"" android:isGame=""true"" android:banner=""@drawable/app_banner"">
    <activity android:label=""@string/app_name"" android:screenOrientation=""fullSensor"" android:launchMode=""singleTask"" android:configChanges=""mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale|layoutDirection|density"" android:hardwareAccelerated=""false"" android:name=""com.UnityTestRunner.UnityTestRunner.UnityPlayerActivity"">
      <intent-filter>
        <action android:name=""android.intent.action.MAIN"" />
        <category android:name=""android.intent.category.LAUNCHER"" />
        <category android:name=""android.intent.category.LEANBACK_LAUNCHER"" />
      </intent-filter>
      <meta-data android:name=""unityplayer.UnityActivity"" android:value=""true"" />
    </activity>
    <meta-data android:name=""unity.build-id"" android:value=""8a616d3b-0433-49d8-bbaf-fd1415e8701e"" />
    <meta-data android:name=""unity.splash-mode"" android:value=""0"" />
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />{0}
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />{1}
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
</manifest>";
        const string kRescheduleOnRestartFalse = "<meta-data android:name=\"reschedule_notifications_on_restart\" android:value=\"false\" />";
        const string kRescheduleOnRestartTrue = "<meta-data android:name=\"reschedule_notifications_on_restart\" android:value=\"true\" />";
        const string kExactSchedulingOn = "<meta-data android:name=\"com.unity.androidnotifications.exact_scheduling\" android:value=\"1\" />";
        const string kExactSchedulingOff = "<meta-data android:name=\"com.unity.androidnotifications.exact_scheduling\" android:value=\"0\" />";
        const string kReceiveBookCompletedPermission = "<uses-permission android:name=\"android.permission.RECEIVE_BOOT_COMPLETED\" />";
        const string kScheduleExactAlarmPermission = "<uses-permission android:name=\"android.permission.SCHEDULE_EXACT_ALARM\" />";
        const string kUseExactAlarmPermission = "<uses-permission android:name=\"android.permission.USE_EXACT_ALARM\" />";
        const string kRequestIgnoreBatteryOptimizationsPermission = "<uses-permission-sdk-23 android:name=\"android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS\" />";

        string GetSourceXml(string metaDataExtra, string permissionExtra)
        {
            if (metaDataExtra == null)
                metaDataExtra = "";
            if (permissionExtra == null)
                permissionExtra = "";
            return string.Format(kManifestTemplate, metaDataExtra, permissionExtra);
        }

#if UNITY_ANDROID
        [Test]
        public void AppendMetadataToManifest_WhenSameValue_Works()
        {
            string sourceXmlContent = GetSourceXml(kRescheduleOnRestartTrue, null);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);


            AndroidNotificationPostProcessor.AppendAndroidMetadataField(null, xmlDoc, "reschedule_notifications_on_restart", "true");

            Assert.IsTrue(xmlDoc.InnerXml.Contains(kRescheduleOnRestartTrue));
        }

        [Test]
        public void AppendMetadataToManifest_WhenOtherValue_Works()
        {
            string sourceXmlContent = GetSourceXml(kRescheduleOnRestartFalse, null);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);


            AndroidNotificationPostProcessor.AppendAndroidMetadataField(null, xmlDoc, "reschedule_notifications_on_restart", "true");

            Assert.IsFalse(xmlDoc.InnerXml.Contains(kRescheduleOnRestartFalse));
            Assert.IsTrue(xmlDoc.InnerXml.Contains(kRescheduleOnRestartTrue));
        }

        [Test]
        public void AppendMetadataToManifest_WhenNotPresent_Works()
        {
            string sourceXmlContent = GetSourceXml(null, null);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);


            AndroidNotificationPostProcessor.AppendAndroidMetadataField(null, xmlDoc, "reschedule_notifications_on_restart", "true");

            Assert.IsTrue(xmlDoc.InnerXml.Contains(kRescheduleOnRestartTrue));
        }

        [Test]
        public void AppendMetadataToManifest_WhenOtherFieldPresentWorks()
        {
            string sourceXmlContent = GetSourceXml(kRescheduleOnRestartTrue, null);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);


            AndroidNotificationPostProcessor.AppendAndroidMetadataField(null, xmlDoc, "do_something", "true");

            Assert.IsTrue(xmlDoc.InnerXml.Contains(kRescheduleOnRestartTrue));
            Assert.IsTrue(xmlDoc.InnerXml.Contains("<meta-data android:name=\"do_something\" android:value=\"true\" />"));
        }

        [Test]
        public void AppendPermissionToManifest_WhenNoPresentWorks()
        {
            string sourceXmlContent = GetSourceXml(null, null);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);


            AndroidNotificationPostProcessor.AppendAndroidPermissionField(null, xmlDoc, "android.permission.RECEIVE_BOOT_COMPLETED");

            Assert.IsTrue(xmlDoc.InnerXml.Contains(kReceiveBookCompletedPermission));
        }

        [Test]
        public void AppendPermissionToManifest_WhenAlreadyPresentWorks()
        {
            string sourceXmlContent = GetSourceXml(null, kReceiveBookCompletedPermission);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);


            AndroidNotificationPostProcessor.AppendAndroidPermissionField(null, xmlDoc, "android.permission.RECEIVE_BOOT_COMPLETED");

            Assert.IsTrue(xmlDoc.InnerXml.Contains(kReceiveBookCompletedPermission));
        }

        [Test]
        public void InjectAndroidManifest_AddsScheduleExactWhenEnabled()
        {
            InjectAndroidManifest_AddsPermissionWhenEnabled(AndroidExactSchedulingOption.AddScheduleExactPermission, kScheduleExactAlarmPermission);
        }

        [Test]
        public void InjectAndroidManifest_AddsUseExactWhenEnabled()
        {
            InjectAndroidManifest_AddsPermissionWhenEnabled(AndroidExactSchedulingOption.AddUseExactAlarmPermission, kUseExactAlarmPermission);
        }

        [Test]
        public void InjectAndroidManifest_AddsBatteryOptimizationsWhenEnabled()
        {
            InjectAndroidManifest_AddsPermissionWhenEnabled(AndroidExactSchedulingOption.AddRequestIgnoreBatteryOptimizationsPermission, kRequestIgnoreBatteryOptimizationsPermission);
            //InjectAndroidManifest_AddsPermissionWhenEnabled(AndroidExactSchedulingOption.AddRequestIgnoreBatteryOptimizationsPermission, "android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS");
        }

        public void InjectAndroidManifest_AddsPermissionWhenEnabled(AndroidExactSchedulingOption flag, string permission)
        {
            string sourceXmlContent = GetSourceXml(null, null);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);
            var settings = new AndroidNotificationPostProcessor.ManifestSettings()
            {
                ExactAlarm = AndroidExactSchedulingOption.ExactWhenAvailable | flag,
            };

            AndroidNotificationPostProcessor.InjectAndroidManifest("test", xmlDoc, settings);

            Assert.IsTrue(xmlDoc.InnerXml.Contains(kExactSchedulingOn));
            Assert.IsTrue(xmlDoc.InnerXml.Contains(permission));
        }

        [Test]
        public void InjectAndroidManifest_DoesNotAddScheduleExactWhenExactNotEnabled()
        {
            InjectAndroidManifest_DoesNotAddPermissionWhenExactNotEnabled("android.permission.SCHEDULE_EXACT_ALARM");
        }

        [Test]
        public void InjectAndroidManifest_DoesNotAddUseExactWhenExactNotEnabled()
        {
            InjectAndroidManifest_DoesNotAddPermissionWhenExactNotEnabled("android.permission.USE_EXACT_ALARM");
        }

        [Test]
        public void InjectAndroidManifest_DoesNotAddBatteryOptimizationsWhenExactNotEnabled()
        {
            InjectAndroidManifest_DoesNotAddPermissionWhenExactNotEnabled("android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS");
        }

        public void InjectAndroidManifest_DoesNotAddPermissionWhenExactNotEnabled(string permission)
        {
            string sourceXmlContent = GetSourceXml(null, null);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);
            var settings = new AndroidNotificationPostProcessor.ManifestSettings()
            {
                // AndroidExactSchedulingOption.ExactWhenAvailable absent, so this one is ignored
                ExactAlarm = AndroidExactSchedulingOption.AddScheduleExactPermission,
            };

            AndroidNotificationPostProcessor.InjectAndroidManifest("test", xmlDoc, settings);

            Assert.IsTrue(xmlDoc.InnerXml.Contains(kExactSchedulingOff));
            Assert.IsFalse(xmlDoc.InnerXml.Contains(permission));
        }

        static readonly string[] kProguardContents = new[]
        {
            "-keep class bitter.jnibridge.* { *; }",
            "-keep class com.unity3d.player.* { *; }",
            "-keep class org.fmod.* { *; }",
        };

        [Test]
        public void InjectProguard_WhenMissing_AddsNotifications()
        {
            string[] result = kProguardContents;
            bool ret = AndroidNotificationPostProcessor.InjectProguard(ref result);

            Assert.IsTrue(ret);
            Assert.IsNotNull(result);
            Assert.IsTrue(result[result.Length - 2].Contains("com.unity.androidnotifications.UnityNotificationManager"));
            Assert.IsTrue(result[result.Length - 1].Contains("com.unity.androidnotifications.NotificationCallback"));

            // try again, now should not modify
            ret = AndroidNotificationPostProcessor.InjectProguard(ref result);
            Assert.IsFalse(ret);
        }

#endif
    }
}
