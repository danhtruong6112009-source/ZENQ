@echo off
setlocal
cd /d "%~dp0"
echo [BUILD] Compiling ZenQ Office to the project root (Release)...
call dotnet build ZenQOffice.csproj -c Release -v:m -p:DebugType=none -p:DebugSymbols=false
if errorlevel 1 (
    echo [ERROR] Build failed. If ZenQOffice is open, close it and run this again.
    exit /b 1
)
if exist ZenQOffice.pdb del ZenQOffice.pdb >nul 2>&1
echo [SUCCESS] Built: %~dp0ZenQOffice.exe
exit /b 0
