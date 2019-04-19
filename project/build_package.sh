#!/usr/bin/env bash
#set -xeuo pipefail

printf "Attempting to build package:"

cd project
./gradlew clean
./gradlew assembleRelease
