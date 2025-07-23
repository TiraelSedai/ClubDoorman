#!/bin/bash

# Скрипт для запуска интеграционных тестов с реальными API
# Используется для проверки работы с реальными сервисами (OpenRouter, Telegram)

set -e

echo "🔍 Запуск интеграционных тестов с реальными API..."
echo ""

# Проверяем наличие .env файла
if [ ! -f "ClubDoorman/.env" ]; then
    echo "❌ Файл ClubDoorman/.env не найден!"
    echo "   Убедитесь, что у вас настроены API ключи"
    exit 1
fi

echo "✅ Файл .env найден"
echo ""

# Запускаем тесты с реальными API
echo "🧪 Запуск тестов с реальными API..."

# Тест загрузки переменных окружения
echo "📋 Тест загрузки переменных окружения..."
dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --filter "Category=environment" --verbosity minimal

# Тест AI анализа фото
echo "🖼️  Тест AI анализа фото..."
dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --filter "Category=ai-photo" --verbosity minimal

# E2E тесты
echo "🌐 E2E тесты полных сценариев..."
dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --filter "Category=e2e" --verbosity minimal

echo ""
echo "✅ Все интеграционные тесты с реальными API выполнены успешно!"
echo ""
echo "💡 Для запуска всех тестов (включая unit тесты) используйте:"
echo "   ./scripts/run_tests.sh"
echo ""
echo "💡 Для запуска только unit тестов используйте:"
echo "   dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --filter \"Category!=integration\"" 