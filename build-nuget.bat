@echo off
cls
".nuget\NuGet.exe" "Install" "FAKE" "-OutputDirectory" "packages" "-ExcludeVersion"
"packages\FAKE\tools\Fake.exe" target=LocalDeploy build.fsx
pause