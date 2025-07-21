#!/usr/bin/env python3
"""
Отладочный скрипт для проверки find_service_classes
"""

import sys
from pathlib import Path

# Добавляем текущую директорию в путь для импорта
sys.path.append('.')

try:
    import csharp_analyzer
except ImportError:
    print("❌ Не удалось импортировать csharp_analyzer")


def debug_finder():
    """Отлаживает find_service_classes"""
    print("🔍 Отладка find_service_classes...")
    
    # Инициализируем анализатор
    analyzer = csharp_analyzer.CSharpAnalyzer("../../ClubDoorman")
    
    # Проверяем пути
    project_root = Path("../../ClubDoorman")
    services_dir = project_root / "ClubDoorman" / "Services"
    handlers_dir = project_root / "Handlers"
    
    print(f"📁 Проект: {project_root}")
    print(f"📁 Services: {services_dir} (существует: {services_dir.exists()})")
    print(f"📁 Handlers: {handlers_dir} (существует: {handlers_dir.exists()})")
    
    # Проверяем файлы в Handlers
    if handlers_dir.exists():
        print(f"\n📄 Файлы в Handlers:")
        for file in handlers_dir.glob("*.cs"):
            print(f"  - {file.name}")
    
    # Проверяем файлы в Services
    if services_dir.exists():
        print(f"\n📄 Файлы в Services:")
        for file in services_dir.glob("*.cs"):
            print(f"  - {file.name}")
    
    # Ищем сервисы
    print(f"\n🔍 Поиск сервисов...")
    services = analyzer.find_service_classes()
    print(f"📋 Найдено сервисов: {len(services)}")
    
    for service in services:
        print(f"  - {service.name} ({len(service.constructor_params)} параметров)")
        for param in service.constructor_params:
            print(f"    * {param.type} {param.name}")


if __name__ == "__main__":
    debug_finder() 