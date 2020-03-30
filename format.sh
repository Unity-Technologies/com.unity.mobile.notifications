#!/bin/sh
echo "Running formatter"
perl ~/unity-meta/Tools/Format/format.pl ./com.unity.mobile.notifications/Editor ./com.unity.mobile.notifications/Runtime ./com.unity.mobile.notifications/Tests ./TestProjects/NotificationSamples/Assets
echo "Removing *.preformat.bak, *.preformat.bak.meta files"
find . -type f -name "*.preformat.bak" -delete
find . -type f -name "*.preformat.bak.meta" -delete