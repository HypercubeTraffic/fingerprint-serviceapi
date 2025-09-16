@echo off
echo Starting BIO600 Fingerprint Web API...
echo.

REM Check if DLLs exist
echo Checking for required DLLs...
if not exist "ZAZ_FpStdLib.dll" (
    echo WARNING: ZAZ_FpStdLib.dll not found in current directory
    echo Please copy from: ..\bin\Debug\ZAZ_FpStdLib.dll
)
if not exist "GALSXXYY.dll" (
    echo WARNING: GALSXXYY.dll not found in current directory
    echo Please copy from: ..\bin\Debug\GALSXXYY.dll
)
if not exist "Gamc.dll" (
    echo WARNING: Gamc.dll not found in current directory
    echo Please copy from: ..\bin\Debug\Gamc.dll
)
if not exist "FpSplit.dll" (
    echo WARNING: FpSplit.dll not found in current directory
    echo Please copy from: ..\bin\Debug\FpSplit.dll
)

echo.
echo Building and running the application...
dotnet restore
dotnet build
if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful! Starting the web server...
    echo.
    echo Web Interface: http://localhost:5000
    echo API Documentation: http://localhost:5000/swagger
    echo.
    dotnet run
) else (
    echo.
    echo Build failed! Please check the error messages above.
    pause
)
