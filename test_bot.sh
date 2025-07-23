#!/bin/bash

# Тестовый скрипт для запуска ClubDoorman с реальным токеном бота
echo "🚀 Запуск ClubDoorman с реальным токеном бота..."

# Переходим в директорию проекта
cd ClubDoorman

# Устанавливаем переменные окружения
export DOORMAN_BOT_API="8038252733:AAEESlLbmAJbvifv9n_ls7UQrt5WFsNWJy8"
export DOORMAN_ADMIN_CHAT="-1001234567890"  # Тестовый ID чата
export DOORMAN_LOG_ADMIN_CHAT="-1001234567890"  # Тестовый ID чата для логов

echo "📋 Переменные окружения:"
echo "DOORMAN_BOT_API: $DOORMAN_BOT_API"
echo "DOORMAN_ADMIN_CHAT: $DOORMAN_ADMIN_CHAT"
echo "DOORMAN_LOG_ADMIN_CHAT: $DOORMAN_LOG_ADMIN_CHAT"

# Запускаем приложение
echo "🔄 Запуск приложения..."
dotnet run 