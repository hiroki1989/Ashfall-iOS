@echo off
set "TEMP=D:\Unity\ProcessTemp\Ashfall-iOS"
set "TMP=D:\Unity\ProcessTemp\Ashfall-iOS"
set "UPM_CACHE_ROOT=D:\Unity\SharedCaches\PackageCache"
if not exist "%TEMP%" mkdir "%TEMP%"
start "" "D:\Unity\Unity Hub\6000.6.0f1\Editor\Unity.exe" -projectPath "D:\Unity\Ashfall-iOS" -logFile "D:\Unity\Ashfall-iOS\Logs\Editor.log"