@echo off

msbuild /t:updateuid ..\EJPClient\EJPClient.csproj
msbuild /t:updateuid ..\EJPControls\EJPControls.csproj



