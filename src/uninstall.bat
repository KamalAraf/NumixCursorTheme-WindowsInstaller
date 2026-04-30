@echo off
:: NumixCursorTheme-WindowsInstaller — Uninstaller (v1.0)
:: https://github.com/KamalAraf/NumixCursorTheme-WindowsInstaller

title Numix Dark Cursor Theme — Uninstaller

echo.
echo  NumixCursorTheme-WindowsInstaller (v1.0)
echo  Uninstalling Numix Dark cursor theme...
echo.

:: check for administrator privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo  [ERROR] Administrator privileges required.
    echo  Please right-click Uninstall.bat and select "Run as administrator".
    echo.
    pause
    exit /b 1
)

:: warn user to switch cursor scheme before proceeding
echo  [WARNING] Before continuing, make sure you have switched to a different
echo  cursor scheme in Mouse Properties. If Numix-Dark is still active when
echo  it is removed, your cursor may appear broken until the next reboot.
echo.
echo  To switch now:  Win + R  -^>  main.cpl  -^>  Pointers tab  -^>  select any other scheme
echo.
echo  Press any key when ready to proceed, or close this window to cancel.
pause >nul
echo.

:: remove registry entries
set "REG_PATH=HKCU\Control Panel\Cursors\Schemes"

reg delete "%REG_PATH%" /v "Numix-Dark" /f >nul 2>&1
reg delete "%REG_PATH%" /v "Numix Dark" /f >nul 2>&1
reg delete "%REG_PATH%" /v "Numix-Cursor-Dark" /f >nul 2>&1

echo  [OK] Registry entries removed.

:: remove cursor files from system directory
set "CURSOR_DIR=%SystemRoot%\Cursors\Numix-Dark"

if exist "%CURSOR_DIR%" (
    rmdir /s /q "%CURSOR_DIR%"
    if exist "%CURSOR_DIR%" (
        echo  [WARNING] Could not fully remove %CURSOR_DIR%
        echo  You may need to delete it manually.
    ) else (
        echo  [OK] Cursor files removed from %CURSOR_DIR%
    )
) else (
    echo  [INFO] Cursor directory not found, skipping file removal.
)

echo.
echo  Uninstallation complete.
echo  The "Numix-Dark" scheme has been removed from Mouse Properties.
echo.
pause

exit /b 0