FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022 AS build
WORKDIR /src
COPY DesktopControlMcp/ ./DesktopControlMcp/
RUN dotnet publish DesktopControlMcp/DesktopControlMcp.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0-windowsservercore-ltsc2022
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["DesktopControlMcp.exe"]
