"""
Основной модуль TestFactory Generator
"""

import sys
import argparse
from pathlib import Path

from .csharp_analyzer import CSharpAnalyzer
from .factory_generator import TestFactoryGenerator
from .test_data_generator import TestDataGenerator


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
        
        print(f"\n🚀 Генерируем TestFactory... {'(режим перезаписи)' if args.force else ''}")
        success_count = 0
        
        for service in services:
            try:
                factory_code = generator.generate_test_factory(service)
                tests_code = generator.generate_test_factory_tests(service)
                if generator.save_files(service, factory_code, tests_code):
                    success_count += 1
            except Exception as e:
                print(f"❌ Ошибка генерации для {service.name}: {e}")
        
        print(f"\n✅ Генерация TestFactory завершена! Создано {success_count} из {len(services)} TestFactory")
    
    # Генерация TestDataFactory
    if generate_test_data:
        print(f"\n📊 Генерируем TestDataFactory... {'(режим перезаписи)' if args.force else ''}")
        test_data_generator = TestDataGenerator(args.project_root, analyzer.test_project_root)
        if test_data_generator.save_test_data_factory(args.force):
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