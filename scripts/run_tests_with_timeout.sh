#!/bin/bash

# Скрипт для запуска тестов с таймаутом и эффективным дебагом
# Использование: ./run_tests_with_timeout.sh [фильтр] [таймаут_в_секундах]

FILTER=${1:-"ModerationServiceTests"}
TIMEOUT=${2:-5}

echo "🔍 Эффективный дебаг тестов"
echo "Фильтр: $FILTER"
echo "Таймаут: ${TIMEOUT}с"
echo "================================"

# Прерываем все процессы dotnet test
echo "🛑 Прерываем старые процессы..."
pkill -f "dotnet test" 2>/dev/null || true
sleep 1

# Запускаем тесты с таймаутом и подробной трассировкой
echo "🚀 Запускаем тесты..."
export DOORMAN_BOT_API="test_api_key_for_integration_tests"
export DOORMAN_ADMIN_CHAT="123456789"
export DOORMAN_BOT_TOKEN="1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"
timeout ${TIMEOUT}s dotnet test --filter "$FILTER" --verbosity normal --logger "console;verbosity=detailed" 2>&1

EXIT_CODE=$?

echo ""
echo "================================"
echo "📊 Результат выполнения:"

if [ $EXIT_CODE -eq 124 ]; then
    echo "❌ ТЕСТЫ ПРЕВЫСИЛИ ТАЙМАУТ ${TIMEOUT}с"
    echo "🔍 Возможные причины зависания:"
    echo "   - SpamHamClassifier.Train() - обучение ML модели"
    echo "   - BadMessageManager.MarkAsBad() - операции с файлами"
    echo "   - SemaphoreHelper.AwaitAsync() - блокировки"
    echo "   - File.ReadAllLines() - чтение больших файлов"
    echo ""
    echo "💡 Рекомендации:"
    echo "   - Увеличьте таймаут: ./run_tests_with_timeout.sh $FILTER 30"
    echo "   - Запустите отдельные тесты для изоляции проблемы"
    echo "   - Проверьте наличие файлов data/spam-ham.txt, data/exclude-tokens.txt"
elif [ $EXIT_CODE -eq 0 ]; then
    echo "✅ Все тесты прошли успешно"
else
    echo "❌ Тесты завершились с ошибкой (код: $EXIT_CODE)"
fi

echo ""
echo "🔍 Для детального анализа запустите:"
echo "   dotnet test --filter \"$FILTER\" --verbosity normal --logger \"console;verbosity=detailed\""

exit $EXIT_CODE 