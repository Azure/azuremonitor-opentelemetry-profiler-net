SETLOCAL
@ECHO OFF
SET CONFIG=%1
SET REBUILD=%2
IF '%CONFIG%' == '' GOTO HELP
IF '%REBUILD%' == '' SET REBUILD=TRUE

ECHO Target Configuration: %CONFIG%. ReBuild: %REBUILD%
SET TEMP_OUT=%~dp0..\Out
mkdir %TEMP_OUT%
ECHO Output Folder: %TEMP_OUT%

pushd %~dp0

SET UPLOADER_FOLDER=%~dp0..\..\ServiceProfiler.EventPipe.Upload
IF '%REBUILD%' == 'TRUE' (
    dotnet restore "%UPLOADER_FOLDER%"
    dotnet build -c %CONFIG% --no-restore "%UPLOADER_FOLDER%"
    dotnet publish --no-restore --no-build "%UPLOADER_FOLDER%" -c %CONFIG% -f net8.0
)

del %TEMP_OUT%\TraceUpload*.zip /Q
7za a -tzip %TEMP_OUT%\TraceUpload30.zip "%UPLOADER_FOLDER%\bin\%CONFIG%\net8.0\publish\*"
dir %TEMP_OUT%\TraceUpload*.zip
popd

GOTO EXIT

:HELP
ECHO Usage
SET HELPMSG="PackUploader <Debug|Release> [TRUE|FALSE]"
ECHO %HELPMSG%
GOTO EXIT

:EXIT
ENDLOCAL
