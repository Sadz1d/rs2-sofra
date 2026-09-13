# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Sofra.sln ./
COPY Sofra.Shared/Sofra.Shared.csproj Sofra.Shared/
COPY Sofra.API/Sofra.API.csproj Sofra.API/
RUN dotnet restore Sofra.API/Sofra.API.csproj
COPY Sofra.Shared/ Sofra.Shared/
COPY Sofra.API/ Sofra.API/
RUN dotnet publish Sofra.API/Sofra.API.csproj -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/images/uploads
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sofra.API.dll"]
