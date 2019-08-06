using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Notifications;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

#if PLATFORM_IOS
using Unity.Notifications.iOS;
using UnityEditor.iOS.Xcode;
#endif

public class iOSNotificationPostProcess : MonoBehaviour {

	[PostProcessBuild]
	public static void OnPostprocessBuild (BuildTarget buildTarget, string path)
	{
		#if PLATFORM_IOS
		if (buildTarget == BuildTarget.iOS) {
			
			var projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
				
			var proj = new PBXProject ();
			proj.ReadFromString(File.ReadAllText(projPath));


			string mainTarget;
			string unityFrameworkTarget;
			
			var unityMainTargetGuidMethod = proj.GetType().GetMethod("GetUnityMainTargetGuid");
			var unityFrameworkTargetGuidMethod = proj.GetType().GetMethod("GetUnityFrameworkTargetGuid");

				
			if (unityMainTargetGuidMethod != null && unityFrameworkTargetGuidMethod != null)
			{
				mainTarget = (string)unityMainTargetGuidMethod.Invoke(proj, null);
				unityFrameworkTarget = (string)unityFrameworkTargetGuidMethod.Invoke(proj, null);

			}
			else
			{
				mainTarget = proj.TargetGuidByName ("Unity-iPhone");
				unityFrameworkTarget = mainTarget;

			}
						
			var settings = UnityNotificationEditorManager.Initialize().iOSNotificationEditorSettingsFlat;
			
			var addPushNotificationCapability = (bool)settings
				                            .Find(i => i.key == "UnityAddRemoteNotificationCapability").val == true;;
			
			var needLocationFramework = (bool)settings
				                            .Find(i => i.key == "UnityUseLocationNotificationTrigger").val == true;;

			proj.AddFrameworkToProject(unityFrameworkTarget, "UserNotifications.framework", true);

			if (needLocationFramework)
				proj.AddFrameworkToProject(unityFrameworkTarget, "CoreLocation.framework", false);
			
			File.WriteAllText (projPath, proj.WriteToString ());

			if (addPushNotificationCapability)
			{
				var useReleaseAPSEnvSetting = settings
					.Find(i => i.key == "UnityUseAPSReleaseEnvironment");
				var useReleaseAPSEnv = false;

				if (useReleaseAPSEnvSetting != null)
					useReleaseAPSEnv = (bool) useReleaseAPSEnvSetting.val;

				var entitlementsFileName = proj.GetBuildPropertyForConfig(mainTarget, "CODE_SIGN_ENTITLEMENTS");

				if (entitlementsFileName == null)
				{
					var bundleIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
					entitlementsFileName = string.Format("{0}.entitlements",
						bundleIdentifier.Substring(bundleIdentifier.LastIndexOf(".") + 1));
				}

				var pbxPath = PBXProject.GetPBXProjectPath(path);
				var capManager = new ProjectCapabilityManager(pbxPath, entitlementsFileName, "Unity-iPhone");
				capManager.AddPushNotifications(!useReleaseAPSEnv);
				capManager.WriteToFile();
			}

			// Get plist
			var plistPath = path + "/Info.plist";
			var plist = new PlistDocument();
			plist.ReadFromString(File.ReadAllText(plistPath));
       
			// Get root
			var rootDict = plist.root;
			
			var requiredVersion = new Version(8, 0);
			bool hasMinOSVersion;
			
			try
			{
				var currentVersion = new Version(PlayerSettings.iOS.targetOSVersionString);
				hasMinOSVersion = currentVersion >= requiredVersion;
			}
			catch (Exception)
			{
				hasMinOSVersion = false;
			}
			
			if (!hasMinOSVersion)
				Debug.Log("UserNotifications are only available on iOSSettings 10 and above, please make sure that you set a correct `Target minimum iOSSettings Version` in Player Settings.");

			foreach (var setting in settings)
			{				
				if (setting.val.GetType() == typeof(bool))
					rootDict.SetBoolean(setting.key, (bool) setting.val);
				else if (setting.val.GetType() == typeof(PresentationOption) || setting.val.GetType() == typeof(AuthorizationOption))
					rootDict.SetInteger(setting.key, (int) setting.val);
			}

			if (addPushNotificationCapability)
			{
				PlistElementArray currentBacgkgroundModes = (PlistElementArray) rootDict["UIBackgroundModes"];

				if (currentBacgkgroundModes == null)
					currentBacgkgroundModes = rootDict.CreateArray("UIBackgroundModes");
				
				currentBacgkgroundModes.AddString("remote-notification");
			}

			// Write to file
			File.WriteAllText(plistPath, plist.WriteToString());
		
			//Get Preprocessor.h
			var preprocessorPath = path + "/Classes/Preprocessor.h";
			var preprocessor = File.ReadAllText(preprocessorPath);

			if (needLocationFramework)
			{
				if (preprocessor.Contains("UNITY_USES_LOCATION"))
					preprocessor = preprocessor.Replace("UNITY_USES_LOCATION 0", "UNITY_USES_LOCATION 1");
			}

			preprocessor = preprocessor.Replace("UNITY_USES_REMOTE_NOTIFICATIONS 0", "UNITY_USES_REMOTE_NOTIFICATIONS 1");
			File.WriteAllText(preprocessorPath, preprocessor);
		}
		#endif

	}

}
