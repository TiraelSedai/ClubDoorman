FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ClubDoorman.csproj", "."]
RUN dotnet restore "./ClubDoorman.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "./ClubDoorman.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ClubDoorman.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
RUN mv /app/publish/data /app/publish/data_init

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir /app/data && chown -R app:app /app/data
ENTRYPOINT ["dotnet", "ClubDoorman.dll"]