@echo off

echo ==============================
echo Packing UltimateAuth packages
echo ==============================

REM eski paketleri temizle
if exist nupkgs (
    echo Cleaning old packages...
    rmdir /s /q nupkgs
)

REM pack işlemi
echo Running dotnet pack...
dotnet pack -c Release -o ./nupkgs

REM sonuç kontrol
if %errorlevel% neq 0 (
    echo ❌ Pack failed!
    pause
    exit /b %errorlevel%
)

echo ✅ Pack completed successfully!
echo Packages are in /nupkgs

pause