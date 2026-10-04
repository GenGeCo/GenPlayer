@echo off
rem Compila PhoneVolume.exe con il compilatore C# incluso in Windows (.NET Framework 4.x)
cd /d "%~dp0"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /codepage:65001 /win32icon:app.ico /out:PhoneVolume.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll PhoneVolume.cs
if errorlevel 1 (echo Compilazione fallita & exit /b 1)
echo OK: PhoneVolume.exe creato
