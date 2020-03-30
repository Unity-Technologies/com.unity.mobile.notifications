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

#if UNITY_ANDROID
        [Test]
        public void AppendMetadataToManifest_WhenSameValue_Works()
        {
            string sourceXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />
    <meta-data android:name=""reschedule_notifications_on_restart"" android:value=""true""/>
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
</manifest>";

            string targetXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />
    <meta-data android:name=""reschedule_notifications_on_restart"" android:value=""true""/>
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
</manifest>";

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);


            AndroidNotificationPostProcessor.AppendAndroidMetadataField(null, xmlDoc, "reschedule_notifications_on_restart", "true");

            XmlDocument targetXmlDoc = new XmlDocument();
            targetXmlDoc.LoadXml(targetXmlContent);

            Assert.AreEqual(targetXmlDoc.InnerXml, xmlDoc.InnerXml);
        }

        [Test]
        public void AppendMetadataToManifest_WhenOtherValue_Works()
        {
            string sourceXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />
    <meta-data android:name=""reschedule_notifications_on_restart"" android:value=""false""/>
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
</manifest>";

            string targetXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />
    <meta-data android:name=""reschedule_notifications_on_restart"" android:value=""true""/>
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
</manifest>";

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);


            AndroidNotificationPostProcessor.AppendAndroidMetadataField(null, xmlDoc, "reschedule_notifications_on_restart", "true");

            XmlDocument targetXmlDoc = new XmlDocument();
            targetXmlDoc.LoadXml(targetXmlContent);

            Assert.AreEqual(targetXmlDoc.InnerXml, xmlDoc.InnerXml);
        }

        [Test]
        public void AppendMetadataToManifest_WhenNotPresent_Works()
        {
            string sourceXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
</manifest>";

            string targetXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />
    <meta-data android:name=""reschedule_notifications_on_restart"" android:value=""true""/>
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
</manifest>";

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);


            AndroidNotificationPostProcessor.AppendAndroidMetadataField(null, xmlDoc, "reschedule_notifications_on_restart", "true");

            XmlDocument targetXmlDoc = new XmlDocument();
            targetXmlDoc.LoadXml(targetXmlContent);

            Assert.AreEqual(targetXmlDoc.InnerXml, xmlDoc.InnerXml);
        }

        [Test]
        public void AppendMetadataToManifest_WhenOtherFieldPresentWorks()
        {
            string sourceXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />
    <meta-data android:name=""reschedule_notifications_on_restart"" android:value=""true""/>
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
</manifest>";

            string targetXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />
    <meta-data android:name=""reschedule_notifications_on_restart"" android:value=""true""/>
    <meta-data android:name=""do_something"" android:value=""true""/>
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
</manifest>";

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);


            AndroidNotificationPostProcessor.AppendAndroidMetadataField(null, xmlDoc, "do_something", "true");

            XmlDocument targetXmlDoc = new XmlDocument();
            targetXmlDoc.LoadXml(targetXmlContent);

            Assert.AreEqual(targetXmlDoc.InnerXml, xmlDoc.InnerXml);
        }

        [Test]
        public void AppendPermissionToManifest_WhenNoPresentWorks()
        {
            string sourceXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />
    <meta-data android:name=""reschedule_notifications_on_restart"" android:value=""true""/>
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
</manifest>";

            string targetXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />
    <meta-data android:name=""reschedule_notifications_on_restart"" android:value=""true""/>
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
  <uses-permission android:name=""android.permission.RECEIVE_BOOT_COMPLETED""/>
</manifest>";

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);


            AndroidNotificationPostProcessor.AppendAndroidPermissionField(null, xmlDoc, "android.permission.RECEIVE_BOOT_COMPLETED");

            XmlDocument targetXmlDoc = new XmlDocument();
            targetXmlDoc.LoadXml(targetXmlContent);

            Assert.AreEqual(targetXmlDoc.InnerXml, xmlDoc.InnerXml);
        }

        [Test]
        public void AppendPermissionToManifest_WhenAlreadyPresentWorks()
        {
            string sourceXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />
    <meta-data android:name=""reschedule_notifications_on_restart"" android:value=""true""/>
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.RECEIVE_BOOT_COMPLETED""/>
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
</manifest>";

            string targetXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
    <meta-data android:name=""unity.splash-enable"" android:value=""True"" />
    <meta-data android:name=""reschedule_notifications_on_restart"" android:value=""true""/>
  </application>
  <uses-feature android:glEsVersion=""0x00020000"" />
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
  <uses-permission android:name=""android.permission.READ_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />
<uses-permission android:name=""android.permission.RECEIVE_BOOT_COMPLETED""/>
  <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch"" android:required=""false"" />
  <uses-feature android:name=""android.hardware.touchscreen.multitouch.distinct"" android:required=""false"" />
</manifest>";

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(sourceXmlContent);


            AndroidNotificationPostProcessor.AppendAndroidPermissionField(null, xmlDoc, "android.permission.RECEIVE_BOOT_COMPLETED");

            XmlDocument targetXmlDoc = new XmlDocument();
            targetXmlDoc.LoadXml(targetXmlContent);

            Assert.AreEqual(targetXmlDoc.InnerXml, xmlDoc.InnerXml);
        }

#endif
    }
}
