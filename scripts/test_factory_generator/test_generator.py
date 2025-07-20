#!/usr/bin/env python3
"""
Тестовый генератор для сравнения с ручными улучшениями
"""

import re
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


class SimpleTestFactoryGenerator:
    """Упрощенный генератор TestFactory"""
    
    def __init__(self):
        pass
    
    def _create_concrete_instance(self, type_name: str) -> str:
        """Создает строку для конкретного экземпляра типа"""
        # Специальные случаи для конкретных типов
        concrete_instances = {
            "BadMessageManager": "new BadMessageManager()",
            "GlobalStatsManager": "new GlobalStatsManager()",
            "SpamHamClassifier": "new SpamHamClassifier(new NullLogger<SpamHamClassifier>())",
            "MimicryClassifier": "new MimicryClassifier(new NullLogger<MimicryClassifier>())",
            "SuspiciousUsersStorage": "new SuspiciousUsersStorage(new NullLogger<SuspiciousUsersStorage>())",
            "AiChecks": 'new AiChecks(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"), new NullLogger<AiChecks>())',
        }
        
        # Убираем generic параметры для проверки
        base_type = re.sub(r'<[^>]+>', '', type_name)
        
        if base_type in concrete_instances:
            return f"            {concrete_instances[base_type]}"
        else:
            # Для неизвестных типов используем мок
            return f"            new Mock<{type_name}>().Object"
    
    def _to_pascal_case(self, name: str) -> str:
        """Конвертирует имя в PascalCase"""
        if not name:
            return name
        return name[0].upper() + name[1:]
    
    def generate_test_factory(self, class_info: ClassInfo) -> str:
        """Генерирует TestFactory для класса"""
        
        # Определяем какие параметры нужно мокать
        mock_params = [p for p in class_info.constructor_params if p.is_interface]
        concrete_params = [p for p in class_info.constructor_params if p.is_concrete]
        logger_params = [p for p in class_info.constructor_params if p.is_logger]
        
        # Генерируем код
        factory_name = f"{class_info.name}TestFactory"
        
        code = f"""using ClubDoorman.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using ClubDoorman.Services;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для {class_info.name}
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class {factory_name}
{{
"""
        
        # Добавляем моки для интерфейсов
        for param in mock_params:
            code += f"    public Mock<{param.type}> {self._to_pascal_case(param.name)}Mock {{ get; }} = new();\n"
        
        code += "\n"
        
        # Метод Create
        code += f"    public {class_info.name} Create{class_info.name}()\n"
        code += "    {\n"
        code += f"        return new {class_info.name}(\n"
        
        # Параметры конструктора
        param_lines = []
        for param in class_info.constructor_params:
            if param.is_interface:
                param_lines.append(f"            {self._to_pascal_case(param.name)}Mock.Object")
            elif param.is_logger:
                # Извлекаем тип логгера из generic параметра
                logger_type_match = re.search(r'ILogger<(\w+)>', param.type)
                if logger_type_match:
                    logger_type = logger_type_match.group(1)
                    param_lines.append(f"            new NullLogger<{logger_type}>()")
                else:
                    param_lines.append(f"            new NullLogger<{class_info.name}>()")
            elif param.is_concrete:
                # Для конкретных классов создаем реальные экземпляры
                param_lines.append(self._create_concrete_instance(param.type))
        
        code += ",\n".join(param_lines)
        code += "\n        );\n"
        code += "    }\n"
        
        # Добавляем вспомогательные методы для настройки
        code += "\n"
        code += "    #region Configuration Methods\n"
        code += "\n"
        
        for param in mock_params:
            param_name_pascal = self._to_pascal_case(param.name)
            code += f"    public {factory_name} With{param_name_pascal}Setup(Action<Mock<{param.type}>> setup)\n"
            code += "    {\n"
            code += f"        setup({param_name_pascal}Mock);\n"
            code += "        return this;\n"
            code += "    }\n\n"
        
        code += "    #endregion\n"
        code += "}\n"
        
        return code


def compare_generated_vs_manual():
    """Сравнивает сгенерированный TestFactory с ручным"""
    print("🔍 Сравнение сгенерированного vs ручного TestFactory...")
    
    # Анализируем MessageHandler
    analyzer = SimpleCSharpAnalyzer("../../ClubDoorman")
    class_info = analyzer.find_message_handler()
    
    if not class_info:
        print("❌ Не удалось найти MessageHandler")
        return
    
    # Генерируем TestFactory
    generator = SimpleTestFactoryGenerator()
    generated_code = generator.generate_test_factory(class_info)
    
    print(f"\n📊 Сгенерированный TestFactory:")
    print("=" * 80)
    print(generated_code)
    print("=" * 80)
    
    # Читаем ручной TestFactory
    manual_file = Path("../../ClubDoorman/ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactory.cs")
    if manual_file.exists():
        manual_code = manual_file.read_text(encoding='utf-8')
        print(f"\n📊 Ручной TestFactory:")
        print("=" * 80)
        print(manual_code)
        print("=" * 80)
        
        # Анализируем различия
        print(f"\n🔍 Анализ различий:")
        
        # Проверяем наличие кастомного конструктора
        if "public MessageHandlerTestFactory()" in manual_code:
            print("✅ Ручной: Есть кастомный конструктор")
        else:
            print("❌ Сгенерированный: Нет кастомного конструктора")
        
        # Проверяем наличие FakeTelegramClient
        if "FakeTelegramClient" in manual_code:
            print("✅ Ручной: Есть FakeTelegramClient")
        else:
            print("❌ Сгенерированный: Нет FakeTelegramClient")
        
        # Проверяем наличие дополнительных методов
        if "CreateMessageHandlerWithFake" in manual_code:
            print("✅ Ручной: Есть дополнительные методы")
        else:
            print("❌ Сгенерированный: Нет дополнительных методов")
        
        # Проверяем моки конкретных классов
        if "Mock<AiChecks>" in manual_code:
            print("✅ Ручной: Есть мок конкретного класса AiChecks")
        else:
            print("❌ Сгенерированный: Нет мока конкретного класса AiChecks")
        
        print(f"\n📊 Выводы:")
        print("1. DX Tool не может определить когда нужно мокать конкретные классы")
        print("2. DX Tool не знает о тестовых утилитах (FakeTelegramClient)")
        print("3. DX Tool не генерирует кастомную инициализацию")
        print("4. DX Tool не создает дополнительные методы")
        
    else:
        print("❌ Ручной TestFactory не найден")


if __name__ == "__main__":
    compare_generated_vs_manual() 