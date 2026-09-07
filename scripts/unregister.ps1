# Unity Hub Custom - Windows Program Unregistration Script
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "   Unity Hub Custom - Windows Program Unregistration" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Remove Start Menu Shortcut
Write-Host "[1/3] 시작 메뉴 바로가기 제거 중..." -ForegroundColor Yellow
$startMenuDir = [Environment]::GetFolderPath("Programs")
$shortcutPath = Join-Path $startMenuDir "Unity Hub Custom.lnk"
if (Test-Path $shortcutPath) {
    Remove-Item -Force $shortcutPath
    Write-Host "  -> 시작 메뉴 바로가기 삭제 완료" -ForegroundColor Gray
} else {
    Write-Host "  -> 시작 메뉴 바로가기 없음 (건너뜀)" -ForegroundColor Gray
}

# 2. Remove Registry Uninstall Key
Write-Host "[2/3] 윈도우 '설정 > 설치된 앱' 레지스트리 제거 중..." -ForegroundColor Yellow
$uninstallRegKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\UnityHubCustom"
if (Test-Path $uninstallRegKey) {
    Remove-Item -Path $uninstallRegKey -Recurse -Force
    Write-Host "  -> 레지스트리(HKCU Uninstall) 삭제 완료" -ForegroundColor Gray
} else {
    Write-Host "  -> 등록 정보 없음 (건너뜀)" -ForegroundColor Gray
}

# 3. Remove App Paths Key
Write-Host "[3/3] '실행'(Win+R) App Paths 제거 중..." -ForegroundColor Yellow
$appPathsKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\App Paths\UnityHubCustom.exe"
if (Test-Path $appPathsKey) {
    Remove-Item -Path $appPathsKey -Recurse -Force
    Write-Host "  -> App Paths 레지스트리 삭제 완료" -ForegroundColor Gray
} else {
    Write-Host "  -> App Paths 없음 (건너뜀)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "[성공] 윈도우 프로그램 등록이 정상적으로 해제되었습니다." -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""
