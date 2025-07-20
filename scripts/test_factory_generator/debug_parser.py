#!/usr/bin/env python3
"""
Отладочный скрипт для проверки парсинга C# файлов
"""

import sys
import re
from pathlib import Path

# Добавляем текущую директорию в путь для импорта
sys.path.append('.')

try:
    from models import ClassInfo, ConstructorParam
except ImportError:
    from models import ClassInfo, ConstructorParam


def debug_parser():
    """Отлаживает парсинг C# файлов"""
    print("🔍 Отладка парсинга C# файлов...")
    
    # Читаем MessageHandler.cs
    file_path = Path("../../ClubDoorman/Handlers/MessageHandler.cs")
    
    try:
        content = file_path.read_text(encoding='utf-8')
        print(f"📄 Размер файла: {len(content)} символов")
        
        # Ищем namespace
        namespace_match = re.search(r'namespace\s+(\S+)', content)
        namespace = namespace_match.group(1) if namespace_match else "Unknown"
        namespace = namespace.rstrip(';')
        print(f"📁 Namespace: {namespace}")
        
        # Ищем классы
        class_pattern = r'public\s+(?:sealed\s+)?class\s+(\w+)'
        class_matches = re.findall(class_pattern, content)
        print(f"🔍 Найденные классы: {class_matches}")
        
        # Ищем конструкторы для каждого класса
        for class_name in class_matches:
            print(f"\n🔧 Анализ класса: {class_name}")
            
            # Ищем конструктор
            constructor_pattern = rf'public\s+{class_name}\s*\(([^)]*)\)'
            constructor_match = re.search(constructor_pattern, content)
            
            if constructor_match:
                constructor_params_str = constructor_match.group(1)
                print(f"✅ Конструктор найден: {constructor_params_str}")
                
                # Парсим параметры
                params = parse_constructor_params(constructor_params_str)
                print(f"📋 Параметров: {len(params)}")
                for i, param in enumerate(params, 1):
                    print(f"  {i}. {param.type} {param.name} (interface: {param.is_interface}, concrete: {param.is_concrete}, logger: {param.is_logger})")
            else:
                print("❌ Конструктор не найден")
                
    except Exception as e:
        print(f"❌ Ошибка: {e}")


def parse_constructor_params(params_str: str) -> list:
    """Парсит параметры конструктора"""
    params = []
    
    # Убираем комментарии и лишние пробелы
    params_str = re.sub(r'//.*$', '', params_str, flags=re.MULTILINE)
    params_str = params_str.strip()
    
    if not params_str:
        return params
    
    # Разбиваем по запятой, учитывая generic типы
    param_parts = split_params(params_str)
    
    for part in param_parts:
        if not part:
            continue
            
        # Ищем тип и имя параметра
        param_match = re.search(r'(\w+(?:<[^>]+>)?)\s+(\w+)', part)
        if param_match:
            param_type = param_match.group(1)
            param_name = param_match.group(2)
            
            # Улучшенная логика определения типов
            is_interface = is_interface_type(param_type)
            is_logger = 'ILogger' in param_type
            is_concrete = not is_interface and not is_logger
            
            params.append(ConstructorParam(
                type=param_type,
                name=param_name,
                is_interface=is_interface,
                is_logger=is_logger,
                is_concrete=is_concrete
            ))
    
    return params


def split_params(params_str: str) -> list:
    """Разбивает параметры с учетом generic типов"""
    parts = []
    current_part = ""
    bracket_count = 0
    
    for char in params_str:
        if char == '<':
            bracket_count += 1
        elif char == '>':
            bracket_count -= 1
        elif char == ',' and bracket_count == 0:
            parts.append(current_part.strip())
            current_part = ""
            continue
        
        current_part += char
    
    if current_part.strip():
        parts.append(current_part.strip())
    
    return parts


def is_interface_type(type_name: str) -> bool:
    """Определяет, является ли тип интерфейсом"""
    # Расширенный список известных интерфейсов
    known_interfaces = {
        'ITelegramBotClient', 'ITelegramBotClientWrapper', 'IUserManager', 'IModerationService', 
        'ICaptchaService', 'IStatisticsService', 'IUpdateDispatcher',
        'IUpdateHandler', 'ICommandHandler', 'ISpamHamClassifier',
        'IMimicryClassifier', 'IBadMessageManager', 'IAiChecks',
        'ISuspiciousUsersStorage', 'ILogger', 'IServiceProvider',
        'IEnumerable', 'ICollection', 'IList', 'IDictionary',
        'IComparable', 'IDisposable', 'IAsyncDisposable',
        'IEquatable', 'IFormattable', 'IConvertible'
    }
    
    # Убираем generic параметры для проверки
    base_type = re.sub(r'<[^>]+>', '', type_name)
    
    # Проверяем по паттерну I + заглавная буква
    if base_type.startswith('I') and len(base_type) > 1 and base_type[1:2].isupper():
        return True
    
    # Проверяем по списку известных интерфейсов
    return base_type in known_interfaces


if __name__ == "__main__":
    debug_parser() 