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
        if prop.type == 'string':
            if prop.name.lower() in ['text', 'message', 'content']:
                return '"Hello, this is a valid message!"'
            elif prop.name.lower() in ['username', 'name']:
                return '"testuser"'
            elif prop.name.lower() in ['title']:
                return '"Test Group"'
            elif prop.name.lower() in ['data']:
                return '"test_data"'
            elif prop.name.lower() in ['id', 'callback_id']:
                return '"test_callback_id"'
            else:
                return '"test_value"'
        elif prop.type == 'int':
            if prop.name.lower() in ['id', 'messageid', 'chatid', 'userid']:
                return '123456789'
            elif prop.name.lower() in ['date']:
                return 'DateTime.UtcNow'
            else:
                return '42'
        elif prop.type == 'long':
            if prop.name.lower() in ['id', 'messageid', 'chatid', 'userid']:
                return '123456789L'
            else:
                return '42L'
        elif prop.type == 'double':
            if prop.name.lower() in ['score', 'mimicryscore']:
                return '0.85'
            else:
                return '3.14'
        elif prop.type == 'float':
            return '3.14f'
        elif prop.type == 'bool':
            if prop.name.lower() in ['isbot', 'is_bot']:
                return 'false'
            elif prop.name.lower() in ['isapproved', 'is_approved']:
                return 'true'
            else:
                return 'true'
        elif prop.type == 'DateTime':
            return 'DateTime.UtcNow'
        elif prop.type == 'User':
            return 'CreateValidUser()'
        elif prop.type == 'Message':
            return 'CreateValidMessage()'
        elif prop.type == 'Chat':
            return 'CreateGroupChat()'
        elif prop.type == 'Update':
            return 'CreateMessageUpdate()'
        elif prop.type == 'CallbackQuery':
            return 'CreateValidCallbackQuery()'
        elif prop.type == 'ChatMemberUpdated':
            return 'CreateMemberJoined()'
        elif prop.type == 'ModerationResult':
            return 'CreateValidModerationResult()'
        elif prop.type == 'SuspiciousUserInfo':
            return 'CreateValidSuspiciousUserInfo()'
        elif prop.type == 'CancellationTokenSource':
            return 'new CancellationTokenSource()'
        elif prop.type == 'List<string>':
            return 'new List<string> { "test1", "test2" }'
        elif prop.type == 'ChatType':
            return 'ChatType.Group'
        elif prop.type == 'ModerationAction':
            return 'ModerationAction.Allow'
        elif prop.is_nullable:
            return 'null'
        else:
            return f'new {prop.type}()'
    
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
        
        # Добавляем дополнительные методы
        code += self._generate_additional_methods()
        
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
            # Для обычных классов создаем методы через object initializer
            if model.name in ['Message', 'User', 'Chat', 'CallbackQuery', 'ChatMemberUpdated']:
                methods.append(f"""
    public static {model.name} CreateValid{model.name}()
    {{
        return new {model.name}
        {{
{self._generate_object_initializer(model, "valid")}
        }};
    }}""")
                
                # Создаем методы для edge cases
                if any(p.type == 'string' for p in model.properties):
                    methods.append(f"""
    public static {model.name} CreateEmpty{model.name}()
    {{
        return new {model.name}
        {{
{self._generate_object_initializer(model, "empty")}
        }};
    }}""")
                
                if any(p.type == 'string' for p in model.properties):
                    methods.append(f"""
    public static {model.name} CreateLong{model.name}()
    {{
        return new {model.name}
        {{
{self._generate_object_initializer(model, "long")}
        }};
    }}""")
            else:
                # Для record'ов создаем методы через конструктор
                methods.append(f"""
    public static {model.name} CreateValid{model.name}()
    {{
        return new {model.name}(
{self._generate_constructor_parameters(model, "valid")}
        );
    }}""")
        
        return "\n".join(methods)
    
    def _generate_additional_methods(self) -> str:
        """Генерирует дополнительные методы для TestDataFactory"""
        return """
    // Дополнительные методы для совместимости с существующими тестами
    public static Message CreateNullTextMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Text = null,
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }
    
    public static Message CreateLongMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Text = "This is a very long message that contains a lot of text and should be considered as a long message for testing purposes. " + 
                   "It has multiple sentences and should trigger any logic that handles long messages. " +
                   "The message continues with more content to ensure it's properly classified as long.",
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }
    
    public static ModerationResult CreateAllowResult()
    {
        return new ModerationResult(ModerationAction.Allow, "Message allowed");
    }
    
    public static ModerationResult CreateDeleteResult()
    {
        return new ModerationResult(ModerationAction.Delete, "Message deleted");
    }
    
    public static ModerationResult CreateBanResult()
    {
        return new ModerationResult(ModerationAction.Ban, "User banned");
    }
    
    public static CaptchaInfo CreateExpiredCaptchaInfo()
    {
        return new CaptchaInfo(
            123456789L,
            "expired-chat",
            DateTime.UtcNow.AddHours(-2), // Expired 2 hours ago
            CreateValidUser(),
            3,
            new CancellationTokenSource(),
            CreateValidMessage()
        );
    }
    
    public static ChatMemberUpdated CreateMemberLeft()
    {
        return new ChatMemberUpdated
        {
            Chat = CreateGroupChat(),
            From = CreateValidUser(),
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMemberMember(),
            NewChatMember = new ChatMemberLeft()
        };
    }
    
    public static ChatMemberUpdated CreateMemberBanned()
    {
        return new ChatMemberUpdated
        {
            Chat = CreateGroupChat(),
            From = CreateValidUser(),
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMemberMember(),
            NewChatMember = new ChatMemberBanned()
        };
    }
    
    public static ChatMemberUpdated CreateMemberRestricted()
    {
        return new ChatMemberUpdated
        {
            Chat = CreateGroupChat(),
            From = CreateValidUser(),
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMemberMember(),
            NewChatMember = new ChatMemberRestricted()
        };
    }
    
    public static ChatMemberUpdated CreateMemberPromoted()
    {
        return new ChatMemberUpdated
        {
            Chat = CreateGroupChat(),
            From = CreateValidUser(),
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMemberMember(),
            NewChatMember = new ChatMemberAdministrator()
        };
    }
    
    public static ChatMemberUpdated CreateMemberDemoted()
    {
        return new ChatMemberUpdated
        {
            Chat = CreateGroupChat(),
            From = CreateValidUser(),
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMemberAdministrator(),
            NewChatMember = new ChatMemberMember()
        };
    }
    
    public static CallbackQuery CreateInvalidCallbackQuery()
    {
        return new CallbackQuery
        {
            Id = "invalid_callback_id",
            From = CreateValidUser(),
            Message = null,
            Data = null
        };
    }
    
    public static Chat CreatePrivateChat()
    {
        return new Chat
        {
            Id = 123456789,
            Type = ChatType.Private,
            Title = "Private Chat",
            Username = "privateuser"
        };
    }
    
    public static User CreateAnonymousUser()
    {
        return new User
        {
            Id = 111111111,
            IsBot = false,
            FirstName = "Anonymous",
            LastName = null,
            Username = null
        };
    }"""
    
    def _generate_object_initializer(self, model: ModelInfo, value_type: str) -> str:
        """Генерирует object initializer для класса"""
        prop_lines = []
        
        for prop in model.properties:
            # Пропускаем readonly свойства
            if prop.name in ['MessageId']:
                continue
                
            if value_type == "valid":
                value = self._get_valid_value(prop)
            elif value_type == "empty":
                value = self._get_empty_value(prop)
            elif value_type == "long":
                value = self._get_long_value(prop)
            else:
                value = self._get_valid_value(prop)
            
            prop_lines.append(f"            {prop.name} = {value},")
        
        return "\n".join(prop_lines)
    
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