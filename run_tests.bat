@echo off
setlocal

echo [TEST] Executing ZenQ Calc 4-Tier Test Suite on .NET 10...
dotnet run --project tests\ZenQCalcTests.csproj -c Release %*
set TEST_EXIT=%ERRORLEVEL%

if %TEST_EXIT% NEQ 0 (
    echo [ERROR] Tests failed with exit code %TEST_EXIT%
    exit /b %TEST_EXIT%
)

exit /b 0
