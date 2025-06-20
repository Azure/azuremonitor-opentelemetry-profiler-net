@ECHO OFF
SETLOCAL
SET CONFIG=%1
SET PKG_TYPE=%2
SET REBUILD=%3
IF '%CONFIG%' == '' GOTO HELP
IF '%PKG_TYPE%' == '' SET PKG_TYPE=private
IF '%REBUILD%' == '' SET REBUILD=TRUE


ECHO Target Configuration: %CONFIG%. ReBuild: %REBUILD%
pushd %~dp0
CLS
ECHO Target Configuration: %CONFIG%. ReBuild: %REBUILD%
FOR /F "TOKENS=1* DELIMS= " %%A IN ('DATE/T') DO SET CDATE=%%B
FOR /F "TOKENS=1,2 eol=/ DELIMS=/ " %%A IN ('DATE/T') DO SET mm=%%B
FOR /F "TOKENS=1,2 DELIMS=/ eol=/" %%A IN ('echo %CDATE%') DO SET dd=%%B
FOR /F "TOKENS=2,3 DELIMS=/ " %%A IN ('echo %CDATE%') DO SET yyyy=%%B
FOR /F "TOKENS=1-2 delims=/:" %%a in ('echo %TIME: =0%') DO SET mytime=%%a%%b
SET CURRENT_DATE_TIME=%yyyy%%mm%%dd%%mytime%
SET ASSEMBLY_VERSION=99.%yyyy%.%mm%%dd%.%mytime%
ECHO Version:%CURRENT_DATE_TIME%
ECHO Assembly Version: %ASSEMBLY_VERSION%

SET BASE_DIR=%~dp0..
SET SLN_DIR=%BASE_DIR%\..\
SET TEMP_OUT=%BASE_DIR%\Out

ECHO Prepare output folder: %TEMP_OUT%
mkdir %TEMP_OUT% > NUL
mkdir %TEMP_OUT%\Nuget > NUL
mkdir %TEMP_OUT%\NugetNoSymbols > NUL
mkdir %TEMP_OUT%\Nuget.BAK > NUL
move %TEMP_OUT%\Nuget\*.nupkg %TEMP_OUT%\Nuget.BAK\ > NUL
del %TEMP_OUT%\Nuget\*.nupkg /S /Q /F > NUL

DEL %BASE_DIR%\ServiceProfiler.EventPipe.Client\bin\%CONFIG%\*.nupkg /Q > NUL
DEL %BASE_DIR%\ServiceProfiler.EventPipe.AspNetCore\bin\%CONFIG%\*.nupkg /Q > NUL

ECHO Restore nuget packages

ECHO dotnet restore %SLN_DIR%\Microsoft.ApplicationInsights.Profiler.sln
dotnet restore %SLN_DIR%\Microsoft.ApplicationInsights.Profiler.sln

IF '%REBUILD%' == 'TRUE' (
    ECHO Rebuild is set to 'TRUE'
    dotnet build --no-restore %SLN_DIR%\Microsoft.ApplicationInsights.Profiler.sln /p:Configuration=%CONFIG% /p:AssemblyVersion=%ASSEMBLY_VERSION%
)

IF '%ERRORLEVEL%' NEQ '0' GOTO ERR

dotnet pack %BASE_DIR%\ServiceProfiler.EventPipe.Client --include-symbols --no-build --no-restore --version-suffix -%PKG_TYPE%-%CURRENT_DATE_TIME% -c %CONFIG%
dotnet pack %BASE_DIR%\ServiceProfiler.EventPipe.AspNetCore --include-symbols --no-build --no-restore --version-suffix -%PKG_TYPE%-%CURRENT_DATE_TIME% -c %CONFIG%

COPY %BASE_DIR%\ServiceProfiler.EventPipe.Client\bin\%CONFIG%\*.symbols.nupkg %TEMP_OUT%\Nuget\
IF '%ERRORLEVEL%' NEQ '0' GOTO ERR
COPY %BASE_DIR%\ServiceProfiler.EventPipe.AspNetCore\bin\%CONFIG%\*.symbols.nupkg %TEMP_OUT%\Nuget\
IF '%ERRORLEVEL%' NEQ '0' GOTO ERR
COPY %BASE_DIR%\ServiceProfiler.EventPipe.Client\bin\%CONFIG%\*.nupkg %TEMP_OUT%\NugetNoSymbols\
IF '%ERRORLEVEL%' NEQ '0' GOTO ERR
COPY %BASE_DIR%\ServiceProfiler.EventPipe.AspNetCore\bin\%CONFIG%\*.nupkg %TEMP_OUT%\NugetNoSymbols\
IF '%ERRORLEVEL%' NEQ '0' GOTO ERR
DEL %TEMP_OUT%\NugetNoSymbols\*.symbols.nupkg /Q > NUL
IF '%ERRORLEVEL%' NEQ '0' GOTO ERR

mkdir %TEMP_OUT%\Nuget.publish
COPY %BASE_DIR%\ServiceProfiler.EventPipe.Client\bin\%CONFIG%\*.symbols.nupkg %TEMP_OUT%\Nuget.publish\
IF '%ERRORLEVEL%' NEQ '0' GOTO ERR
COPY %BASE_DIR%\ServiceProfiler.EventPipe.AspNetCore\bin\%CONFIG%\*.symbols.nupkg %TEMP_OUT%\Nuget.publish\
IF '%ERRORLEVEL%' NEQ '0' GOTO ERR

IF '%ERRORLEVEL%' NEQ '0' GOTO ERR

popd
IF '%ERRORLEVEL%' == '0' ECHO Package succeeded :-)
GOTO EXIT

:HELP
ECHO Usage
SET "HELPMSG=PackNugetPackage <Debug|Release> [PackageType] [TRUE|FALSE]"
SETLOCAL EnableDelayedExpansion
(
    ECHO !HELPMSG!
    ECHO By default: PackageType=private, REBUILD=TRUE
)
GOTO EXIT

:ERR
ECHO Package failed :-(
GOTO EXIT

:EXIT
ENDLOCAL
