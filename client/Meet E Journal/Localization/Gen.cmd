@echo off

cd ..\EJPClient\bin\%1
if NOT EXIST LocBaml.EXE xcopy ..\..\..\Localization\LocBaml.exe .
if NOT EXIST en-US md en-US
LocBaml.exe /generate ja-JP\eJournalPlus.resources.dll /trans:..\..\..\Localization\EJPClient.en-US.txt /out:en-US /cul:en-US
LocBaml.exe /generate ja-JP\EjpControls.resources.dll /trans:..\..\..\Localization\EJPControls.en-US.txt /out:en-US /cul:en-US

cd ..\..\..\Localization


