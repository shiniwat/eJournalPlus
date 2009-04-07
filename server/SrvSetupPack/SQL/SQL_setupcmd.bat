@ECHO OFF

setlocal
REM <copyright file="SQL_setup.bat" company="Microsoft">
REM Copyright (c) Microsoft Corporation. All rights reserved.
REM </copyright>
REM <summary> 
REM Create database for MEET eJournal Server
REM </summary>

set DBNAME=meetMasterData

IF NOT EXIST .\Logs\Nul mkdir Logs

IF "%1" == "" GOTO USAGE
IF "%2" == "" GOTO USAGE
IF "%3" == "" GOTO USAGE

GOTO VALIDPARAM

:USAGE
@ECHO ON
@ECHO	*********************************************************
@ECHO	* USAGE: SQL_Setup {server} {sa} {sa password}
@ECHO	*   Where: server: database server name (could be localhost)
@ECHO	*          sa    : DB administrator name: usually == sa
@ECHO	*          sa password : administrator password
@ECHO	*********************************************************
@ECHO OFF
GOTO ENDSCRIPT

:VALIDPARAM
@ECHO ON
@ECHO	*********************************************************
@ECHO	*	ServerName 	  	:	%1		*
@ECHO	*	UserName	  	:	%2		*
@ECHO	*	Password		:	*********	*
@ECHO	*********************************************************
@ECHO OFF

@ECHO ON
@ECHO	*	Creating database....					*
@ECHO OFF

sqlcmd -S %1 -U %2 -P %3 -i createDB.sql -u -o ./Logs/createDB.log
IF ERRORLEVEL 1 GOTO DBERROR 

rem CD Create Script
@ECHO ON
@ECHO	*	Creating tables....						*
@ECHO OFF

sqlcmd -S %1 -U %2 -P %3 -d %DBNAME% -i initTables.sql -u -o ./Logs/InitTables.log

@ECHO ON
@ECHO	*	Creating SPs....						*
@ECHO OFF

sqlcmd -S %1 -U %2 -P %3 -d %DBNAME% -i createSPs.sql -u -o ./Logs/CreateSPs.log 

@ECHO ON
@ECHO	*	Inserting data into tables....			*
@ECHO OFF

sqlcmd -S %1 -U %2 -P %3 -d %DBNAME% -i sampleData.sql -u -o ./Logs/SampleData.log 

@ECHO ON
@ECHO	*	Creating user tables....				*
@ECHO OFF

sqlcmd -S %1 -U %2 -P %3 -i createUserTable.sql -u -o ./Logs/userTable.log

@ECHO ON
@ECHO	*	Inserting user data into tables....		    *
@ECHO OFF
set USERDATA=meetF_user_205
bcp %USERDATA%.dbo.Assignments in 205_Assignments.bcp -S %1 -U %2 -P %3 -E -n

bcp %USERDATA%.dbo.StudyMetaData in 205_StudyMetaData.bcp -S %1 -U %2 -P %3 -E -n

@ECHO ON
@ECHO	*	Inserting another user data into tables....	*
@ECHO OFF
set USERDATA=meetF_user_206
bcp %USERDATA%.dbo.Assignments in 206_Assignments.bcp -S %1 -U %2 -P %3 -E -n

bcp %USERDATA%.dbo.StudyMetaData in 206_StudyMetaData.bcp -S %1 -U %2 -P %3 -E -n

@ECHO ON
@ECHO	*	Database setup completed ....			*
@ECHO OFF
GOTO ENDSCRIPT

:DBERR
@ECHO ON
@ECHO	*	Database setup failed ....				*
@ECHO OFF

:ENDSCRIPT
endlocal
