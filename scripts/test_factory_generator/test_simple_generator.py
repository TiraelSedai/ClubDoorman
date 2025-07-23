#!/usr/bin/env python3
"""
Упрощенный тест улучшенного генератора с тестовыми данными
"""

import sys
from pathlib import Path

# Добавляем текущую директорию в путь для импорта
sys.path.append('.')

try:
    from models import ClassInfo, ConstructorParam
    from complexity_analyzer import ComplexityAnalyzer, ComplexityReport
    from factory_generator import TestFactoryGenerator
except ImportError:
    from models import ClassInfo, ConstructorParam
    from complexity_analyzer import ComplexityAnalyzer, ComplexityReport
    from factory_generator import TestFactoryGenerator


def test_simple_generator():
    """Тестирует улучшенный генератор с тестовыми данными"""
    print("🔧 Тестирование улучшенного DX Tool с тестовыми данными...")
    
    # Создаем тестовые данные для MessageHandler
    test_params = [
        ConstructorParam(type='ITelegramBotClientWrapper', name='bot', is_interface=True, is_logger=False, is_concrete=False),
        ConstructorParam(type='IModerationService', name='moderationService', is_interface=True, is_logger=False, is_concrete=False),
        ConstructorParam(type='ICaptchaService', name='captchaService', is_interface=True, is_logger=False, is_concrete=False),
        ConstructorParam(type='IUserManager', name='userManager', is_interface=True, is_logger=False, is_concrete=False),
        ConstructorParam(type='SpamHamClassifier', name='classifier', is_interface=False, is_logger=False, is_concrete=True),
        ConstructorParam(type='BadMessageManager', name='badMessageManager', is_interface=False, is_logger=False, is_concrete=True),
        ConstructorParam(type='AiChecks', name='aiChecks', is_interface=False, is_logger=False, is_concrete=True),
        ConstructorParam(type='GlobalStatsManager', name='globalStatsManager', is_interface=False, is_logger=False, is_concrete=True),
        ConstructorParam(type='IStatisticsService', name='statisticsService', is_interface=True, is_logger=False, is_concrete=False),
        ConstructorParam(type='IServiceProvider', name='serviceProvider', is_interface=True, is_logger=False, is_concrete=False),
        ConstructorParam(type='ILogger<MessageHandler>', name='logger', is_interface=True, is_logger=True, is_concrete=False),
    ]
    
    message_handler = ClassInfo(
        name="MessageHandler",
        namespace="ClubDoorman.Handlers",
        constructor_params=test_params,
        file_path="Handlers/MessageHandler.cs"
    )
    
    print(f"✅ Создан {message_handler.name}")
    
    # Анализируем сложность
    analyzer = ComplexityAnalyzer()
    params_dict = []
    for param in message_handler.constructor_params:
        params_dict.append({
            'type': param.type,
            'name': param.name,
            'is_interface': param.is_interface,
            'is_logger': param.is_logger,
            'is_concrete': param.is_concrete
        })
    
    complexity_report = analyzer.analyze_constructor(
        class_name=message_handler.name,
        namespace=message_handler.namespace,
        constructor_params=params_dict
    )
    
    print(f"📊 Сложность: {complexity_report.complexity_score}/10 ({complexity_report.complexity_level.value})")
    print(f"💡 Предлагаемые маркеры: {[marker.value for marker in complexity_report.suggested_markers]}")
    
    # Инициализируем генератор
    generator = TestFactoryGenerator(Path("../../ClubDoorman/ClubDoorman.Test"))
    
    # Устанавливаем анализ сложности
    generator.set_complexity_analysis(complexity_report, [])
    
    # Генерируем TestFactory
    factory_code = generator.generate_test_factory(message_handler)
    
    print(f"\n📝 Сгенерированный TestFactory:")
    print("=" * 80)
    print(factory_code)
    print("=" * 80)
    
    # Анализируем улучшения
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
    
    if complexity_report.complexity_score >= 7:
        print("🔴 ВЫСОКАЯ сложность - нужны маркеры")
    elif complexity_report.complexity_score >= 3:
        print("🟡 СРЕДНЯЯ сложность - могут потребоваться маркеры")
    else:
        print("🟢 НИЗКАЯ сложность - DX Tool справится")


if __name__ == "__main__":
    test_simple_generator() 