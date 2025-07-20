#!/bin/bash

# Скрипт для запуска тестов в соответствии с TDD принципами
# Поддерживает различные типы тестов и категории

set -e

# Цвета для вывода
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Функции для вывода
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

# Функция помощи
show_help() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --unit                    Run unit tests only"
    echo "  --integration             Run integration tests only"
    echo "  --performance             Run performance tests only"
    echo "  --mutation                Run mutation tests only"
    echo "  --moderation              Run moderation tests only"
    echo "  --users                   Run user management tests only"
    echo "  --handlers                Run handler tests only"
    echo "  --all                     Run all tests (default)"
    echo "  --coverage                Run with coverage report"
    echo "  --verbose                 Verbose output"
    echo "  --parallel                Run tests in parallel"
    echo "  --timeout <seconds>       Set test timeout"
    echo "  --filter <pattern>        Filter tests by pattern"
    echo "  --help                    Show this help"
    echo ""
    echo "Examples:"
    echo "  $0 --unit --moderation    # Run unit tests for moderation"
    echo "  $0 --integration --verbose # Run integration tests with verbose output"
    echo "  $0 --coverage --parallel  # Run all tests with coverage in parallel"
}

# Переменные по умолчанию
TEST_TYPE="all"
VERBOSE=false
PARALLEL=false
COVERAGE=false
TIMEOUT=300
FILTER=""

# Парсинг аргументов
while [[ $# -gt 0 ]]; do
    case $1 in
        --unit)
            TEST_TYPE="unit"
            shift
            ;;
        --integration)
            TEST_TYPE="integration"
            shift
            ;;
        --performance)
            TEST_TYPE="performance"
            shift
            ;;
        --mutation)
            TEST_TYPE="mutation"
            shift
            ;;
        --moderation)
            FILTER="Category=moderation"
            shift
            ;;
        --users)
            FILTER="Category=users"
            shift
            ;;
        --handlers)
            FILTER="Category=handlers"
            shift
            ;;
        --all)
            TEST_TYPE="all"
            shift
            ;;
        --coverage)
            COVERAGE=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        --parallel)
            PARALLEL=true
            shift
            ;;
        --timeout)
            TIMEOUT="$2"
            shift 2
            ;;
        --filter)
            FILTER="$2"
            shift 2
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Проверка наличия .NET
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK not found. Please install .NET 8.0 or later."
    exit 1
fi

# Проверка версии .NET
DOTNET_VERSION=$(dotnet --version)
print_info "Using .NET version: $DOTNET_VERSION"

# Переход в директорию проекта
cd "$(dirname "$0")/.."

# Функция для сборки проекта
build_project() {
    print_info "Building project..."
    dotnet build --configuration Release --no-restore
    if [ $? -eq 0 ]; then
        print_success "Project built successfully"
    else
        print_error "Build failed"
        exit 1
    fi
}

# Функция для запуска unit тестов
run_unit_tests() {
    print_info "Running unit tests..."
    
    local args="--filter Category=unit"
    if [ ! -z "$FILTER" ]; then
        args="$args --filter $FILTER"
    fi
    if [ "$VERBOSE" = true ]; then
        args="$args --verbosity normal"
    fi
    if [ "$PARALLEL" = true ]; then
        args="$args --maxcpucount:0"
    fi
    
    dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj $args
}

# Функция для запуска integration тестов
run_integration_tests() {
    print_info "Running integration tests..."
    
    local args="--filter Category=integration"
    if [ ! -z "$FILTER" ]; then
        args="$args --filter $FILTER"
    fi
    if [ "$VERBOSE" = true ]; then
        args="$args --verbosity normal"
    fi
    if [ "$PARALLEL" = true ]; then
        args="$args --maxcpucount:0"
    fi
    
    dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj $args
}

# Функция для запуска performance тестов
run_performance_tests() {
    print_info "Running performance tests..."
    
    local args="--filter Category=performance"
    if [ ! -z "$FILTER" ]; then
        args="$args --filter $FILTER"
    fi
    if [ "$VERBOSE" = true ]; then
        args="$args --verbosity normal"
    fi
    
    dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj $args
}

# Функция для запуска mutation тестов
run_mutation_tests() {
    print_info "Running mutation tests..."
    
    # Проверка наличия Stryker.NET
    if ! command -v dotnet-stryker &> /dev/null; then
        print_warning "Stryker.NET not found. Installing..."
        dotnet tool install -g dotnet-stryker
    fi
    
    cd ClubDoorman.Test
    
    local args="--test-project-path . --target-framework net8.0"
    if [ "$VERBOSE" = true ]; then
        args="$args --log-level debug"
    fi
    
    dotnet stryker $args
    
    cd ..
}

# Функция для запуска тестов с покрытием
run_tests_with_coverage() {
    print_info "Running tests with coverage..."
    
    # Проверка наличия coverlet
    if ! dotnet tool list -g | grep -q "coverlet.collector"; then
        print_warning "Coverlet not found. Installing..."
        dotnet tool install -g coverlet.collector
    fi
    
    local args="--collect:\"XPlat Code Coverage\""
    if [ ! -z "$FILTER" ]; then
        args="$args --filter $FILTER"
    fi
    if [ "$VERBOSE" = true ]; then
        args="$args --verbosity normal"
    fi
    
    dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj $args
    
    # Генерация отчета
    if command -v reportgenerator &> /dev/null; then
        print_info "Generating coverage report..."
        reportgenerator -reports:ClubDoorman.Test/TestResults/*/coverage.cobertura.xml -targetdir:ClubDoorman.Test/coverage -reporttypes:Html
        print_success "Coverage report generated: ClubDoorman.Test/coverage/index.html"
    else
        print_warning "ReportGenerator not found. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool"
    fi
}

# Функция для запуска всех тестов
run_all_tests() {
    print_info "Running all tests..."
    
    local args=""
    if [ ! -z "$FILTER" ]; then
        args="--filter $FILTER"
    fi
    if [ "$VERBOSE" = true ]; then
        args="$args --verbosity normal"
    fi
    if [ "$PARALLEL" = true ]; then
        args="$args --maxcpucount:0"
    fi
    
    dotnet test ClubDoorman.Test/ClubDoorman.Test.csproj $args
}

# Основная логика
main() {
    print_info "Starting test execution..."
    print_info "Test type: $TEST_TYPE"
    print_info "Verbose: $VERBOSE"
    print_info "Parallel: $PARALLEL"
    print_info "Coverage: $COVERAGE"
    print_info "Timeout: ${TIMEOUT}s"
    if [ ! -z "$FILTER" ]; then
        print_info "Filter: $FILTER"
    fi
    
    # Сборка проекта
    build_project
    
    # Установка таймаута
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
    
    # Запуск тестов в зависимости от типа
    case $TEST_TYPE in
        "unit")
            if [ "$COVERAGE" = true ]; then
                run_tests_with_coverage
            else
                run_unit_tests
            fi
            ;;
        "integration")
            run_integration_tests
            ;;
        "performance")
            run_performance_tests
            ;;
        "mutation")
            run_mutation_tests
            ;;
        "all")
            if [ "$COVERAGE" = true ]; then
                run_tests_with_coverage
            else
                run_all_tests
            fi
            ;;
        *)
            print_error "Unknown test type: $TEST_TYPE"
            exit 1
            ;;
    esac
    
    print_success "Test execution completed!"
}

# Запуск основной функции
main "$@" 