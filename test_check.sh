#!/bin/bash

echo "🔍 Проверка компиляции тестов..."
cd ClubDoorman.Test
dotnet build

echo "🧪 Запуск тестов..."
dotnet test 