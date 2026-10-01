@echo off
setlocal
taskkill /f /im ZenQOffice.exe >nul 2>&1
taskkill /f /im GenOffice.exe >nul 2>&1

echo [BUILD] Compiling ZenQ Office on Modern .NET 10 (Release)...
dotnet build ZenQOffice.csproj -c Release -v:m

if %ERRORLEVEL% EQU 0 (
    echo [DEPLOY] Copying binaries to root directory...
    copy /y bin\Release\net10.0-windows\* . >nul 2>&1
    echo [SUCCESS] ZenQOffice.exe built successfully with .NET 10!
    dir ZenQOffice.exe
) else (
    echo [ERROR] Build failed!
    exit /b 1
)
