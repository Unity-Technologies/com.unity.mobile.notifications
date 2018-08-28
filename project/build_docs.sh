#!/usr/bin/env bash
set -xeuo pipefail

brew install doxygen
cd com.unity.android.notifications
doxygen Doxyfile