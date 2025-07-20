#!/usr/bin/env python3
"""
Генератор тестовых данных для C# моделей
"""

import re
from pathlib import Path
from typing import List, Dict, Optional
from dataclasses import dataclass

try:
    from .models import ClassInfo
except ImportError:
    from models import ClassInfo


@dataclass
class PropertyInfo:
    """Информация о свойстве модели"""
    name: str
    type: str
    is_nullable: bool = False
    is_collection: bool = False
    is_enum: bool = False
    enum_values: List[str] = None


@dataclass
class ModelInfo:
    """Информация о модели данных"""
    name: str
    namespace: str
    properties: List[PropertyInfo]
    is_record: bool = False
    is_enum: bool = False
    enum_values: List[str] = None


class TestDataGenerator:
    """Генератор тестовых данных"""
    
    def __init__(self, models_dir: Path):
        self.models_dir = models_dir
        self.known_types = {
            'string': 'string',
            'int': 'int',
            'long': 'long',
            'double': 'double',
            'float': 'float',
            'bool': 'bool',
            'DateTime': 'DateTime',
            'User': 'User',
            'Message': 'Message',
            'Chat': 'Chat',
            'Update': 'Update',
            'CallbackQuery': 'CallbackQuery',
            'ChatMemberUpdated': 'ChatMemberUpdated',
            'ModerationResult': 'ModerationResult',
            'ModerationAction': 'ModerationAction',
            'SuspiciousUserInfo': 'SuspiciousUserInfo',
            'CancellationTokenSource': 'CancellationTokenSource'
        }
    
    def _parse_csharp_file(self, file_path: Path) -> List[ModelInfo]:
        """Парсит C# файл и извлекает модели"""
        models = []
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
        except FileNotFoundError:
            return models
        
        # Поиск record'ов
        record_pattern = r'record\s+(\w+)(?:<[^>]+>)?\s*\(([^)]*)\)'
        for match in re.finditer(record_pattern, content):
            model_name = match.group(1)
            params_str = match.group(2)
            
            properties = self._parse_record_parameters(params_str)
            models.append(ModelInfo(
                name=model_name,
                namespace=self._extract_namespace(content),
                properties=properties,
                is_record=True
            ))
        
        # Поиск enum'ов
        enum_pattern = r'enum\s+(\w+)\s*\{([^}]*)\}'
        for match in re.finditer(enum_pattern, content):
            enum_name = match.group(1)
            enum_content = match.group(2)
            
            enum_values = self._parse_enum_values(enum_content)
            models.append(ModelInfo(
                name=enum_name,
                namespace=self._extract_namespace(content),
                properties=[],
                is_enum=True,
                enum_values=enum_values
            ))
        
        return models
    
    def _parse_record_parameters(self, params_str: str) -> List[PropertyInfo]:
        """Парсит параметры record'а"""
        properties = []
        
        if not params_str.strip():
            return properties
        
        # Удаляем комментарии /// и разбиваем по строкам
        lines = params_str.split('\n')
        clean_lines = []
        
        for line in lines:
            line = line.strip()
            if line and not line.startswith('///'):
                clean_lines.append(line)
        
        # Объединяем в одну строку и разбиваем по запятой
        clean_content = ' '.join(clean_lines)
        
        # Разбиваем параметры по запятой, учитывая вложенные скобки
        param_lines = []
        current_param = ""
        paren_count = 0
        
        for char in clean_content:
            if char == '(':
                paren_count += 1
            elif char == ')':
                paren_count -= 1
            elif char == ',' and paren_count == 0:
                param_lines.append(current_param.strip())
                current_param = ""
                continue
            
            current_param += char
        
        if current_param.strip():
            param_lines.append(current_param.strip())
        
        for param_line in param_lines:
            if not param_line:
                continue
            
            # Парсим параметр: Type Name = DefaultValue
            param_match = re.search(r'(\w+(?:<[^>]+>)?(?:\?)?)\s+(\w+)(?:\s*=\s*(.+))?', param_line)
            if param_match:
                prop_type = param_match.group(1)
                prop_name = param_match.group(2)
                
                # Определяем характеристики типа
                is_nullable = prop_type.endswith('?')
                is_collection = 'List<' in prop_type or 'IEnumerable<' in prop_type
                is_enum = prop_type in self.known_types and prop_type.endswith('Action')
                
                properties.append(PropertyInfo(
                    name=prop_name,
                    type=prop_type,
                    is_nullable=is_nullable,
                    is_collection=is_collection,
                    is_enum=is_enum
                ))
        
        return properties
    
    def _parse_enum_values(self, enum_content: str) -> List[str]:
        """Парсит значения enum'а"""
        values = []
        
        # Удаляем комментарии /// и разбиваем по строкам
        lines = enum_content.split('\n')
        
        for line in lines:
            line = line.strip()
            if line and not line.startswith('///'):
                # Ищем значения enum'а
                value_match = re.search(r'(\w+)(?:\s*=\s*\d+)?', line)
                if value_match:
                    value = value_match.group(1)
                    if value not in ['enum', 'public', 'private', 'internal', 'Allow', 'Delete', 'Ban', 'Report', 'RequireManualReview']:
                        values.append(value)
        
        return values
    
    def _extract_namespace(self, content: str) -> str:
        """Извлекает namespace из содержимого файла"""
        namespace_match = re.search(r'namespace\s+([^;\s]+)', content)
        return namespace_match.group(1) if namespace_match else "ClubDoorman.Models"
    
    def _get_valid_value(self, prop: PropertyInfo) -> str:
        """Возвращает валидное значение для свойства"""
        prop_type = prop.type.lower()
        prop_name = prop.name.lower()
        
        if 'string' in prop_type:
            if 'name' in prop_name:
                return f'"{prop.name.capitalize()}"'
            elif 'title' in prop_name:
                return f'"{prop.name.capitalize()} Title"'
            elif 'reason' in prop_name:
                return f'"Test reason"'
            elif 'messages' in prop_name:
                return 'new List<string> { "First message", "Second message", "Third message" }'
            else:
                return f'"{prop.name}"'
        elif 'int' in prop_type:
            return "1"
        elif 'long' in prop_type:
            return "123456789"
        elif 'double' in prop_type or 'float' in prop_type:
            return "0.5"
        elif 'bool' in prop_type:
            return "false"
        elif 'datetime' in prop_type:
            return "DateTime.UtcNow"
        elif 'user' in prop_type.lower():
            return "TestDataFactory.CreateValidUser()"
        elif 'message' in prop_type.lower():
            return "TestDataFactory.CreateValidMessage()"
        elif 'chat' in prop_type.lower():
            return "TestDataFactory.CreateGroupChat()"
        elif 'cancellationtokensource' in prop_type.lower():
            return "new CancellationTokenSource()"
        elif 'list<string>' in prop_type.lower():
            return 'new List<string> { "First message", "Second message", "Third message" }'
        elif prop.type.endswith('?'):
            # Nullable тип
            return "null"
        elif prop.type == 'ModerationAction':
            return "ModerationAction.Allow"
        else:
            return f"new {prop.type}()"
    
    def _get_empty_value(self, prop: PropertyInfo) -> str:
        """Возвращает пустое значение для свойства"""
        prop_type = prop.type.lower()
        
        if 'string' in prop_type:
            return '""'
        elif prop.type.endswith('?'):
            return "null"
        else:
            return self._get_valid_value(prop)
    
    def _get_long_value(self, prop: PropertyInfo) -> str:
        """Возвращает длинное значение для свойства"""
        prop_type = prop.type.lower()
        
        if 'string' in prop_type:
            return 'new string(\'A\', 1000)'
        else:
            return self._get_valid_value(prop)
    
    def generate_test_data_factory(self) -> str:
        """Генерирует полный TestDataFactory"""
        
        # Собираем все модели
        all_models = []
        for model_file in self.models_dir.glob("*.cs"):
            models = self._parse_csharp_file(model_file)
            all_models.extend(models)
        
        # Генерируем код
        code = """using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Models;
using System;
using System.Threading;
using System.Collections.Generic;

namespace ClubDoorman.Test.TestData;

/// <summary>
/// Фабрика для создания тестовых данных
/// Автоматически сгенерировано
/// </summary>
public static class TestDataFactory
{
    #region Telegram Types

    public static Message CreateValidMessage()
    {
        return new Message
        {
            MessageId = 1,
            Date = DateTime.UtcNow,
            Text = "Hello, this is a valid message!",
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }

    public static Message CreateSpamMessage()
    {
        return new Message
        {
            MessageId = 2,
            Date = DateTime.UtcNow,
            Text = "BUY NOW!!! AMAZING OFFER!!! CLICK HERE!!!",
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }

    public static Message CreateEmptyMessage()
    {
        return new Message
        {
            MessageId = 3,
            Date = DateTime.UtcNow,
            Text = "",
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }

    public static User CreateValidUser()
    {
        return new User
        {
            Id = 123456789,
            IsBot = false,
            FirstName = "Test",
            LastName = "User",
            Username = "testuser"
        };
    }

    public static User CreateBotUser()
    {
        return new User
        {
            Id = 987654321,
            IsBot = true,
            FirstName = "TestBot",
            Username = "testbot"
        };
    }

    public static Chat CreateGroupChat()
    {
        return new Chat
        {
            Id = -1001234567890,
            Type = ChatType.Group,
            Title = "Test Group",
            Username = "testgroup"
        };
    }

    public static Chat CreateSupergroupChat()
    {
        return new Chat
        {
            Id = -1009876543210,
            Type = ChatType.Supergroup,
            Title = "Test Supergroup",
            Username = "testsupergroup"
        };
    }

    public static CallbackQuery CreateValidCallbackQuery()
    {
        return new CallbackQuery
        {
            Id = "test_callback_id",
            From = CreateValidUser(),
            Message = CreateValidMessage(),
            Data = "test_data"
        };
    }

    public static ChatMemberUpdated CreateMemberJoined()
    {
        return new ChatMemberUpdated
        {
            Chat = CreateGroupChat(),
            From = CreateValidUser(),
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMemberMember(),
            NewChatMember = new ChatMemberMember()
        };
    }

    public static Update CreateMessageUpdate()
    {
        return new Update
        {
            Message = CreateValidMessage()
        };
    }

    public static Update CreateCallbackQueryUpdate()
    {
        return new Update
        {
            CallbackQuery = CreateValidCallbackQuery()
        };
    }

    public static Update CreateChatMemberUpdate()
    {
        return new Update
        {
            ChatMember = CreateMemberJoined()
        };
    }

    #endregion

    #region Domain Models
"""
        
        # Добавляем методы для каждой модели
        for model in all_models:
            code += self._generate_test_data_methods(model)
        
        code += """
    #endregion
}
"""
        
        return code
    
    def _generate_test_data_methods(self, model: ModelInfo) -> str:
        """Генерирует методы для создания тестовых данных модели"""
        methods = []
        
        if model.is_enum:
            # Для enum'ов создаем методы для каждого значения
            for enum_value in model.enum_values:
                methods.append(f"""
    public static {model.name} Create{enum_value}{model.name}()
    {{
        return {model.name}.{enum_value};
    }}""")
        else:
            # Для record'ов создаем методы создания
            methods.append(f"""
    public static {model.name} CreateValid{model.name}()
    {{
        return new {model.name}(
{self._generate_constructor_parameters(model, "valid")}
        );
    }}""")
            
            # Создаем методы для edge cases
            if any(p.type == 'string' for p in model.properties):
                methods.append(f"""
    public static {model.name} CreateEmpty{model.name}()
    {{
        return new {model.name}(
{self._generate_constructor_parameters(model, "empty")}
        );
    }}""")
            
            if any(p.type == 'string' for p in model.properties):
                methods.append(f"""
    public static {model.name} CreateLong{model.name}()
    {{
        return new {model.name}(
{self._generate_constructor_parameters(model, "long")}
        );
    }}""")
        
        return "\n".join(methods)
    
    def _generate_constructor_parameters(self, model: ModelInfo, value_type: str) -> str:
        """Генерирует параметры конструктора"""
        param_lines = []
        
        for prop in model.properties:
            if value_type == "valid":
                value = self._get_valid_value(prop)
            elif value_type == "empty":
                value = self._get_empty_value(prop)
            elif value_type == "long":
                value = self._get_long_value(prop)
            else:
                value = self._get_valid_value(prop)
            
            param_lines.append(f"            {value}")
        
        return ",\n".join(param_lines)


def test_test_data_generator():
    """Тестирует генератор тестовых данных"""
    print("🔧 Тестирование TestData Generator...")
    
    # Создаем временную модель для тестирования
    models_dir = Path("../../ClubDoorman/Models")
    
    if not models_dir.exists():
        print(f"❌ Папка {models_dir} не найдена")
        return
    
    generator = TestDataGenerator(models_dir)
    
    # Генерируем TestDataFactory
    factory_code = generator.generate_test_data_factory()
    
    print("✅ TestDataFactory сгенерирован:")
    print("=" * 80)
    print(factory_code)
    print("=" * 80)
    
    # Сохраняем результат
    output_file = Path("../../ClubDoorman.Test/TestData/TestDataFactory.Generated.cs")
    output_file.parent.mkdir(parents=True, exist_ok=True)
    
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(factory_code)
    
    print(f"✅ Файл сохранен: {output_file}")


if __name__ == "__main__":
    test_test_data_generator() 