FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы проекта
COPY ClubDoorman.sln ./
COPY ClubDoorman/*.csproj ./ClubDoorman/
COPY ClubDoorman.Test/*.csproj ./ClubDoorman.Test/

# Восстанавливаем зависимости
RUN dotnet restore

# Копируем исходный код
COPY . .

# Собираем приложение
RUN dotnet build -c Release -o /app/build

# Публикуем приложение
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .

# Создаем директорию для данных
RUN mkdir -p /app/data

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "ClubDoorman.dll"] 