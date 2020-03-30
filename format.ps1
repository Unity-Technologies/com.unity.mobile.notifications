Write-Host "Running formatter"
perl "${Home}/unity-meta/Tools/Format/format.pl" ./com.unity.mobile.notifications/Editor ./com.unity.mobile.notifications/Runtime ./com.unity.mobile.notifications/Tests ./TestProjects/NotificationSamples/Assets
Write-Host "Removing *.preformat.bak, *.preformat.bak.meta files"
Get-ChildItem . -recurse -include *.preformat.bak | Remove-Item
Get-ChildItem . -recurse -include *.preformat.bak.meta | Remove-Item