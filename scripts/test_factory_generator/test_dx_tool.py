#!/usr/bin/env python3
"""
Тестовый скрипт для оценки DX утилиты
"""

import sys
import os
from pathlib import Path

# Добавляем текущую директорию в путь
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from csharp_analyzer import CSharpAnalyzer
from factory_generator import TestFactoryGenerator
from business_logic_analyzer import BusinessLogicAnalyzer
from complexity_analyzer import ComplexityAnalyzer

def test_dx_tool():
    """Тестируем DX утилиту на реальных классах"""
    
    print("🧪 ТЕСТИРОВАНИЕ DX УТИЛИТЫ")
    print("=" * 50)
    
    # Пути к проекту
    project_root = Path("../../ClubDoorman")
    test_project_root = Path("../../ClubDoorman.Test")
    
    print(f"📁 Проект: {project_root}")
    print(f"📁 Тесты: {test_project_root}")
    
    # 1. Анализ классов
    print("\n🔍 ЭТАП 1: Анализ классов")
    print(f"Проверяем пути:")
    print(f"  - project_root: {project_root} (существует: {project_root.exists()})")
    print(f"  - Services: {project_root / 'Services'} (существует: {(project_root / 'Services').exists()})")
    print(f"  - Handlers: {project_root / 'Handlers'} (существует: {(project_root / 'Handlers').exists()})")
    
    analyzer = CSharpAnalyzer(str(project_root), force_overwrite=False)
    services = analyzer.find_service_classes()
    
    print(f"Найдено {len(services)} сервисных классов:")
    for service in services:
        print(f"  - {service.name} ({len(service.constructor_params)} параметров)")
    
    # 2. Анализ сложности
    print("\n🧠 ЭТАП 2: Анализ сложности")
    complexity_analyzer = ComplexityAnalyzer()
    
    for service in services:
        complexity_report = analyzer.analyze_class_complexity(service)
        print(f"  {service.name}: {complexity_report.complexity_score}/10 ({complexity_report.complexity_level.value})")
        print(f"    Обоснование: {', '.join(complexity_report.reasoning)}")
    
    # 3. Анализ бизнес-логики
    print("\n💼 ЭТАП 3: Анализ бизнес-логики")
    business_analyzer = BusinessLogicAnalyzer(project_root)
    
    for service in services:
        logic_info = business_analyzer.analyze_service_logic(service)
        print(f"  {service.name}:")
        print(f"    - Telegram клиент: {logic_info.has_telegram_client}")
        print(f"    - Модерация: {logic_info.has_moderation_logic}")
        print(f"    - Капча: {logic_info.has_captcha_logic}")
        print(f"    - Пользователи: {logic_info.has_user_management}")
        print(f"    - AI проверки: {logic_info.has_ai_checks}")
    
    # 4. Генерация TestFactory
    print("\n🚀 ЭТАП 4: Генерация TestFactory")
    generator = TestFactoryGenerator(test_project_root, force_overwrite=False)
    
    success_count = 0
    for service in services:
        try:
            # Анализируем сложность и бизнес-логику
            complexity_report = analyzer.analyze_class_complexity(service)
            logic_info = business_analyzer.analyze_service_logic(service)
            
            # Генерируем TestFactory
            factory_code = generator.generate_test_factory(service)
            
            # Добавляем умные методы
            smart_methods = business_analyzer.generate_smart_test_factory_methods(service, logic_info)
            if smart_methods.strip():
                factory_code = factory_code.replace("    #endregion\n}", 
                    f"    #endregion\n\n    #region Smart Methods Based on Business Logic\n{smart_methods}\n    #endregion\n}}")
            
            # Сохраняем файл
            if generator.save_files(service, factory_code, ""):
                success_count += 1
                print(f"  ✅ {service.name} - создан")
            else:
                print(f"  ⚠️  {service.name} - уже существует")
                
        except Exception as e:
            print(f"  ❌ {service.name} - ошибка: {e}")
    
    print(f"\n📊 РЕЗУЛЬТАТЫ:")
    print(f"  - Всего классов: {len(services)}")
    print(f"  - Успешно создано: {success_count}")
    if len(services) > 0:
        print(f"  - Успешность: {success_count/len(services)*100:.1f}%")
    else:
        print(f"  - Успешность: N/A (нет классов для анализа)")

if __name__ == "__main__":
    test_dx_tool() 