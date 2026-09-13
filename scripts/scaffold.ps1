# Sofra — kreiranje skeletona projekata (pokrenuti JEDNOM iz root foldera repozitorija, u PowerShellu)
# Preduslovi: .NET 10 SDK, Flutter (stable), Visual Studio 2026 s "Desktop development with C++", Android Studio.
$ErrorActionPreference = "Stop"

Write-Host "== .NET solution ==" -ForegroundColor Cyan
dotnet new sln -n Sofra
dotnet new classlib -n Sofra.Shared -f net10.0 --force
dotnet new webapi  -n Sofra.API    -f net10.0 --use-controllers --force
dotnet new worker  -n Sofra.Worker -f net10.0 --force
dotnet sln Sofra.sln add Sofra.Shared/Sofra.Shared.csproj Sofra.API/Sofra.API.csproj Sofra.Worker/Sofra.Worker.csproj
dotnet add Sofra.API/Sofra.API.csproj    reference Sofra.Shared/Sofra.Shared.csproj
dotnet add Sofra.Worker/Sofra.Worker.csproj reference Sofra.Shared/Sofra.Shared.csproj

Write-Host "== NuGet paketi: API ==" -ForegroundColor Cyan
$api = "Sofra.API/Sofra.API.csproj"
dotnet add $api package Microsoft.EntityFrameworkCore.SqlServer
dotnet add $api package Microsoft.EntityFrameworkCore.Design
dotnet add $api package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add $api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add $api package FluentValidation.AspNetCore
dotnet add $api package Serilog.AspNetCore
dotnet add $api package Serilog.Sinks.File
dotnet add $api package Swashbuckle.AspNetCore
dotnet add $api package RabbitMQ.Client
dotnet add $api package Stripe.net
dotnet add $api package QuestPDF
dotnet add $api package DotNetEnv

Write-Host "== NuGet paketi: Worker ==" -ForegroundColor Cyan
$worker = "Sofra.Worker/Sofra.Worker.csproj"
dotnet add $worker package Microsoft.EntityFrameworkCore.SqlServer
dotnet add $worker package RabbitMQ.Client
dotnet add $worker package MailKit
dotnet add $worker package Serilog.Extensions.Hosting
dotnet add $worker package Serilog.Sinks.Console
dotnet add $worker package Serilog.Sinks.File
dotnet add $worker package DotNetEnv

Write-Host "== EF alat ==" -ForegroundColor Cyan
dotnet tool install --global dotnet-ef 2>$null; dotnet tool update --global dotnet-ef

Write-Host "== Flutter aplikacije ==" -ForegroundColor Cyan
flutter create sofra_desktop --platforms=windows --org ba.sofra --project-name sofra_desktop
flutter create sofra_mobile  --platforms=android --org ba.sofra --project-name sofra_mobile

Push-Location sofra_desktop
flutter pub add provider http flutter_secure_storage signalr_netcore fl_chart intl file_picker printing pdf cached_network_image
Pop-Location
Push-Location sofra_mobile
flutter pub add provider http flutter_secure_storage signalr_netcore flutter_stripe mobile_scanner intl cached_network_image image_picker
Pop-Location

Write-Host ""
Write-Host "Gotovo. Sljedeće: obriši template ostatke (WeatherForecast*), kopiraj .env.example u .env i pokreni 'docker compose up --build'." -ForegroundColor Green
