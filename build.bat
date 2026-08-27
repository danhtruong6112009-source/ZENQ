@echo off
taskkill /f /im GenOffice.exe >nul 2>&1
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize /win32icon:GenOffice.ico /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:Microsoft.CSharp.dll /r:PdfiumViewer.dll /r:PdfSharp.dll /out:GenOffice.exe /recurse:src\*.cs
if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] GenOffice.exe built successfully!
    dir GenOffice.exe
) else (
    echo [ERROR] Build failed!
    exit /b 1
)
