@echo off
setlocal enabledelayedexpansion

echo ===================================================
echo [1/3] Building ZenQOffice.exe...
echo ===================================================
call build.bat
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] ZenQOffice build failed!
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

copy /y ZenQOffice.exe temp_dist\ >nul
copy /y ZenQOffice.dll temp_dist\ >nul
copy /y ZenQOffice.deps.json temp_dist\ >nul
copy /y ZenQOffice.runtimeconfig.json temp_dist\ >nul
copy /y *.dll temp_dist\ >nul

:: Remove any stray pdb or xml documentation to keep payload clean and protect privacy
del /s /q temp_dist\*.pdb >nul 2>&1
del /s /q temp_dist\*.xml >nul 2>&1

if not exist temp_dist\x64 mkdir temp_dist\x64
if not exist temp_dist\x86 mkdir temp_dist\x86

if exist pdfium.dll copy /y pdfium.dll temp_dist\ >nul
if exist ZenQOffice.ico copy /y ZenQOffice.ico temp_dist\ >nul
if exist GenOffice.ico copy /y GenOffice.ico temp_dist\ >nul
if exist ZENQOFFICE.png copy /y ZENQOFFICE.png temp_dist\ >nul
if exist GENOFFICE.png copy /y GENOFFICE.png temp_dist\ >nul
if exist x64\pdfium.dll copy /y x64\pdfium.dll temp_dist\x64\ >nul
if exist x86\pdfium.dll copy /y x86\pdfium.dll temp_dist\x86\ >nul
if exist data\graphics.json (
    if not exist temp_dist\data mkdir temp_dist\data >nul 2>&1
    copy /y data\graphics.json temp_dist\data\ >nul
)
if exist data\images.json (
    if not exist temp_dist\data mkdir temp_dist\data >nul 2>&1
    copy /y data\images.json temp_dist\data\ >nul
)
if exist icon (
    if not exist temp_dist\icon mkdir temp_dist\icon >nul 2>&1
    copy /y icon\* temp_dist\icon\ >nul
    if exist temp_dist\icon\*backup* del /q temp_dist\icon\*backup* >nul 2>&1
    if exist temp_dist\icon\bannernew.png del /q temp_dist\icon\bannernew.png >nul 2>&1
    if exist temp_dist\icon\*.md del /q temp_dist\icon\*.md >nul 2>&1
)
if exist img (
    if not exist temp_dist\img mkdir temp_dist\img >nul 2>&1
    xcopy /s /e /y img\* temp_dist\img\ >nul
)
if exist fonts (
    if not exist temp_dist\fonts mkdir temp_dist\fonts >nul 2>&1
    copy /y fonts\* temp_dist\fonts\ >nul
)

python -c "import zipfile, os; z = zipfile.ZipFile('payload.zip', 'w', zipfile.ZIP_DEFLATED, compresslevel=9); [z.write(os.path.join(r, f), os.path.relpath(os.path.join(r, f), 'temp_dist')) for r, d, fs in os.walk('temp_dist') for f in fs]; z.close()"
rmdir /s /q temp_dist

echo Payload zip created: payload.zip

echo.
echo ===================================================
echo [3/3] Compiling ZenQOffice-Setup.exe...
echo ===================================================
taskkill /f /im ZenQOffice-Setup.exe >nul 2>&1
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize /main:ZenQOffice.Setup.Program /win32icon:ZenQOffice.ico /win32manifest:src\setup\app.manifest /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:Microsoft.CSharp.dll /resource:payload.zip,payload.zip /resource:ZenQOffice.ico,app.ico /out:ZenQOffice-Setup.exe src\setup\*.cs src\AssemblyInfo.cs

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
