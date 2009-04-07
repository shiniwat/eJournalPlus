@echo off

cd ..\EJPClient\bin\%1
if NOT EXIST LocBaml.EXE xcopy ..\..\..\Localization\LocBaml.exe .
LocBaml.exe /parse ja-JP\eJournalPlus.resources.dll /out:..\..\..\Localization\EJPClient.parse.csv
LocBaml.exe /parse ja-JP\EjpControls.resources.dll /out:..\..\..\Localization\EJPControls.parse.csv

cd ..\..\..\Localization



