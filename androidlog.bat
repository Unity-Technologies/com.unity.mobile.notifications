rem skip logs collection if tests are successful, sometimes we have adb hangs/fails

if "x%YAMATO_COMMAND_BLOCK_SUCCESS%" == "x1" goto :END

%ANDROID_SDK_ROOT%\platform-tools\adb.exe connect %BOKKEN_DEVICE_IP%
%ANDROID_SDK_ROOT%\platform-tools\adb.exe logcat -d > upm-ci~/test-results/android/android_device_log.txt

:END
