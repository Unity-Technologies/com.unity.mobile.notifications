#!/usr/bin/env bash
#set -xeuo pipefail
#set -x

printf "Cloning sample project:\n\n"

cd com.unity.mobile.notifications

mkdir Samples

cd Samples

git clone -b release-1-4 https://github.com/Unity-Technologies/NotificationsSamples.git tmp

rm -rf tmp/Assets/Editor
rm tmp/Assets/Editor.meta

mv tmp/Assets NotificationsSamples

mv tmp/README.md NotificationsSamples/README.md
mv tmp/License.md NotificationsSamples/License.md

rm -rf tmp

cd NotificationsSamples
touch .sample.json
echo "{\"displayName\": \"Notification Samples\",\"description\": \"Description for sample\"}" >> .sample.json
