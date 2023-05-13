echo Restoring dotnet tools...
dotnet tool restore

set PATH_TO_THE_TEST_IMAGES_FOLDER=../../../../../../../ImageProcessing/testImages

dotnet run --project ./build/build.fsproj -- -t %*
