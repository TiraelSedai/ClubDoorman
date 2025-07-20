"""
Основной модуль TestFactory Generator
"""

import sys
import argparse
from pathlib import Path

from .csharp_analyzer import CSharpAnalyzer
from .factory_generator import TestFactoryGenerator


def main():
    """Основная функция"""
    parser = argparse.ArgumentParser(description='Генератор TestFactory для C# проектов')
    parser.add_argument('project_root', help='Путь к корню проекта')
    parser.add_argument('--force', '-f', action='store_true', 
                       help='Перезаписать существующие файлы')
    parser.add_argument('--verbose', '-v', action='store_true',
                       help='Подробный вывод')
    
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
    
    print(f"\n✅ Генерация завершена! Создано {success_count} из {len(services)} TestFactory")
    print("\n📝 Следующие шаги:")
    print("1. Проверьте сгенерированные файлы")
    print("2. Запустите тесты: dotnet test --filter Category=test-infrastructure")
    print("3. При необходимости настройте моки в TestFactory")


if __name__ == "__main__":
    main() 