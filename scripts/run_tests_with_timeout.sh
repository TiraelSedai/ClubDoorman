#!/bin/bash

# Скрипт для запуска тестов с новой системой таймаутов
# Теперь таймауты настраиваются в test-timeouts.json

set -e

echo "🔧 Setting up test environment..."

# Экспортируем переменные окружения для тестов
export DOORMAN_BOT_API="https://api.telegram.org"
export DOORMAN_ADMIN_CHAT="123456789"
export DOORMAN_BOT_TOKEN="1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"
export DOORMAN_OPENROUTER_API="test-api-key-for-tests-only"

echo "   DOORMAN_BOT_API: $DOORMAN_BOT_API"
echo "   DOORMAN_ADMIN_CHAT: $DOORMAN_ADMIN_CHAT"
echo "   DOORMAN_BOT_TOKEN: $DOORMAN_BOT_TOKEN"
echo "   DOORMAN_OPENROUTER_API: $DOORMAN_OPENROUTER_API"

# Проверяем, передан ли фильтр тестов
if [ $# -eq 0 ]; then
    echo "🚀 Running all tests with configurable timeouts..."
    dotnet test --verbosity normal --logger "console;verbosity=detailed"
else
    echo "🚀 Running tests with filter: $1"
    dotnet test --filter "$1" --verbosity normal --logger "console;verbosity=detailed"
fi

echo "✅ Tests completed!" 