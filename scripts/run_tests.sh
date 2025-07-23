#!/bin/bash

# Test runner script for ClubDoorman
# Sets up environment variables and runs tests

set -e

# Test environment variables (safe test values)
export DOORMAN_BOT_API="https://api.telegram.org"
export DOORMAN_ADMIN_CHAT="123456789"
export DOORMAN_BOT_TOKEN="1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"
export DOORMAN_OPENROUTER_API="test-api-key-for-tests-only"

# Optional test-specific settings
export DOORMAN_AI_ENABLED_CHATS=""
export DOORMAN_DISABLE_MEDIA_FILTERING="false"
export DOORMAN_GROUP_APPROVAL_MODE="false"
export DOORMAN_USE_NEW_APPROVAL_SYSTEM="false"

echo "🔧 Setting up test environment..."
echo "   DOORMAN_BOT_API: $DOORMAN_BOT_API"
echo "   DOORMAN_ADMIN_CHAT: $DOORMAN_ADMIN_CHAT"
echo "   DOORMAN_BOT_TOKEN: $DOORMAN_BOT_TOKEN"
echo "   DOORMAN_OPENROUTER_API: $DOORMAN_OPENROUTER_API"
echo ""

# Check if test filter is provided
if [ $# -eq 0 ]; then
    echo "🚀 Running all tests..."
    dotnet test --verbosity normal
else
    echo "🚀 Running tests with filter: $1"
    dotnet test --filter "$1" --verbosity normal
fi

echo ""
echo "✅ Tests completed!" 