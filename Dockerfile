# Multi-stage Dockerfile for RaceDay ASP.NET Core MVC Web Application
# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj files and restore dependencies
COPY ["src/RaceDay.Web/RaceDay.Web.csproj", "src/RaceDay.Web/"]
COPY ["src/RaceDay.API/RaceDay.API.csproj", "src/RaceDay.API/"]
RUN dotnet restore "src/RaceDay.Web/RaceDay.Web.csproj"

# Copy full source and compile
COPY . .
WORKDIR "/app/src/RaceDay.Web"
RUN dotnet publish "RaceDay.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
