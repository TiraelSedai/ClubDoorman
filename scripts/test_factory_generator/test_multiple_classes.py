#!/usr/bin/env python3
"""
Тест DX Tool на разных типах классов
"""

import sys
from pathlib import Path

# Добавляем текущую директорию в путь для импорта
sys.path.append('.')

try:
    import csharp_analyzer
    import factory_generator
    import complexity_analyzer
except ImportError as e:
    print(f"❌ Ошибка импорта: {e}")
    sys.exit(1)


def test_multiple_classes():
    """Тестирует DX Tool на разных типах классов"""
    print("🔧 Тестирование DX Tool на разных типах классов...")
    
    # Инициализируем анализатор
    analyzer = csharp_analyzer.CSharpAnalyzer("../../ClubDoorman")
    
    # Находим все классы
    services = analyzer.find_service_classes()
    print(f"📋 Найдено классов: {len(services)}")
    
    # Тестируем разные типы классов
    test_classes = [
        "MessageHandler",      # Высокая сложность (11 параметров)
        "ModerationService",   # Средняя сложность (8 параметров)
        "CaptchaService",      # Низкая сложность (2 параметра)
        "SpamHamClassifier",   # Простой (1 параметр)
        "CallbackQueryHandler" # Средняя сложность (8 параметров)
    ]
    
    results = []
    
    for class_name in test_classes:
        print(f"\n{'='*60}")
        print(f"🔍 Тестирование: {class_name}")
        print(f"{'='*60}")
        
        # Находим класс
        target_class = None
        for service in services:
            if service.name == class_name:
                target_class = service
                break
        
        if not target_class:
            print(f"❌ Класс {class_name} не найден")
            continue
        
        # Анализируем сложность
        complexity_report = analyzer.analyze_class_complexity(target_class)
        print(f"📊 Сложность: {complexity_report.complexity_score}/10 ({complexity_report.complexity_level.value})")
        print(f"📋 Параметров: {len(target_class.constructor_params)}")
        
        # Ищем маркеры
        file_path = Path("../../ClubDoorman") / target_class.file_path
        markers = analyzer.find_test_markers(file_path)
        test_markers = markers.get(target_class.name, [])
        print(f"🏷️ Маркеры: {test_markers}")
        
        # Инициализируем генератор
        generator = factory_generator.TestFactoryGenerator(Path("../../ClubDoorman/ClubDoorman.Test"))
        generator.set_complexity_analysis(complexity_report, test_markers)
        
        # Генерируем TestFactory
        factory_code = generator.generate_test_factory(target_class)
        
        # Анализируем результат
        analysis = analyze_generated_factory(factory_code, target_class, complexity_report)
        results.append({
            'class_name': class_name,
            'complexity_score': complexity_report.complexity_score,
            'complexity_level': complexity_report.complexity_level.value,
            'params_count': len(target_class.constructor_params),
            'concrete_params': len(complexity_report.concrete_types),
            'interface_params': len(complexity_report.interface_types),
            'has_fake_telegram': "FakeTelegramClient" in factory_code,
            'has_concrete_mocks': any(f"Mock<{param.type}>" in factory_code for param in target_class.constructor_params if param.is_concrete),
            'has_custom_constructor': f"public {class_name}TestFactory()" in factory_code,
            'has_additional_methods': "Additional Methods" in factory_code,
            'suggested_markers': [marker.value for marker in complexity_report.suggested_markers]
        })
        
        print(f"✅ Результат анализа:")
        print(f"  - FakeTelegramClient: {'✅' if analysis['has_fake_telegram'] else '❌'}")
        print(f"  - Моки конкретных классов: {'✅' if analysis['has_concrete_mocks'] else '❌'}")
        print(f"  - Кастомный конструктор: {'✅' if analysis['has_custom_constructor'] else '❌'}")
        print(f"  - Дополнительные методы: {'✅' if analysis['has_additional_methods'] else '❌'}")
    
    # Итоговый отчет
    print(f"\n{'='*80}")
    print(f"📊 ИТОГОВЫЙ ОТЧЕТ")
    print(f"{'='*80}")
    
    for result in results:
        print(f"\n🔍 {result['class_name']}:")
        print(f"  - Сложность: {result['complexity_score']}/10 ({result['complexity_level']})")
        print(f"  - Параметров: {result['params_count']} (конкретные: {result['concrete_params']}, интерфейсы: {result['interface_params']})")
        print(f"  - FakeTelegramClient: {'✅' if result['has_fake_telegram'] else '❌'}")
        print(f"  - Моки конкретных классов: {'✅' if result['has_concrete_mocks'] else '❌'}")
        print(f"  - Кастомный конструктор: {'✅' if result['has_custom_constructor'] else '❌'}")
        print(f"  - Дополнительные методы: {'✅' if result['has_additional_methods'] else '❌'}")
        print(f"  - Предлагаемые маркеры: {result['suggested_markers']}")
    
    # Анализ универсальности
    print(f"\n🎯 АНАЛИЗ УНИВЕРСАЛЬНОСТИ:")
    
    high_complexity = [r for r in results if r['complexity_score'] >= 7]
    medium_complexity = [r for r in results if 3 <= r['complexity_score'] < 7]
    low_complexity = [r for r in results if r['complexity_score'] < 3]
    
    print(f"  - Высокая сложность: {len(high_complexity)} классов")
    print(f"  - Средняя сложность: {len(medium_complexity)} классов")
    print(f"  - Низкая сложность: {len(low_complexity)} классов")
    
    # Проверяем покрытие
    classes_with_fake_telegram = [r for r in results if r['has_fake_telegram']]
    classes_with_concrete_mocks = [r for r in results if r['has_concrete_mocks']]
    classes_with_custom_constructor = [r for r in results if r['has_custom_constructor']]
    classes_with_additional_methods = [r for r in results if r['has_additional_methods']]
    
    print(f"\n📈 ПОКРЫТИЕ ФУНКЦИОНАЛЬНОСТИ:")
    print(f"  - Использование FakeTelegramClient: {len(classes_with_fake_telegram)}/{len(results)} классов")
    print(f"  - Моки конкретных классов: {len(classes_with_concrete_mocks)}/{len(results)} классов")
    print(f"  - Кастомные конструкторы: {len(classes_with_custom_constructor)}/{len(results)} классов")
    print(f"  - Дополнительные методы: {len(classes_with_additional_methods)}/{len(results)} классов")


def analyze_generated_factory(factory_code: str, class_info, complexity_report) -> dict:
    """Анализирует сгенерированный TestFactory"""
    return {
        'has_fake_telegram': "FakeTelegramClient" in factory_code,
        'has_concrete_mocks': any(f"Mock<{param.type}>" in factory_code for param in class_info.constructor_params if param.is_concrete),
        'has_custom_constructor': f"public {class_info.name}TestFactory()" in factory_code,
        'has_additional_methods': "Additional Methods" in factory_code
    }


if __name__ == "__main__":
    test_multiple_classes() 