# Sofra — build za predaju (APK + Windows) i ZIP arhiva prema uputama (poglavlje 9.2)
# Pokrenuti iz root foldera repozitorija. Rezultat: fit-build-gggg-mm-dd.zip u root-u.
$ErrorActionPreference = "Stop"
$date = Get-Date -Format "yyyy-MM-dd"
$zip  = "fit-build-$date.zip"
$stage = "release-stage"

Write-Host "== Mobile (Android, API = http://10.0.2.2:5000) ==" -ForegroundColor Cyan
Push-Location sofra_mobile
flutter clean
flutter build apk --release --dart-define=API_BASE_URL=http://10.0.2.2:5000
Pop-Location

Write-Host "== Desktop (Windows, API = http://localhost:5000) ==" -ForegroundColor Cyan
Push-Location sofra_desktop
flutter clean
flutter build windows --release --dart-define=API_BASE_URL=http://localhost:5000
Pop-Location

Write-Host "== ZIP ==" -ForegroundColor Cyan
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path "$stage/sofra_mobile/build/app/outputs/flutter-apk" | Out-Null
New-Item -ItemType Directory -Path "$stage/sofra_desktop/build/windows/x64/runner" | Out-Null
Copy-Item "sofra_mobile/build/app/outputs/flutter-apk/app-release.apk" "$stage/sofra_mobile/build/app/outputs/flutter-apk/"
Copy-Item "sofra_desktop/build/windows/x64/runner/Release" "$stage/sofra_desktop/build/windows/x64/runner/Release" -Recurse
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path "$stage/*" -DestinationPath $zip
Remove-Item $stage -Recurse -Force
Write-Host "Kreirano: $zip  (NE sadrži .env). Provjeri APK u AVD-u i .exe na Windowsu prije release-a." -ForegroundColor Green
