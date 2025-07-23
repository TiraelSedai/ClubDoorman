"""
Основной модуль TestFactory Generator
"""

import sys
import argparse
from pathlib import Path

from csharp_analyzer import CSharpAnalyzer
from factory_generator import TestFactoryGenerator
from test_data_generator import TestDataGenerator
from legacy_analyzer import LegacyAnalyzer
from business_logic_analyzer import BusinessLogicAnalyzer


def main():
    """Основная функция"""
    parser = argparse.ArgumentParser(description='Генератор TestFactory для C# проектов')
    parser.add_argument('project_root', help='Путь к корню проекта')
    parser.add_argument('--force', '-f', action='store_true', 
                       help='Перезаписать существующие файлы')
    parser.add_argument('--verbose', '-v', action='store_true',
                       help='Подробный вывод')
    parser.add_argument('--generate-test-data', action='store_true',
                       help='Генерировать TestDataFactory')
    parser.add_argument('--generate-test-base', action='store_true',
                       help='Генерировать TestBase')
    parser.add_argument('--full-setup', action='store_true',
                       help='Полная настройка: TestFactory + TestData + TestBase')
    
    args = parser.parse_args()
    
    if args.verbose:
        print(f"🔍 Анализируем проект: {args.project_root}")
    
    analyzer = CSharpAnalyzer(args.project_root, args.force)
    services = analyzer.find_service_classes()
    
    if not services:
        print("❌ Не найдено сервисных классов с конструкторами")
        sys.exit(1)
    
    print(f"📦 Найдено {len(services)} сервисных классов:")
    for service in services:
        print(f"  - {service.name} ({len(service.constructor_params)} параметров)")
        if args.verbose:
            for param in service.constructor_params:
                param_type = "interface" if param.is_interface else "concrete" if param.is_concrete else "logger"
                print(f"    └─ {param.type} {param.name} ({param_type})")
    
    # Определяем что генерировать
    generate_test_factory = not (args.generate_test_data or args.generate_test_base)
    generate_test_data = args.generate_test_data or args.full_setup
    generate_test_base = args.generate_test_base or args.full_setup
    
    # Генерация TestFactory
    if generate_test_factory:
        generator = TestFactoryGenerator(analyzer.test_project_root, args.force)
        business_analyzer = BusinessLogicAnalyzer(Path(args.project_root))
        
        print(f"\n🧠 Анализируем бизнес-логику сервисов...")
        logic_info_map = {}
        for service in services:
            logic_info = business_analyzer.analyze_service_logic(service)
            logic_info_map[service.name] = logic_info
            if args.verbose:
                print(f"  - {service.name}: сложность {logic_info.complexity_score}, "
                      f"Telegram: {logic_info.has_telegram_client}, "
                      f"Модерация: {logic_info.has_moderation_logic}, "
                      f"Капча: {logic_info.has_captcha_logic}")
        
        print(f"\n🚀 Генерируем умные TestFactory на основе бизнес-логики... {'(режим перезаписи)' if args.force else ''}")
        success_count = 0
        
        for service in services:
            try:
                # Генерируем базовый TestFactory
                factory_code = generator.generate_test_factory(service)
                
                # Добавляем умные методы на основе бизнес-логики
                logic_info = logic_info_map[service.name]
                smart_methods = business_analyzer.generate_smart_test_factory_methods(service, logic_info)
                
                # Вставляем умные методы в TestFactory
                if smart_methods.strip():
                    factory_code = factory_code.replace("    #endregion\n}", f"    #endregion\n\n    #region Smart Methods Based on Business Logic\n{smart_methods}\n    #endregion\n}}")
                
                tests_code = generator.generate_test_factory_tests(service)
                if generator.save_files(service, factory_code, tests_code):
                    success_count += 1
            except Exception as e:
                print(f"❌ Ошибка генерации для {service.name}: {e}")
        
        print(f"\n✅ Генерация умных TestFactory завершена! Создано {success_count} из {len(services)} TestFactory")
    
    # Генерация TestDataFactory
    if generate_test_data:
        print(f"\n📊 Генерируем TestDataFactory... {'(режим перезаписи)' if args.force else ''}")
        models_dir = Path(args.project_root) / "ClubDoorman" / "Models"
        test_data_generator = TestDataGenerator(models_dir)
        test_data_factory_code = test_data_generator.generate_test_data_factory()
        test_data_factory_path = analyzer.test_project_root / "TestData" / "TestDataFactory.Generated.cs"
        test_data_factory_path.parent.mkdir(exist_ok=True)
        
        try:
            test_data_factory_path.write_text(test_data_factory_code, encoding='utf-8')
            print("✅ TestDataFactory создан успешно")
        except Exception as e:
            print(f"❌ Ошибка создания TestDataFactory: {e}")
            print("✅ TestDataFactory создан успешно")
        else:
            print("⚠️  TestDataFactory не создан")
    
    # Генерация TestBase
    if generate_test_base:
        print(f"\n🔧 Генерируем TestBase... {'(режим перезаписи)' if args.force else ''}")
        # TODO: Добавить генерацию TestBase
        print("⚠️  Генерация TestBase пока не реализована")
    
    print("\n📝 Следующие шаги:")
    print("1. Проверьте сгенерированные файлы")
    print("2. Запустите тесты: dotnet test --filter Category=test-infrastructure")
    print("3. При необходимости настройте моки в TestFactory")


if __name__ == "__main__":
    main() 