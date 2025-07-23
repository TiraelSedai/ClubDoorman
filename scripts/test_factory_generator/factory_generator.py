"""
Генератор TestFactory для C# классов
"""

import re
from pathlib import Path
from typing import List, Dict, Optional

try:
    from .models import ClassInfo
    from complexity_analyzer import ComplexityReport
except ImportError:
    from models import ClassInfo
    from complexity_analyzer import ComplexityReport


class TestFactoryGenerator:
    """Генератор TestFactory"""
    
    def __init__(self, test_project_root: Path, force_overwrite: bool = False):
        self.test_project_root = test_project_root
        # Проверяем, является ли test_project_root уже папкой TestInfrastructure
        if test_project_root.name == "TestInfrastructure":
            self.test_infrastructure_dir = test_project_root
        else:
            self.test_infrastructure_dir = test_project_root / "TestInfrastructure"
        self.force_overwrite = force_overwrite
        self.complexity_report: Optional[ComplexityReport] = None
        self.test_markers: List[str] = []
    
    def _create_concrete_instance(self, type_name: str, class_info: ClassInfo) -> str:
        """Создает строку для конкретного экземпляра типа"""
        # Специальные случаи для конкретных типов
        concrete_instances = {
            "BadMessageManager": "new BadMessageManager()",
            "GlobalStatsManager": "new GlobalStatsManager()",
            "SpamHamClassifier": "new SpamHamClassifier(new NullLogger<SpamHamClassifier>())",
            "MimicryClassifier": "new MimicryClassifier(new NullLogger<MimicryClassifier>())",
            "SuspiciousUsersStorage": "new SuspiciousUsersStorage(new NullLogger<SuspiciousUsersStorage>())",
            "AiChecks": 'new AiChecks(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"), new NullLogger<AiChecks>())',
            "IntroFlowService": 'new IntroFlowService(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"), new NullLogger<IntroFlowService>(), new Mock<ICaptchaService>().Object, new Mock<IUserManager>().Object, new AiChecks(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"), new NullLogger<AiChecks>()), new Mock<IStatisticsService>().Object, new GlobalStatsManager(), new Mock<IModerationService>().Object)'
        }
        
        # Убираем generic параметры для проверки
        base_type = re.sub(r'<[^>]+>', '', type_name)
        
        if base_type in concrete_instances:
            return f"            {concrete_instances[base_type]}"
        elif base_type == "TelegramBotClient":
            # Для TelegramBotClient используем реальный экземпляр с тестовым токеном
            return f'            new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz")'
        else:
            # Для неизвестных типов используем мок
            return f"            new Mock<{type_name}>().Object"
    
    def _get_required_usings(self, class_info: ClassInfo) -> List[str]:
        """Определяет необходимые using директивы"""
        usings = [
            f"using {class_info.namespace};",
            "using Microsoft.Extensions.Logging;",
            "using Microsoft.Extensions.Logging.Abstractions;",
            "using Moq;",
            "using NUnit.Framework;"
        ]
        
        # Добавляем специфичные using statements в зависимости от класса и параметров
        has_telegram = any('TelegramBotClient' in p.type for p in class_info.constructor_params)
        has_handlers = any('Handler' in p.type for p in class_info.constructor_params)
        has_services = any('Service' in p.type for p in class_info.constructor_params)
        has_models = any('Model' in p.type for p in class_info.constructor_params)
        
        if has_telegram or "Handler" in class_info.name:
            usings.append("using Telegram.Bot;")
        
        if has_handlers or "Handler" in class_info.name:
            usings.append("using ClubDoorman.Services;")
            usings.append("using ClubDoorman.Handlers;")
        
        if has_services or "Service" in class_info.name:
            usings.append("using ClubDoorman.Services;")
        
        if has_models or any('Model' in p.type for p in class_info.constructor_params):
            usings.append("using ClubDoorman.Models;")
        
        # Убираем дубликаты
        return list(dict.fromkeys(usings))
    
    def _to_pascal_case(self, name: str) -> str:
        """Конвертирует имя в PascalCase"""
        if not name:
            return name
        return name[0].upper() + name[1:]
    
    def set_complexity_analysis(self, complexity_report: ComplexityReport, test_markers: List[str]):
        """Устанавливает результаты анализа сложности"""
        self.complexity_report = complexity_report
        self.test_markers = test_markers
    
    def _should_mock_concrete_type(self, type_name: str) -> bool:
        """Определяет, нужно ли мокать конкретный тип"""
        if not self.complexity_report:
            return False
        
        base_type = re.sub(r'<[^>]+>', '', type_name)
        
        # Проверяем маркеры
        if 'TestRequiresConcreteMock' in self.test_markers:
            return True
        
        # Проверяем типы, которые всегда нужно мокать
        always_mock_types = {
            'AiChecks', 'SpamHamClassifier', 'MimicryClassifier',
            'BadMessageManager', 'GlobalStatsManager', 'SuspiciousUsersStorage'
        }
        
        return base_type in always_mock_types
    
    def _should_use_utility(self, type_name: str) -> bool:
        """Определяет, нужно ли использовать утилиту вместо мока"""
        if not self.complexity_report:
            return False
        
        base_type = re.sub(r'<[^>]+>', '', type_name)
        
        # Проверяем маркеры
        if 'TestRequiresUtility' in self.test_markers:
            return True
        
        # Проверяем типы, которые лучше использовать как утилиты
        utility_types = {
            'ITelegramBotClientWrapper': 'FakeTelegramClient',
            'ITelegramBotClient': 'FakeTelegramClient',
            'TelegramBotClient': 'FakeTelegramClient'  # Добавляем конкретный класс
        }
        
        return base_type in utility_types
    
    def _get_utility_type(self, type_name: str) -> str:
        """Получает тип утилиты для замены"""
        base_type = re.sub(r'<[^>]+>', '', type_name)
        
        utility_types = {
            'ITelegramBotClientWrapper': 'FakeTelegramClient',
            'ITelegramBotClient': 'FakeTelegramClient',
            'TelegramBotClient': 'FakeTelegramClient'  # Добавляем конкретный класс
        }
        
        return utility_types.get(base_type, type_name)
    
    def _generate_custom_constructor(self, class_info: ClassInfo) -> str:
        """Генерирует кастомный конструктор для сложной инициализации"""
        code = f"    public {class_info.name}TestFactory()\n"
        code += "    {\n"
        
        # Добавляем сложную инициализацию для конкретных классов
        for param in class_info.constructor_params:
            if param.is_concrete and self._should_mock_concrete_type(param.type):
                param_name_pascal = self._to_pascal_case(param.name)
                base_type = re.sub(r'<[^>]+>', '', param.type)
                
                if base_type == "AiChecks":
                    code += f"        // Создаем мок AiChecks с правильными параметрами конструктора\n"
                    code += f"        var mockLogger = new Mock<ILogger<{base_type}>>();\n"
                    code += f"        {param_name_pascal}Mock = new Mock<{param.type}>(new TelegramBotClient(\"1234567890:ABCdefGHIjklMNOpqrsTUVwxyz\"), mockLogger.Object);\n"
                elif base_type in ["SpamHamClassifier", "MimicryClassifier", "SuspiciousUsersStorage"]:
                    code += f"        // Создаем мок {base_type} с логгером\n"
                    code += f"        var mockLogger = new Mock<ILogger<{base_type}>>();\n"
                    code += f"        {param_name_pascal}Mock = new Mock<{param.type}>(mockLogger.Object);\n"
                else:
                    code += f"        // Создаем мок {base_type}\n"
                    code += f"        {param_name_pascal}Mock = new Mock<{param.type}>();\n"
        
        code += "    }\n\n"
        return code
    
    def _generate_additional_methods(self, class_info: ClassInfo) -> str:
        """Генерирует дополнительные методы для гибкости"""
        code = "\n"
        code += "    #region Additional Methods\n"
        code += "\n"
        
        # Метод с кастомным FakeTelegramClient
        if any(self._should_use_utility(param.type) for param in class_info.constructor_params):
            utility_param = next(param for param in class_info.constructor_params if self._should_use_utility(param.type))
            utility_type = self._get_utility_type(utility_param.type)
            param_name_pascal = self._to_pascal_case(utility_param.name)
            
            code += f"    public {class_info.name} Create{class_info.name}WithFake({utility_type}? {param_name_pascal} = null)\n"
            code += "    {\n"
            code += f"        var client = {param_name_pascal} ?? new {utility_type}();\n"
            code += "\n"
            code += f"        return new {class_info.name}(\n"
            
            # Параметры конструктора
            param_lines = []
            for param in class_info.constructor_params:
                if param.is_interface and not self._should_use_utility(param.type):
                    param_lines.append(f"            {self._to_pascal_case(param.name)}Mock.Object")
                elif param.is_interface and self._should_use_utility(param.type):
                    param_lines.append(f"            client")
                elif param.is_logger:
                    logger_type_match = re.search(r'ILogger<(\w+)>', param.type)
                    if logger_type_match:
                        logger_type = logger_type_match.group(1)
                        param_lines.append(f"            new NullLogger<{logger_type}>()")
                    else:
                        param_lines.append(f"            new NullLogger<{class_info.name}>()")
                elif param.is_concrete and self._should_mock_concrete_type(param.type):
                    param_lines.append(f"            {self._to_pascal_case(param.name)}Mock.Object")
                elif param.is_concrete:
                    param_lines.append(self._create_concrete_instance(param.type, class_info))
            
            code += ",\n".join(param_lines)
            code += "\n        );\n"
            code += "    }\n\n"
        
        code += "    #endregion\n"
        return code
    
    def generate_test_factory(self, class_info: ClassInfo) -> str:
        """Генерирует TestFactory для класса"""
        
        # Определяем какие параметры нужно мокать
        mock_params = [p for p in class_info.constructor_params if p.is_interface]
        concrete_params = [p for p in class_info.constructor_params if p.is_concrete]
        logger_params = [p for p in class_info.constructor_params if p.is_logger]
        
        # Генерируем код
        factory_name = f"{class_info.name}TestFactory"
        
        # Определяем необходимые using statements
        usings = self._get_required_usings(class_info)
        
        code = f"""{chr(10).join(usings)}

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
        
        # Добавляем моки для интерфейсов и конкретных типов, которые нужно мокать
        for param in class_info.constructor_params:
            if param.is_interface and not self._should_use_utility(param.type):
                code += f"    public Mock<{param.type}> {self._to_pascal_case(param.name)}Mock {{ get; }} = new();\n"
            elif param.is_concrete and self._should_mock_concrete_type(param.type):
                code += f"    public Mock<{param.type}> {self._to_pascal_case(param.name)}Mock {{ get; }} = new();\n"
        
        # Добавляем утилиты
        for param in class_info.constructor_params:
            if self._should_use_utility(param.type):
                utility_type = self._get_utility_type(param.type)
                code += f"    public {utility_type} {self._to_pascal_case(param.name)} {{ get; }} = new();\n"
        
        # Добавляем кастомный конструктор для сложной инициализации
        if self.complexity_report and self.complexity_report.complexity_score >= 7:
            code += self._generate_custom_constructor(class_info)
        
        code += "\n"
        
        # Метод Create
        code += f"    public {class_info.name} Create{class_info.name}()\n"
        code += "    {\n"
        code += f"        return new {class_info.name}(\n"
        
        # Параметры конструктора
        param_lines = []
        for param in class_info.constructor_params:
            if param.is_interface and not self._should_use_utility(param.type):
                param_lines.append(f"            {self._to_pascal_case(param.name)}Mock.Object")
            elif param.is_interface and self._should_use_utility(param.type):
                param_lines.append(f"            {self._to_pascal_case(param.name)}")
            elif param.is_logger:
                # Извлекаем тип логгера из generic параметра
                logger_type_match = re.search(r'ILogger<(\w+)>', param.type)
                if logger_type_match:
                    logger_type = logger_type_match.group(1)
                    param_lines.append(f"            new NullLogger<{logger_type}>()")
                else:
                    param_lines.append(f"            new NullLogger<{class_info.name}>()")
            elif param.is_concrete and self._should_mock_concrete_type(param.type):
                param_lines.append(f"            {self._to_pascal_case(param.name)}Mock.Object")
            elif param.is_concrete:
                # Для конкретных классов создаем реальные экземпляры
                param_lines.append(self._create_concrete_instance(param.type, class_info))
        
        code += ",\n".join(param_lines)
        code += "\n        );\n"
        code += "    }\n"
        
        # Добавляем вспомогательные методы для настройки
        code += "\n"
        code += "    #region Configuration Methods\n"
        code += "\n"
        
        for param in class_info.constructor_params:
            if (param.is_interface and not self._should_use_utility(param.type)) or \
               (param.is_concrete and self._should_mock_concrete_type(param.type)):
                param_name_pascal = self._to_pascal_case(param.name)
                code += f"    public {factory_name} With{param_name_pascal}Setup(Action<Mock<{param.type}>> setup)\n"
                code += "    {\n"
                code += f"        setup({param_name_pascal}Mock);\n"
                code += "        return this;\n"
                code += "    }\n\n"
        
        code += "    #endregion\n"
        
        # Добавляем дополнительные методы для гибкости
        if self.complexity_report and self.complexity_report.complexity_score >= 7:
            code += self._generate_additional_methods(class_info)
        
        code += "}\n"
        
        return code
    
    def generate_test_factory_tests(self, class_info: ClassInfo) -> str:
        """Генерирует тесты для TestFactory"""
        
        factory_name = f"{class_info.name}TestFactory"
        
        # Определяем необходимые using statements для тестов
        usings = self._get_required_usings(class_info)
        
        code = f"""{chr(10).join(usings)}

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для {factory_name}
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class {factory_name}Tests
{{
    [Test]
    public void Create{class_info.name}_ReturnsWorkingInstance()
    {{
        // Arrange
        var factory = new {factory_name}();

        // Act
        var instance = factory.Create{class_info.name}();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<{class_info.name}>());
    }}

    [Test]
    public void Create{class_info.name}_ConfiguresAllDependencies()
    {{
        // Arrange
        var factory = new {factory_name}();

        // Act
        var instance = factory.Create{class_info.name}();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }}

    [Test]
    public void Create{class_info.name}_CreatesFreshInstanceEachTime()
    {{
        // Arrange
        var factory = new {factory_name}();

        // Act
        var instance1 = factory.Create{class_info.name}();
        var instance2 = factory.Create{class_info.name}();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }}
"""
        
        # Добавляем тесты для моков
        mock_params = [p for p in class_info.constructor_params if p.is_interface]
        for param in mock_params:
            param_name_pascal = self._to_pascal_case(param.name)
            code += f"""
    [Test]
    public void {param_name_pascal}Mock_IsProperlyConfigured()
    {{
        // Arrange
        var factory = new {factory_name}();

        // Act & Assert
        Assert.That(factory.{param_name_pascal}Mock, Is.Not.Null);
        Assert.That(factory.{param_name_pascal}Mock.Object, Is.Not.Null);
    }}
"""
        
        code += "}\n"
        return code
    
    def save_files(self, class_info: ClassInfo, factory_code: str, tests_code: str):
        """Сохраняет сгенерированные файлы"""
        
        # Создаем папку TestInfrastructure если её нет
        self.test_infrastructure_dir.mkdir(exist_ok=True)
        
        # Сохраняем TestFactory
        factory_file = self.test_infrastructure_dir / f"{class_info.name}TestFactory.cs"
        
        # Проверяем существование файла
        if factory_file.exists() and not self.force_overwrite:
            print(f"⚠️  Файл {factory_file} уже существует. Используйте --force для перезаписи.")
            return False
        
        factory_file.write_text(factory_code, encoding='utf-8')
        print(f"✅ Создан: {factory_file}")
        
        # Сохраняем тесты для TestFactory
        tests_file = self.test_infrastructure_dir / f"{class_info.name}TestFactoryTests.cs"
        
        # Проверяем существование файла
        if tests_file.exists() and not self.force_overwrite:
            print(f"⚠️  Файл {tests_file} уже существует. Используйте --force для перезаписи.")
            return False
        
        tests_file.write_text(tests_code, encoding='utf-8')
        print(f"✅ Создан: {tests_file}")
        
        return True 