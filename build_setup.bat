@echo off
setlocal enabledelayedexpansion

echo ===================================================
echo [1/3] Building ZenQOffice.exe...
echo ===================================================
call build.bat
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] ZenQOffice.exe build failed!
    exit /b 1
)

echo.
echo ===================================================
echo [2/3] Packing application payload into zip...
echo ===================================================
if exist payload.zip del payload.zip
if exist temp_dist rmdir /s /q temp_dist
mkdir temp_dist
mkdir temp_dist\x64
mkdir temp_dist\x86

copy /y bin\Release\net10.0-windows\* temp_dist\ >nul
if exist pdfium.dll copy /y pdfium.dll temp_dist\ >nul
if exist ZenQOffice.ico copy /y ZenQOffice.ico temp_dist\ >nul
if exist GenOffice.ico copy /y GenOffice.ico temp_dist\ >nul
if exist ZENQOFFICE.png copy /y ZENQOFFICE.png temp_dist\ >nul
if exist GENOFFICE.png copy /y GENOFFICE.png temp_dist\ >nul
if exist x64\pdfium.dll copy /y x64\pdfium.dll temp_dist\x64\ >nul
if exist x86\pdfium.dll copy /y x86\pdfium.dll temp_dist\x86\ >nul
if exist data\graphics.json (
    mkdir temp_dist\data >nul 2>&1
    copy /y data\graphics.json temp_dist\data\ >nul
)
if exist icon (
    mkdir temp_dist\icon >nul 2>&1
    copy /y icon\* temp_dist\icon\ >nul
)

powershell -Command "Compress-Archive -Path 'temp_dist\*' -DestinationPath 'payload.zip' -Force"
rmdir /s /q temp_dist

echo Payload zip created: payload.zip

echo.
echo ===================================================
echo [3/3] Compiling ZenQOffice-Setup.exe...
echo ===================================================
taskkill /f /im ZenQOffice-Setup.exe >nul 2>&1
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize /main:ZenQOffice.Setup.Program /win32icon:ZenQOffice.ico /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:Microsoft.CSharp.dll /resource:payload.zip,payload.zip /resource:ZenQOffice.ico,app.ico /out:ZenQOffice-Setup.exe src\setup\*.cs src\AssemblyInfo.cs

if %ERRORLEVEL% EQU 0 (
    if exist payload.zip del payload.zip
    echo.
    echo ===================================================
    echo [SUCCESS] ZenQOffice-Setup.exe built successfully!
    echo ===================================================
    dir ZenQOffice-Setup.exe
) else (
    echo.
    echo [ERROR] Setup build failed!
    exit /b 1
)
