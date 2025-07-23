#!/bin/bash

# Скрипт для запуска E2E тестов ClubDoorman
# Используется для проверки полных сценариев с реальными API

set -e

echo "🌐 Запуск E2E тестов ClubDoorman..."
echo ""

# Проверяем наличие .env файла
if [ ! -f "ClubDoorman/.env" ]; then
    echo "❌ Файл ClubDoorman/.env не найден!"
    echo "   Убедитесь, что у вас настроены API ключи"
    exit 1
fi

echo "✅ Файл .env найден"
echo ""

# Проверяем наличие API ключей
echo "🔑 Проверка API ключей..."
source ClubDoorman/.env

if [ -z "$DOORMAN_OPENROUTER_API" ] || [ -z "$DOORMAN_BOT_API" ]; then
    echo "❌ API ключи не настроены!"
    echo "   Нужны: DOORMAN_OPENROUTER_API и DOORMAN_BOT_API"
    exit 1
fi

echo "✅ API ключи настроены"
echo ""

# Запускаем E2E тесты
echo "🧪 Запуск E2E тестов..."
dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --filter "Category=e2e" --verbosity normal

echo ""
echo "✅ E2E тесты завершены!"
echo ""
echo "💡 Для запуска всех тестов (включая unit тесты) используйте:"
echo "   ./scripts/run_tests.sh"
echo ""
echo "💡 Для запуска только интеграционных тестов используйте:"
echo "   ./scripts/run_integration_tests.sh" 