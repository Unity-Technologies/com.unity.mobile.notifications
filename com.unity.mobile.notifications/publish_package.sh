#!/usr/bin/env bash
set -xeuo pipefail

brew install jq

cd com.unity.mobile.notifications
ls
ls Runtime/Android/Plugins/Android/

if [ -f Runtime/Android/Plugins/Android/androidnotifications-release.aar ];
then
    echo "Built AAR package found in Plugins folder."
else
	echo "Built AAR package not found in Plugins folder!"
    exit 1
fi


REPO_REV=$(git rev-parse HEAD)
REPO_URL="gitlab.cds.internal.unity3d.com/upm-packages/mobile/mobile-notifications.git"

MANIFEST=$(jq -r ".repository += {\"url\": \"$REPO_URL\", \"revision\": \"$REPO_REV\"}" package.json)
echo "$MANIFEST" > package.json


curl -u $BIN_USERNAME@unity:$BIN_API_KEY https://packages.unity.com/auth > .npmrc
sed -i -e 's/npm\/unity\/unity/npm\/unity\/unity-staging/g' .npmrc
npm publish upm-ci\~/packages/com.unity.mobile.notifications-1.0.0-preview.15.1.tgz
