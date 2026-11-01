param(
    [long]$MinimumFreeKb = 1250000,
    [int]$JobWorkers = 2,
    [string]$UnityExe = 'D:\Unity\Unity Hub\6000.6.0f1\Editor\Unity.exe',
    [string]$ProjectPath = 'D:\Unity\Ashfall-iOS',
    [string]$OutputFolder = 'D:\Unity\BuildTemp\low-memory-build'
)

$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $UnityExe -PathType Leaf)) { throw 'Unity executable not found.' }
if (!(Test-Path -LiteralPath $ProjectPath -PathType Container)) { throw 'Unity project not found.' }
if ($MinimumFreeKb -lt 1000000 -or $JobWorkers -lt 1) { throw 'Unsafe memory floor or worker count.' }
$editor = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" |
    Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf($ProjectPath,[StringComparison]::OrdinalIgnoreCase) -ge 0 }
if ($editor) { throw 'This project already has a Unity process; save and close it before building.' }
$os = Get-CimInstance Win32_OperatingSystem
if ($os.FreePhysicalMemory -lt ($MinimumFreeKb + 2000000)) { throw 'Not enough free RAM to start this build safely.' }
New-Item -ItemType Directory -Force -Path $OutputFolder | Out-Null
$log = Join-Path $OutputFolder 'unity.log'
$telemetry = Join-Path $OutputFolder 'memory.csv'
'utc,available_physical_kb,available_virtual_kb,unity_working_set_bytes' | Set-Content -LiteralPath $telemetry -Encoding utf8
$arguments = @('-batchmode','-nographics','-job-worker-count',"$JobWorkers",'-projectPath',"`"$ProjectPath`"",'-executeMethod','ActionSkillBuildReview.Run','-logFile',"`"$log`"",'-quit')
$process = Start-Process -FilePath $UnityExe -ArgumentList $arguments -WindowStyle Hidden -PassThru
do {
    Start-Sleep -Seconds 1
    $process.Refresh()
    if ($process.HasExited) { break }
    $os = Get-CimInstance Win32_OperatingSystem
    $line = '{0},{1},{2},{3}' -f [DateTime]::UtcNow.ToString('o'),$os.FreePhysicalMemory,$os.FreeVirtualMemory,$process.WorkingSet64
    Add-Content -LiteralPath $telemetry -Value $line -Encoding utf8
    if ($os.FreePhysicalMemory -lt $MinimumFreeKb) {
        Stop-Process -Id $process.Id -Force
        Write-Error "Stopped the build process at $($os.FreePhysicalMemory) KB free; the build is incomplete. See $telemetry."
        exit 3
    }
} while ($true)
if ($process.ExitCode -ne 0) { Write-Error "Unity failed ($($process.ExitCode)). See $log."; exit $process.ExitCode }
if (!(Select-String -LiteralPath $log -Pattern 'ACTION_SKILL_BUILD Succeeded' -Quiet)) { throw "Unity exited without a successful build marker; see $log." }
Write-Output "Build succeeded. Log: $log; memory: $telemetry"
