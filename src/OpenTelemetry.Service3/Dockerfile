FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["OpenTelemetry.Service3/OpenTelemetry.Service3.csproj", "OpenTelemetry.Service3/"]
RUN dotnet restore "OpenTelemetry.Service3/OpenTelemetry.Service3.csproj"
COPY . .
WORKDIR "/src/OpenTelemetry.Service3"
RUN dotnet build "OpenTelemetry.Service3.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OpenTelemetry.Service3.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenTelemetry.Service3.dll"]
