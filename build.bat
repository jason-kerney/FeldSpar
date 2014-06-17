@echo off
cls
".nuget\NuGet.exe" "Install" "FAKE" "-OutputDirectory" "packages" "-ExcludeVersion"
"packages\FAKE.2.18.1\tools\Fake.exe" build.fsx
pause