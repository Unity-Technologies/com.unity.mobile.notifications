#!/usr/bin/env bash
#set -xeuo pipefail
#set -x

printf "Cloning sample project:\n\n"

cd com.unity.mobile.notifications

mkdir Samples

cd Samples

git clone https://github.com/Unity-Technologies/NotificationsSamples.git tmp

rm -rf tmp/Assets/Editor
rm -rf tmp/Assets/Plugins

mv tmp/Assets NotificationsSamples

mv tmp/README.md NotificationsSamples/README.md
mv tmp/License.md NotificationsSamples/License.md

rm -rf tmp

cd NotificationsSamples
touch .sample.json
echo "{\"displayName\": \"Notification Samples\",\"description\": \"Description for sample\"}" >> .sample.json
