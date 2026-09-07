# Unity Hub Custom - Windows Program Registration Script
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "   Unity Hub Custom - Windows Program Registration" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

$rootDir = Split-Path -Parent $PSScriptRoot
$exePath = Join-Path $rootDir "bin\Release\UnityHubCustom.exe"
$appDir = Join-Path $rootDir "bin\Release"
$icoPath = Join-Path $rootDir "app.ico"

# 1. Check if exe exists
if (-not (Test-Path $exePath)) {
    Write-Host "[경고] 실행 파일($exePath)이 없습니다." -ForegroundColor Yellow
    Write-Host "build.bat을 실행하여 먼저 빌드해 주세요." -ForegroundColor Yellow
    exit 1
}

# 2. Register Start Menu Shortcut
Write-Host "[1/3] 시작 메뉴 바로가기 및 검색 등록 중..." -ForegroundColor Green
$startMenuDir = [Environment]::GetFolderPath("Programs")
$shortcutPath = Join-Path $startMenuDir "Unity Hub Custom.lnk"

$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $appDir
$shortcut.Description = "Unity Hub Custom (레이아웃 초기화 실행기)"
if (Test-Path $icoPath) {
    $shortcut.IconLocation = "$icoPath,0"
} else {
    $shortcut.IconLocation = "$exePath,0"
}
$shortcut.Save()
Write-Host "  -> 시작 메뉴 바로가기 생성 완료: $shortcutPath" -ForegroundColor Gray

# 3. Register to Windows "Installed Apps" (Settings / Control Panel)
Write-Host "[2/3] 윈도우 '설정 > 설치된 앱' 등록 중..." -ForegroundColor Green
$uninstallRegKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\UnityHubCustom"
if (-not (Test-Path $uninstallRegKey)) {
    New-Item -Path $uninstallRegKey -Force | Out-Null
}
$unregisterBat = Join-Path $rootDir "unregister.bat"
Set-ItemProperty -Path $uninstallRegKey -Name "DisplayName" -Value "Unity Hub Custom"
Set-ItemProperty -Path $uninstallRegKey -Name "DisplayVersion" -Value "1.0.0"
Set-ItemProperty -Path $uninstallRegKey -Name "Publisher" -Value "UnityHubCustom"
Set-ItemProperty -Path $uninstallRegKey -Name "DisplayIcon" -Value "$exePath,0"
Set-ItemProperty -Path $uninstallRegKey -Name "InstallLocation" -Value $appDir
Set-ItemProperty -Path $uninstallRegKey -Name "UninstallString" -Value "`"$unregisterBat`""
Write-Host "  -> 레지스트리(HKCU Uninstall) 등록 완료" -ForegroundColor Gray

# 4. Register to App Paths (Win + R execution)
Write-Host "[3/3] '실행'(Win+R) 명령 등록 중..." -ForegroundColor Green
$appPathsKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\App Paths\UnityHubCustom.exe"
if (-not (Test-Path $appPathsKey)) {
    New-Item -Path $appPathsKey -Force | Out-Null
}
Set-ItemProperty -Path $appPathsKey -Name "(Default)" -Value $exePath
Set-ItemProperty -Path $appPathsKey -Name "Path" -Value $appDir
Write-Host "  -> App Paths 등록 완료: Win+R에서 'UnityHubCustom' 입력 가능" -ForegroundColor Gray

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "[성공] 윈도우 프로그램 등록이 완료되었습니다!" -ForegroundColor Green
Write-Host "  * 시작 메뉴 및 Windows 검색(Win키 후 검색) 지원" -ForegroundColor White
Write-Host "  * Windows 설정 [설치된 앱] 목록에 등록 완료" -ForegroundColor White
Write-Host "  * [Win + R] 창에서 'UnityHubCustom' 입력 시 즉시 실행" -ForegroundColor White
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""
