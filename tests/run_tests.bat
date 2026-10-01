@echo off
pushd "%~dp0\.."
call run_tests.bat %*
popd
