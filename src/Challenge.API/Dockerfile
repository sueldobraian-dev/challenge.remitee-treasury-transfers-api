FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Challenge.API/Challenge.API.csproj", "src/Challenge.API/"]
COPY ["src/Challenge.InfrastructureBootstrap/Challenge.InfrastructureBootstrap.csproj", "src/Challenge.Infrastructure.Bootstrap/"]
COPY ["src/Challenge.Infrastructure/Challenge.Infrastructure.csproj", "src/Challenge.Infrastructure/"]
COPY ["src/Challenge.Application/Challenge.Application.csproj", "src/Challenge.Application/"]
COPY ["src/Challenge.Domain/Challenge.Domain.csproj", "src/Challenge.Domain/"]
RUN dotnet restore "src/Challenge.API/Challenge.API.csproj"
COPY . .
WORKDIR "/src/src/Challenge.API"
RUN dotnet build "Challenge.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Challenge.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Challenge.API.dll"]
