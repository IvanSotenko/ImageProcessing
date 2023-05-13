#!/usr/bin/env bash

set -eu
set -o pipefail

echo "Restoring dotnet tools..."
dotnet tool restore

PATH_TO_THE_TEST_IMAGES_FOLDER=../../../../../../../ImageProcessing/testImages

FAKE_DETAILED_ERRORS=true PATH_TO_THE_TEST_IMAGES_FOLDER=$PATH_TO_THE_TEST_IMAGES_FOLDER dotnet run --project ./build/build.fsproj -- -t "$@"
