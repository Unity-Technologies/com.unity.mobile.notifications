# FAQ

**Why are small Android icons white in the editor notification settings preview ?**

Small notification icons are [required](https://material.io/design/platform-guidance/android-notifications.html#anatomy-of-a-notification) to be monochrome and Android ignores all non-alpha channels in the icon image, therefore Unity automatically strips all  RGB channels. You can also provide icons by putting them in the `\Assets\Plugins\Android\res\drawable-{scaleFactor}`folder in that case they will not be automatically processed, however icons which contain non alpha channel will not be correctly displayed on Android 5.0 and above.

The notification color can be modified by setting the `AndroidNotification.Color` property.



**Why are Notifications not delivered on certain Huawei and Xiaomi phones when my app is closed and not running in the background ?**

Seems that Huawei (including Honor) and Xiaomi utilize aggresive batter saver [techniques](https://stackoverflow.com/questions/47145722/how-to-deal-with-huaweis-and-xiaomis-battery-optimizations)  which restrict app background activities unless the app has been whitelisted by the user in device settings. This means that any scheduled notifications will not be delivered if the app is closed or not running in the background. We are not aware of any way to workaround this behaviour besides encouraging the user to whitelist your app.

**What can I do if notifications with a location trigger don’t work?**

Make sure you add the CoreLocation framework to your Project. You can do this in the **Mobile Notification Settings** menu in the Unity Editor (menu: **Edit** > **Project Settings** > **Mobile Notification Settings**) Alternatively, add it manually to the Xcode project, or use the Unity Xcode API. You also need to use the [Location Service API](https://docs.unity3d.com/ScriptReference/LocationService.html) to request permission for your app to access location data.
