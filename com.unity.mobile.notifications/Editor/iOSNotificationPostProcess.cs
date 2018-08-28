using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Notifications.Android;
using Unity.Notifications.iOS;
using UnityEditor;
using UnityEditor.Callbacks;

using UnityEngine;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

public class iOSNotificationPostProcess : MonoBehaviour {

	[PostProcessBuild]
	public static void OnPostprocessBuild (BuildTarget buildTarget, string path)
	{
		#if UNITY_IOS
		if (buildTarget == BuildTarget.iOS) {
			
			string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
				
			PBXProject proj = new PBXProject ();
			proj.ReadFromString (File.ReadAllText (projPath));			
			string target = proj.TargetGuidByName ("Unity-iPhone");


			var useReleaseAPSEnvSetting = UnityNotificationEditorManager.Initialize().iOSNotificationEditorSettings
				.Find(i => i.key == "UnityAPSReleaseEnvironment");
			var useReleaseAPSEnv = false;
			
			if (useReleaseAPSEnvSetting != null)
				useReleaseAPSEnv = (bool)useReleaseAPSEnvSetting.val;
			
			proj.AddFrameworkToProject(target, "UserNotifications.framework", useReleaseAPSEnv);
			
			File.WriteAllText (projPath, proj.WriteToString ());
			
			string pbxPath = PBXProject.GetPBXProjectPath(path);
			var capManager = new ProjectCapabilityManager(pbxPath, string.Format("{0}.entitlements", PlayerSettings.productName), "Unity-iPhone");
			capManager.AddPushNotifications(true);
			capManager.WriteToFile();
			
			// Get plist
			string plistPath = path + "/Info.plist";
			PlistDocument plist = new PlistDocument();
			plist.ReadFromString(File.ReadAllText(plistPath));
       
			// Get root
			PlistElementDict rootDict = plist.root;
			
			var settings = UnityNotificationEditorManager.Initialize().iOSNotificationEditorSettings;

			foreach (var setting in settings)
			{
				Debug.Log(string.Format("setting: {0} val: {1}", setting.key, setting.val.ToString()));
				
				if (setting.val.GetType() == typeof(bool))
					rootDict.SetBoolean(setting.key, (bool) setting.val);
				else if (setting.val.GetType() == typeof(PresentationOption))
					rootDict.SetInteger(setting.key, (int) setting.val);
			}

			// Write to file
			File.WriteAllText(plistPath, plist.WriteToString());
		
			//Get Preprocessor.h
			var preprocessorPath = path + "/Classes/Preprocessor.h";
			var preprocessor = File.ReadAllText(preprocessorPath);
						
			preprocessor = preprocessor.Replace("UNITY_USES_REMOTE_NOTIFICATIONS 0", "UNITY_USES_REMOTE_NOTIFICATIONS 1");
			File.WriteAllText(preprocessorPath, preprocessor);
		}
		#endif

	}

}
