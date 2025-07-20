"""
Утилиты для TestFactory Generator
"""

import re
from typing import List


def to_pascal_case(name: str) -> str:
    """Конвертирует имя в PascalCase"""
    if not name:
        return name
    return name[0].upper() + name[1:]


def split_params(params_str: str) -> List[str]:
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
        'ITelegramBotClient', 'IUserManager', 'IModerationService', 
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


def get_base_type(type_name: str) -> str:
    """Убирает generic параметры из типа"""
    return re.sub(r'<[^>]+>', '', type_name)


def clean_constructor_params(params_str: str) -> str:
    """Очищает строку параметров от комментариев и лишних пробелов"""
    # Убираем комментарии и лишние пробелы
    params_str = re.sub(r'//.*$', '', params_str, flags=re.MULTILINE)
    return params_str.strip() 