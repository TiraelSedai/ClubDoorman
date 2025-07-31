#!/bin/bash

# Запуск тестов без demo категории
echo "🧪 Запуск тестов без demo категории..."
echo "Исключаем: TestKitDemoTests, TestKitAutoFixtureDemoTests, TestKitStage3DemoTests, TestKitTelegramDemoTests"

dotnet test --filter "Category!=demo" --logger "console;verbosity=normal" --no-restore

echo ""
echo "✅ Тесты без demo завершены"
echo "💡 Для запуска ТОЛЬКО demo тестов: dotnet test --filter \"Category=demo\""
echo "💡 Для запуска ВСЕХ тестов: dotnet test"