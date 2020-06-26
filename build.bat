@echo off
Color 07
if "%1"=="" (set para="Default") else set para=%1 
cls
 ".nuget\NuGet.exe" "Install" "FAKE" "-OutputDirectory" "packages" "-ExcludeVersion"
"fake.exe" build.fsx %para%
pause