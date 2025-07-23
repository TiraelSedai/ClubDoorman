# ClubDoorman Tests

This directory contains comprehensive tests for the ClubDoorman bot.

## Test Structure

- **ModerationServiceTests.cs** - Integration tests for ModerationService
- **ModerationServiceSimpleTests.cs** - Unit tests for ModerationService
- **CriticalFunctionalityTests.cs** - Critical functionality tests
- **SimpleFiltersTests.cs** - Tests for SimpleFilters service
- **TextProcessorNullTests.cs** - Null safety tests for TextProcessor

## Running Tests

### Quick Start
```bash
# Run all tests with environment setup
./scripts/run_tests.sh

# Run specific test class
./scripts/run_tests.sh ModerationServiceTests

# Run with timeout (for integration tests)
./scripts/run_tests_with_timeout.sh ModerationServiceTests 30

# Direct dotnet test (requires environment variables)
dotnet test --filter "ModerationServiceTests"
```

### Test Environment

Tests use safe test values - no real API keys or sensitive data:

- **DOORMAN_BOT_API**: `https://api.telegram.org`
- **DOORMAN_ADMIN_CHAT**: `123456789`
- **DOORMAN_BOT_TOKEN**: `1234567890:ABCdefGHIjklMNOpqrsTUVwxyz`
- **DOORMAN_OPENROUTER_API**: `test-api-key-for-tests-only`

### Test Profiles

- **Integration Tests**: Full integration with ML and TelegramBotClient
- **Unit Tests**: Isolated unit tests with mocked dependencies

## Test Coverage

Current coverage focuses on:
- ✅ Null safety and error handling
- ✅ Business logic validation
- ✅ Integration scenarios
- ✅ Critical functionality

## Adding New Tests

1. Follow existing naming conventions
2. Use `TestData.cs` for common test data
3. Add appropriate timeouts for integration tests
4. Document any new test environment requirements

## Infrastructure

- **run_tests.sh**: Main test runner with environment setup
- **run_tests_with_timeout.sh**: Test runner with timeout protection
- **test.env**: Test environment variables
- **launchSettings.json**: VS Code/IDE test profiles 