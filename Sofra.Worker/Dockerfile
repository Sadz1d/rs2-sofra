# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Sofra.slnx ./
COPY Sofra.Shared/Sofra.Shared.csproj Sofra.Shared/
COPY Sofra.Worker/Sofra.Worker.csproj Sofra.Worker/
RUN dotnet restore Sofra.Worker/Sofra.Worker.csproj
COPY Sofra.Shared/ Sofra.Shared/
COPY Sofra.Worker/ Sofra.Worker/
RUN dotnet publish Sofra.Worker/Sofra.Worker.csproj -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Sofra.Worker.dll"]
