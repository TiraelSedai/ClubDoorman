#!/bin/bash

# Скрипт для запуска тестов ClubDoorman
# Использование:
#   ./scripts/run-tests.sh fast     # Только быстрые тесты
#   ./scripts/run-tests.sh slow     # Только медленные тесты  
#   ./scripts/run-tests.sh all      # Все тесты
#   ./scripts/run-tests.sh          # По умолчанию - быстрые тесты

set -e

# Цвета для вывода
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Функция для вывода с цветом
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Определяем тип тестов
TEST_TYPE=${1:-fast}

case $TEST_TYPE in
    "fast")
        print_info "Запуск быстрых тестов (исключая slow)..."
        FILTER="TestCategory!=slow"
        ;;
    "slow")
        print_info "Запуск медленных тестов (только slow)..."
        FILTER="TestCategory=slow"
        ;;
    "all")
        print_info "Запуск всех тестов..."
        FILTER=""
        ;;
    *)
        print_error "Неизвестный тип тестов: $TEST_TYPE"
        echo "Доступные опции: fast, slow, all"
        exit 1
        ;;
esac

# Проверяем, что мы в корневой директории проекта
if [ ! -f "ClubDoorman.sln" ]; then
    print_error "Скрипт должен запускаться из корневой директории проекта"
    exit 1
fi

# Восстанавливаем зависимости
print_info "Восстанавливаем зависимости..."
dotnet restore

# Собираем проект
print_info "Собираем проект..."
dotnet build --no-restore

# Запускаем тесты
print_info "Запускаем тесты..."
if [ -z "$FILTER" ]; then
    dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --logger "console;verbosity=minimal"
else
    dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj --filter "$FILTER" --logger "console;verbosity=minimal"
fi

print_success "Тесты завершены!" 