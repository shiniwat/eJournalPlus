@echo off

cd ..\EJPClient\bin\%1
if NOT EXIST LocBaml.EXE xcopy ..\..\..\Localization\LocBaml.exe .
LocBaml.exe /parse ja-JP\eJournalPlus.resources.dll /out:..\..\..\Localization\EJPClient.parse.csv
cd ..\..\..\Localization



