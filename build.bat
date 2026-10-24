@echo off
rem ---------------------------------------------------------------------------
rem GitForce - compilation Release, publication en un seul executable et
rem creation de l'installeur avec Inno Setup 6.
rem
rem   build.bat          compile, publie et cree l'installeur
rem   build.bat run      ... puis lance l'installeur cree
rem ---------------------------------------------------------------------------
setlocal
cd /d "%~dp0"

set PUBLISH_DIR=src\bin\Release\net10.0-windows\publish\win-x64

rem --- GitForce ne doit pas tourner depuis le dossier de publication (fichier verrouille)
tasklist /fi "imagename eq GitForce.exe" 2>nul | find /i "GitForce.exe" >nul
if not errorlevel 1 (
    echo GitForce est en cours d'execution : fermez-le avant de publier.
    goto :error
)

rem --- Inno Setup 7 ou 6 : pour tous les utilisateurs ou pour l'utilisateur courant
set ISCC=
for %%v in (7 6) do (
    if not defined ISCC if exist "%ProgramFiles%\Inno Setup %%v\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup %%v\ISCC.exe"
    if not defined ISCC if exist "%ProgramFiles(x86)%\Inno Setup %%v\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup %%v\ISCC.exe"
    if not defined ISCC if exist "%LOCALAPPDATA%\Programs\Inno Setup %%v\ISCC.exe" set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup %%v\ISCC.exe"
)
if not defined ISCC (
    echo Inno Setup introuvable ^(ISCC.exe^). Installez Inno Setup 6 ou 7.
    goto :error
)
echo Inno Setup : %ISCC%

echo.
echo === 1/3 Compilation Release
dotnet build GitForce.slnx -c Release --nologo
if errorlevel 1 goto :error

echo.
echo === 2/3 Publication en un seul executable
dotnet publish src\GitForce.csproj -p:PublishProfile=FolderProfile --nologo
if errorlevel 1 goto :error
if not exist "%PUBLISH_DIR%\GitForce.exe" (
    echo GitForce.exe absent de %PUBLISH_DIR%.
    goto :error
)

echo.
echo === 3/3 Creation de l'installeur
"%ISCC%" /Q installer\GitForce.iss
if errorlevel 1 goto :error

rem --- Installeur le plus recent
set SETUP=
for /f "delims=" %%f in ('dir /b /o-d "installer\Output\GitForce-Setup-*.exe" 2^>nul') do if not defined SETUP set "SETUP=installer\Output\%%f"

echo.
echo === Termine
echo   Executable : %PUBLISH_DIR%\GitForce.exe
echo   Installeur : %SETUP%

if /i "%~1"=="run" if defined SETUP (
    echo.
    echo Lancement de l'installeur...
    start "" "%SETUP%"
)
endlocal
exit /b 0

:error
echo.
echo *** Echec de la construction ***
endlocal
exit /b 1
