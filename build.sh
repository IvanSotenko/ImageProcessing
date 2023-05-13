#!/usr/bin/env bash

set -eu
set -o pipefail

echo "Restoring dotnet tools..."
dotnet tool restore

PATH_TO_THE_TEST_IMAGES_FOLDER=${PATH_TO_THE_TEST_IMAGES_FOLDER-../../../../../../../ImageProcessing/testImages}

PATH_TO_THE_TEST_IMAGES_FOLDER=$PATH_TO_THE_TEST_IMAGES_FOLDER;FAKE_DETAILED_ERRORS=true dotnet run --project ./build/build.fsproj -- -t "$@"
