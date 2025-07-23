#!/usr/bin/env python3
"""
Тестовый скрипт для проверки улучшенного DX Tool
"""

import sys
from pathlib import Path

# Добавляем текущую директорию в путь для импорта
sys.path.append('.')

# Импортируем напрямую
import csharp_analyzer
import factory_generator
import complexity_analyzer


def test_improved_generator():
    """Тестирует улучшенный генератор с анализом сложности"""
    print("🔧 Тестирование улучшенного DX Tool...")
    
    # Инициализируем анализатор
    analyzer = csharp_analyzer.CSharpAnalyzer("../../ClubDoorman")
    
    # Находим MessageHandler
    services = analyzer.find_service_classes()
    message_handler = None
    
    for service in services:
        if service.name == "MessageHandler":
            message_handler = service
            break
    
    if not message_handler:
        print("❌ MessageHandler не найден")
        return
    
    print(f"✅ Найден {message_handler.name}")
    
    # Анализируем сложность
    complexity_report = analyzer.analyze_class_complexity(message_handler)
    print(f"📊 Сложность: {complexity_report.complexity_score}/10 ({complexity_report.complexity_level.value})")
    
    # Ищем маркеры
    file_path = Path("../../ClubDoorman") / message_handler.file_path
    markers = analyzer.find_test_markers(file_path)
    test_markers = markers.get(message_handler.name, [])
    
    print(f"🏷️ Найденные маркеры: {test_markers}")
    print(f"💡 Предлагаемые маркеры: {[marker.value for marker in complexity_report.suggested_markers]}")
    
    # Инициализируем генератор
    generator = factory_generator.TestFactoryGenerator(Path("../../ClubDoorman/ClubDoorman.Test"))
    
    # Устанавливаем анализ сложности
    generator.set_complexity_analysis(complexity_report, test_markers)
    
    # Генерируем TestFactory
    factory_code = generator.generate_test_factory(message_handler)
    
    print(f"\n📝 Сгенерированный TestFactory:")
    print("=" * 80)
    print(factory_code)
    print("=" * 80)
    
    # Анализируем различия с ручным
    print(f"\n🔍 Анализ улучшений:")
    
    # Проверяем наличие FakeTelegramClient
    if "FakeTelegramClient" in factory_code:
        print("✅ Есть FakeTelegramClient (утилита)")
    else:
        print("❌ Нет FakeTelegramClient")
    
    # Проверяем моки конкретных классов
    concrete_mocks = []
    for param in message_handler.constructor_params:
        if param.is_concrete and f"Mock<{param.type}>" in factory_code:
            concrete_mocks.append(param.type)
    
    if concrete_mocks:
        print(f"✅ Есть моки конкретных классов: {', '.join(concrete_mocks)}")
    else:
        print("❌ Нет моков конкретных классов")
    
    # Проверяем кастомную инициализацию
    if "public MessageHandlerTestFactory()" in factory_code:
        print("✅ Есть кастомный конструктор")
    else:
        print("❌ Нет кастомного конструктора")
    
    # Проверяем дополнительные методы
    if "CreateMessageHandlerWithFake" in factory_code:
        print("✅ Есть дополнительные методы")
    else:
        print("❌ Нет дополнительных методов")
    
    print(f"\n📊 Итоговая оценка:")
    print(f"  - Сложность: {complexity_report.complexity_score}/10")
    print(f"  - Конкретные классы: {len(complexity_report.concrete_types)}")
    print(f"  - Интерфейсы: {len(complexity_report.interface_types)}")
    print(f"  - Маркеры: {len(test_markers)}")
    
    if complexity_report.complexity_score >= 7:
        print("🔴 ВЫСОКАЯ сложность - нужны маркеры")
    elif complexity_report.complexity_score >= 3:
        print("🟡 СРЕДНЯЯ сложность - могут потребоваться маркеры")
    else:
        print("🟢 НИЗКАЯ сложность - DX Tool справится")


if __name__ == "__main__":
    test_improved_generator() 