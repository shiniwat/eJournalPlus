@echo off

cd ..\EJPClient\bin\x86\%1
if NOT EXIST LocBaml.EXE xcopy ..\..\..\..\Localization\x86\LocBaml.exe .
if NOT EXIST en-US md en-US
LocBaml.exe /generate ja-JP\eJournalPlus.resources.dll /trans:..\..\..\..\Localization\EJPClient.en-US.txt /out:en-US /cul:en-US
LocBaml.exe /generate ja-JP\EjpControls.resources.dll /trans:..\..\..\..\Localization\EJPControls.en-US.txt /out:en-US /cul:en-US

cd ..\..\..\..\Localization


