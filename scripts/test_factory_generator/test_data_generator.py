"""
Генератор TestDataFactory для доменных моделей
"""

import re
from pathlib import Path
from typing import List, Dict, Any
from dataclasses import dataclass


@dataclass
class PropertyInfo:
    """Информация о свойстве модели"""
    name: str
    type: str
    is_readonly: bool = False
    is_required: bool = True
    default_value: str = None


@dataclass
class ModelInfo:
    """Информация о доменной модели"""
    name: str
    namespace: str
    properties: List[PropertyInfo]
    is_record: bool = False
    is_enum: bool = False
    enum_values: List[str] = None


class TestDataGenerator:
    """Генератор TestDataFactory для доменных моделей"""
    
    def __init__(self, project_root: Path, test_project_root: Path):
        self.project_root = Path(project_root)
        self.test_project_root = Path(test_project_root)
        self.models_dir = self.project_root / "ClubDoorman" / "Models"
        self.test_data_dir = self.test_project_root / "TestData"
    
    def _parse_csharp_file(self, file_path: Path) -> List[ModelInfo]:
        """Парсит C# файл и извлекает информацию о моделях"""
        models = []
        
        if not file_path.exists():
            return models
        
        content = file_path.read_text(encoding='utf-8')
        
        # Ищем namespace
        namespace_match = re.search(r'namespace\s+(\S+);', content)
        namespace = namespace_match.group(1) if namespace_match else "ClubDoorman.Models"
        
        # Ищем record классы
        record_pattern = r'public\s+(?:sealed\s+)?record\s+(\w+)\s*\(([^)]*)\)'
        for match in re.finditer(record_pattern, content, re.MULTILINE | re.DOTALL):
            model_name = match.group(1)
            params_str = match.group(2)
            
            properties = self._parse_record_parameters(params_str)
            models.append(ModelInfo(
                name=model_name,
                namespace=namespace,
                properties=properties,
                is_record=True
            ))
        
        # Ищем enum'ы
        enum_pattern = r'public\s+enum\s+(\w+)\s*{([^}]*)}'
        for match in re.finditer(enum_pattern, content, re.MULTILINE | re.DOTALL):
            enum_name = match.group(1)
            enum_content = match.group(2)
            
            enum_values = []
            for line in enum_content.split('\n'):
                line = line.strip()
                if line and not line.startswith('///'):
                    # Извлекаем имя enum значения
                    value_match = re.search(r'(\w+)', line)
                    if value_match:
                        enum_values.append(value_match.group(1))
            
            models.append(ModelInfo(
                name=enum_name,
                namespace=namespace,
                properties=[],
                is_enum=True,
                enum_values=enum_values
            ))
        
        return models
    
    def _parse_record_parameters(self, params_str: str) -> List[PropertyInfo]:
        """Парсит параметры record конструктора"""
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
                default_value = param_match.group(3) if param_match.group(3) else None
                
                # Определяем, является ли свойство обязательным
                is_required = default_value is None and not prop_type.endswith('?')
                
                properties.append(PropertyInfo(
                    name=prop_name,
                    type=prop_type,
                    is_required=is_required,
                    default_value=default_value
                ))
        
        return properties
    
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
    
    def _generate_constructor_parameters(self, model: ModelInfo, variant: str) -> str:
        """Генерирует параметры конструктора для разных вариантов"""
        params = []
        
        for prop in model.properties:
            if variant == "valid":
                value = self._get_valid_value(prop)
            elif variant == "empty":
                value = self._get_empty_value(prop)
            elif variant == "long":
                value = self._get_long_value(prop)
            else:
                value = self._get_valid_value(prop)
            
            params.append(f"            {value}")
        
        return ",\n".join(params)
    
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
        elif 'double' in prop_type:
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
        
        # Добавляем методы для доменных моделей
        for model in all_models:
            code += self._generate_test_data_methods(model)
        
        code += """
    #endregion
}
"""
        
        return code
    
    def save_test_data_factory(self, force_overwrite: bool = False):
        """Сохраняет TestDataFactory в файл"""
        factory_code = self.generate_test_data_factory()
        
        # Создаем директорию если не существует
        self.test_data_dir.mkdir(parents=True, exist_ok=True)
        
        file_path = self.test_data_dir / "TestDataFactory.cs"
        
        if file_path.exists() and not force_overwrite:
            print(f"⚠️  Файл {file_path} уже существует. Используйте --force для перезаписи.")
            return False
        
        file_path.write_text(factory_code, encoding='utf-8')
        print(f"✅ Создан: {file_path}")
        return True 