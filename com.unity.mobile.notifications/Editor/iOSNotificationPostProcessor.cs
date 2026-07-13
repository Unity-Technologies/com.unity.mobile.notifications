#if UNITY_IOS
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using Unity.Notifications;
using Unity.Notifications.iOS;

public class iOSNotificationPostProcessor : MonoBehaviour
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget != BuildTarget.iOS)
            return;

        var settings = NotificationSettingsManager.Initialize().iOSNotificationSettingsFlat;

        var needLocationFramework = (bool)settings.Find(i => i.Key == NotificationSettings.iOSSettings.USE_LOCATION_TRIGGER).Value;
        var addPushNotificationCapability = (bool)settings.Find(i => i.Key == NotificationSettings.iOSSettings.ADD_PUSH_CAPABILITY).Value;
        var addTimeSensitiveEntitlement = (bool)settings.Find(i => i.Key == NotificationSettings.iOSSettings.ADD_TIME_SENSITIVE_ENTITLEMENT).Value;

        var useReleaseAPSEnv = false;
        if (addPushNotificationCapability)
        {
            var useReleaseAPSEnvSetting = settings.Find(i => i.Key == NotificationSettings.iOSSettings.USE_APS_RELEASE);
            if (useReleaseAPSEnvSetting != null)
                useReleaseAPSEnv = (bool)useReleaseAPSEnvSetting.Value;
        }

        PatchPBXProject(path, needLocationFramework, addPushNotificationCapability, useReleaseAPSEnv, addTimeSensitiveEntitlement);
        PatchPlist(path, settings, addPushNotificationCapability);
        PatchPreprocessor(path, needLocationFramework, addPushNotificationCapability);
    }

    private static void PatchPBXProject(string path, bool needLocationFramework, bool addPushNotificationCapability, bool useReleaseAPSEnv, bool addTimeSensitiveEntitlement)
    {
        var pbxProjectPath = PBXProject.GetPBXProjectPath(path);
        List<string> swiftPropertiesToAdd = new();

        var needsToWriteChanges = false;

        var pbxProject = new PBXProject();
        pbxProject.ReadFromString(File.ReadAllText(pbxProjectPath));

        string mainTarget;
        string unityFrameworkTarget;

        var unityMainTargetGuidMethod = pbxProject.GetType().GetMethod("GetUnityMainTargetGuid");
        var unityFrameworkTargetGuidMethod = pbxProject.GetType().GetMethod("GetUnityFrameworkTargetGuid");

        if (unityMainTargetGuidMethod != null && unityFrameworkTargetGuidMethod != null)
        {
            mainTarget = (string)unityMainTargetGuidMethod.Invoke(pbxProject, null);
            unityFrameworkTarget = (string)unityFrameworkTargetGuidMethod.Invoke(pbxProject, null);
        }
        else
        {
            mainTarget = pbxProject.TargetGuidByName("Unity-iPhone");
            unityFrameworkTarget = mainTarget;
        }

        // Add necessary frameworks.
        if (!pbxProject.ContainsFramework(unityFrameworkTarget, "UserNotifications.framework"))
        {
            pbxProject.AddFrameworkToProject(unityFrameworkTarget, "UserNotifications.framework", true);
            needsToWriteChanges = true;
        }
        if (needLocationFramework && !pbxProject.ContainsFramework(unityFrameworkTarget, "CoreLocation.framework"))
        {
            pbxProject.AddFrameworkToProject(unityFrameworkTarget, "CoreLocation.framework", false);
            swiftPropertiesToAdd.Add("-DUNITY_USES_LOCATION");
            needsToWriteChanges = true;
        }

        var entitlementsFileName = pbxProject.GetBuildPropertyForAnyConfig(mainTarget, "CODE_SIGN_ENTITLEMENTS");
        if (entitlementsFileName == null)
        {
            var bundleIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
            entitlementsFileName = string.Format("{0}.entitlements", bundleIdentifier.Substring(bundleIdentifier.LastIndexOf(".") + 1));
        }

        // Update the entitlements file.
        if (addPushNotificationCapability)
        {
            swiftPropertiesToAdd.Add("-DUNITY_USES_REMOTE_NOTIFICATIONS");
            var capManager = new ProjectCapabilityManager(pbxProjectPath, entitlementsFileName, "Unity-iPhone");
            capManager.AddPushNotifications(!useReleaseAPSEnv);
            capManager.WriteToFile();
        }

        if (addTimeSensitiveEntitlement)
        {
            var entitlementsFile = new PlistDocument();
            var entitlementsFilePath = Path.Combine(path, entitlementsFileName);
            if (File.Exists(entitlementsFilePath))
                entitlementsFile.ReadFromFile(entitlementsFilePath);
            var entitlement = entitlementsFile.root["com.apple.developer.usernotifications.time-sensitive"] as PlistElementBoolean;
            if (entitlement == null || entitlement.AsBoolean() == false)
            {
                entitlementsFile.root["com.apple.developer.usernotifications.time-sensitive"] = new PlistElementBoolean(true);
                entitlementsFile.WriteToFile(entitlementsFilePath);
            }
            if (pbxProject.GetBuildPropertyForAnyConfig(mainTarget, "CODE_SIGN_ENTITLEMENTS") == null)
            {
                pbxProject.AddBuildProperty(mainTarget, "CODE_SIGN_ENTITLEMENTS", entitlementsFileName);
                needsToWriteChanges = true;
            }
        }

        if (swiftPropertiesToAdd.Count > 0)
        {
            pbxProject.UpdateBuildProperty(unityFrameworkTarget, "OTHER_SWIFT_FLAGS", swiftPropertiesToAdd, new string[0]);
            needsToWriteChanges = true;
        }

        if (needsToWriteChanges)
            pbxProject.WriteToFile(pbxProjectPath);
    }

    private static void PatchPlist(string path, List<Unity.Notifications.NotificationSetting> settings, bool addPushNotificationCapability)
    {
        var plistPath = path + "/Info.plist";
        var plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        var rootDict = plist.root;
        var needsToWriteChanges = false;

        // Add all the settings to the plist.
        foreach (var setting in settings)
        {
            if (ShouldAddSettingToPlist(setting, rootDict))
            {
                needsToWriteChanges = true;
                if (setting.Value.GetType() == typeof(bool))
                {
                    rootDict.SetBoolean(setting.Key, (bool)setting.Value);
                }
                else if (setting.Value.GetType() == typeof(PresentationOption) ||
                         setting.Value.GetType() == typeof(AuthorizationOption))
                {
                    rootDict.SetInteger(setting.Key, (int)setting.Value);
                }
            }
        }

        // Add "remote-notification" to the list of supported UIBackgroundModes.
        if (addPushNotificationCapability)
        {
            PlistElementArray currentBackgroundModes = (PlistElementArray)rootDict["UIBackgroundModes"];
            if (currentBackgroundModes == null)
                currentBackgroundModes = rootDict.CreateArray("UIBackgroundModes");

            var remoteNotificationElement = new PlistElementString("remote-notification");
            if (!currentBackgroundModes.values.Contains(remoteNotificationElement))
            {
                currentBackgroundModes.values.Add(remoteNotificationElement);
                needsToWriteChanges = true;
            }
        }

        if (needsToWriteChanges)
            File.WriteAllText(plistPath, plist.WriteToString());
    }

    // If the plist doesn't contain the key, or it's value is different, we should add/overwrite it.
    private static bool ShouldAddSettingToPlist(Unity.Notifications.NotificationSetting setting,
        PlistElementDict rootDict)
    {
        if (!rootDict.values.ContainsKey(setting.Key))
            return true;
        else if (setting.Value.GetType() == typeof(bool))
            return !rootDict.values[setting.Key].AsBoolean().Equals((bool)setting.Value);
        else if (setting.Value.GetType() == typeof(PresentationOption) || setting.Value.GetType() == typeof(AuthorizationOption))
            return !rootDict.values[setting.Key].AsInteger().Equals((int)setting.Value);
        else
            return false;
    }

    private static void PatchPreprocessor(string path, bool needLocationFramework, bool addPushNotificationCapability)
    {
        if (!(needLocationFramework || addPushNotificationCapability))
            return;

        var preprocessorPath = Path.Combine(path, "Classes/Preprocessor.h");
#if UNITY_6000_5_OR_NEWER
        if (PlayerSettings.xcodeProjectType == XcodeProjectType.Swift)
            preprocessorPath = Path.Combine(path, "UnityFramework/Prefix/Preprocessor.h");
#endif

        var preprocessor = File.ReadAllText(preprocessorPath);
        var needsToWriteChanges = false;

        if (needLocationFramework && preprocessor.Contains("UNITY_USES_LOCATION"))
        {
            preprocessor = preprocessor.Replace("UNITY_USES_LOCATION 0", "UNITY_USES_LOCATION 1");
            needsToWriteChanges = true;
        }

        if (addPushNotificationCapability && preprocessor.Contains("UNITY_USES_REMOTE_NOTIFICATIONS"))
        {
            preprocessor =
                preprocessor.Replace("UNITY_USES_REMOTE_NOTIFICATIONS 0", "UNITY_USES_REMOTE_NOTIFICATIONS 1");
            needsToWriteChanges = true;
        }

        if (needsToWriteChanges)
            File.WriteAllText(preprocessorPath, preprocessor);
    }
}
#endif
