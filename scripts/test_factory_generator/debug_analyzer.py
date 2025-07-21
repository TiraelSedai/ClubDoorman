#!/usr/bin/env python3
"""
Отладочный скрипт для проверки работы анализатора
"""

import sys
from pathlib import Path

# Добавляем текущую директорию в путь для импорта
sys.path.append('.')

try:
    from models import ClassInfo, ConstructorParam
    from complexity_analyzer import ComplexityAnalyzer, ComplexityReport
except ImportError:
    from models import ClassInfo, ConstructorParam
    from complexity_analyzer import ComplexityAnalyzer, ComplexityReport


def debug_analyzer():
    """Отлаживает работу анализатора"""
    print("🔍 Отладка анализатора...")
    
    # Проверяем путь
    project_root = Path("../../ClubDoorman")
    print(f"📁 Проект: {project_root}")
    print(f"📁 Существует: {project_root.exists()}")
    
    # Проверяем папки
    handlers_dir = project_root / "Handlers"
    print(f"📁 Handlers: {handlers_dir}")
    print(f"📁 Существует: {handlers_dir.exists()}")
    
    if handlers_dir.exists():
        for file in handlers_dir.glob("*.cs"):
            print(f"📄 Файл: {file}")
    
    # Проверяем MessageHandler напрямую
    message_handler_file = handlers_dir / "MessageHandler.cs"
    print(f"📄 MessageHandler: {message_handler_file}")
    print(f"📄 Существует: {message_handler_file.exists()}")
    
    if message_handler_file.exists():
        try:
            content = message_handler_file.read_text(encoding='utf-8')
            print(f"📄 Размер: {len(content)} символов")
            
            # Ищем класс
            import re
            class_pattern = r'public\s+(?:sealed\s+)?class\s+(\w+)'
            matches = re.findall(class_pattern, content)
            print(f"🔍 Найденные классы: {matches}")
            
            # Ищем конструктор
            constructor_pattern = r'public\s+MessageHandler\s*\(([^)]*)\)'
            constructor_match = re.search(constructor_pattern, content)
            if constructor_match:
                print(f"🔧 Конструктор найден: {constructor_match.group(1)}")
            else:
                print("❌ Конструктор не найден")
                
        except Exception as e:
            print(f"❌ Ошибка чтения: {e}")


if __name__ == "__main__":
    debug_analyzer() 