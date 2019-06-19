 echo "begin:"
npm install upm-ci-utils@stable --registry https://api.bintray.com/npm/unity/unity-npm -g
echo "installed upm-ci"
echo "publishing package"
upm-ci package publish
echo "published package"
