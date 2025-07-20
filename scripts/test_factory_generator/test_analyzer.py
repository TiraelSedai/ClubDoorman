#!/usr/bin/env python3
"""
Тестовый скрипт для анализа MessageHandler
"""

import re
import sys
from pathlib import Path
from dataclasses import dataclass
from typing import List


@dataclass
class ConstructorParam:
    """Параметр конструктора"""
    type: str
    name: str
    is_interface: bool
    is_logger: bool
    is_concrete: bool


@dataclass
class ClassInfo:
    """Информация о классе"""
    name: str
    namespace: str
    constructor_params: List[ConstructorParam]
    file_path: str


class SimpleCSharpAnalyzer:
    """Упрощенный анализатор C# кода"""
    
    def __init__(self, project_root: str):
        self.project_root = Path(project_root)
        
    def find_message_handler(self) -> ClassInfo:
        """Находит MessageHandler класс"""
        handlers_dir = self.project_root / "Handlers"
        message_handler_file = handlers_dir / "MessageHandler.cs"
        
        if not message_handler_file.exists():
            print(f"❌ Файл {message_handler_file} не найден")
            return None
        
        return self._parse_file(message_handler_file)
    
    def _parse_file(self, file_path: Path) -> ClassInfo:
        """Парсит C# файл и извлекает информацию о MessageHandler"""
        try:
            content = file_path.read_text(encoding='utf-8')
        except Exception as e:
            print(f"Ошибка чтения файла {file_path}: {e}")
            return None
        
        # Ищем namespace
        namespace_match = re.search(r'namespace\s+(\S+)', content)
        namespace = namespace_match.group(1) if namespace_match else "Unknown"
        namespace = namespace.rstrip(';')
        
        # Ищем MessageHandler класс с конструктором
        class_pattern = r'public\s+(?:sealed\s+)?class\s+MessageHandler[^{]*?{.*?public\s+MessageHandler\s*\(([^)]*)\)[^{]*?{'
        class_match = re.search(class_pattern, content, re.DOTALL)
        
        if not class_match:
            print("❌ Конструктор MessageHandler не найден")
            return None
        
        constructor_params_str = class_match.group(1)
        
        # Парсим параметры конструктора
        params = self._parse_constructor_params(constructor_params_str)
        
        return ClassInfo(
            name="MessageHandler",
            namespace=namespace,
            constructor_params=params,
            file_path=str(file_path.relative_to(self.project_root))
        )
    
    def _parse_constructor_params(self, params_str: str) -> List[ConstructorParam]:
        """Парсит параметры конструктора"""
        params = []
        
        # Убираем комментарии и лишние пробелы
        params_str = re.sub(r'//.*$', '', params_str, flags=re.MULTILINE)
        params_str = params_str.strip()
        
        if not params_str:
            return params
        
        # Разбиваем по запятой, учитывая generic типы
        param_parts = self._split_params(params_str)
        
        for part in param_parts:
            if not part:
                continue
                
            # Ищем тип и имя параметра
            param_match = re.search(r'(\w+(?:<[^>]+>)?)\s+(\w+)', part)
            if param_match:
                param_type = param_match.group(1)
                param_name = param_match.group(2)
                
                # Улучшенная логика определения типов
                is_interface = self._is_interface_type(param_type)
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
    
    def _split_params(self, params_str: str) -> List[str]:
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
    
    def _is_interface_type(self, type_name: str) -> bool:
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


def analyze_message_handler():
    """Анализирует MessageHandler и показывает что DX Tool определит"""
    print("🔍 Анализ MessageHandler...")
    
    analyzer = SimpleCSharpAnalyzer("../../ClubDoorman")
    class_info = analyzer.find_message_handler()
    
    if not class_info:
        print("❌ Не удалось найти MessageHandler")
        return
    
    print(f"\n📊 Найден класс: {class_info.name}")
    print(f"📁 Namespace: {class_info.namespace}")
    print(f"📄 Файл: {class_info.file_path}")
    print(f"🔧 Параметров конструктора: {len(class_info.constructor_params)}")
    
    print(f"\n📋 Параметры конструктора:")
    for i, param in enumerate(class_info.constructor_params, 1):
        param_type = "interface" if param.is_interface else "concrete" if param.is_concrete else "logger"
        print(f"  {i}. {param.type} {param.name} ({param_type})")
    
    # Анализ сложности
    print(f"\n🔍 Анализ сложности:")
    
    concrete_mocks = [p for p in class_info.constructor_params if p.is_concrete]
    interfaces = [p for p in class_info.constructor_params if p.is_interface]
    loggers = [p for p in class_info.constructor_params if p.is_logger]
    
    print(f"  - Конкретные классы: {len(concrete_mocks)}")
    for param in concrete_mocks:
        print(f"    * {param.type} {param.name}")
    
    print(f"  - Интерфейсы: {len(interfaces)}")
    for param in interfaces:
        print(f"    * {param.type} {param.name}")
    
    print(f"  - Логгеры: {len(loggers)}")
    for param in loggers:
        print(f"    * {param.type} {param.name}")
    
    # Определяем сложность
    complexity_score = 0
    if len(concrete_mocks) > 0:
        complexity_score += len(concrete_mocks) * 2
    if len(class_info.constructor_params) > 8:
        complexity_score += 2
    
    print(f"\n📊 Оценка сложности: {complexity_score}/10")
    if complexity_score < 3:
        print("🟢 НИЗКАЯ сложность - DX Tool справится")
    elif complexity_score < 7:
        print("🟡 СРЕДНЯЯ сложность - могут потребоваться маркеры")
    else:
        print("🔴 ВЫСОКАЯ сложность - точно нужны маркеры")


if __name__ == "__main__":
    analyze_message_handler() 