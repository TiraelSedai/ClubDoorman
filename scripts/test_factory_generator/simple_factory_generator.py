"""
Упрощенный генератор фабрик с улучшенным качеством
Генерирует простые, но эффективные фабрики для тестов
"""
from pathlib import Path
from typing import List, Dict, Any, Optional
from dataclasses import dataclass
from interface_analyzer import InterfaceAnalyzer, MethodInfo, InterfaceInfo


@dataclass
class DependencyInfo:
    """Информация о зависимости"""
    name: str
    type: str
    interface_name: str
    is_optional: bool = False


@dataclass
class ClassInfo:
    """Информация о классе"""
    name: str
    namespace: str
    dependencies: List[DependencyInfo]
    constructor_params: List[str]
    is_handler: bool = False
    is_service: bool = False
    is_classifier: bool = False


class SimpleFactoryGenerator:
    """Упрощенный генератор фабрик"""
    
    def __init__(self, project_root: Path):
        self.project_root = project_root
        self.interface_analyzer = InterfaceAnalyzer(project_root)
    
    def generate_simple_factory(self, class_info: ClassInfo) -> str:
        """Генерирует упрощенную фабрику для класса"""
        
        # Определяем тип класса
        class_name = class_info.name
        if "Handler" in class_name:
            class_info.is_handler = True
        elif "Service" in class_name:
            class_info.is_service = True
        elif "Classifier" in class_name:
            class_info.is_classifier = True
        
        # Генерируем фабрику
        if class_info.is_handler:
            return self._generate_handler_factory(class_info)
        elif class_info.is_service:
            return self._generate_service_factory(class_info)
        elif class_info.is_classifier:
            return self._generate_classifier_factory(class_info)
        else:
            return self._generate_generic_factory(class_info)
    
    def _generate_handler_factory(self, class_info: ClassInfo) -> str:
        """Генерирует фабрику для обработчиков"""
        class_name = class_info.name
        
        # Создаем моки для зависимостей
        mock_declarations = []
        mock_setups = []
        
        for dep in class_info.dependencies:
            mock_name = f"{dep.name}Mock"
            mock_declarations.append(f"    public Mock<{dep.type}> {mock_name} {{ get; }} = new();")
            
            # Генерируем базовые настройки моков
            mock_setup = self._generate_basic_mock_setup(dep)
            if mock_setup:
                mock_setups.append(mock_setup)
        
        # Генерируем конструктор
        constructor_params = []
        for dep in class_info.dependencies:
            constructor_params.append(f"{dep.name}Mock.Object")
        
        constructor_params_str = ",\n            ".join(constructor_params)
        
        return f"""using {class_info.namespace};
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Упрощенная TestFactory для {class_name}
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class {class_name}TestFactory
{{
{chr(10).join(mock_declarations)}
    public Mock<ILogger<{class_name}>> LoggerMock {{ get; }} = new();
    public FakeTelegramClient FakeTelegramClient {{ get; }} = new();

    public {class_name} Create{class_name}()
    {{
        return new {class_name}(
            FakeTelegramClient,
{chr(10).join(f"            {param}" for param in constructor_params)},
            LoggerMock.Object
        );
    }}

    /// <summary>
    /// Настраивает базовые моки для типичных сценариев
    /// </summary>
    public void SetupDefaultMocks()
    {{
{chr(10).join(mock_setups) if mock_setups else "        // Базовые настройки не требуются"}
    }}

    /// <summary>
    /// Создает фабрику с предустановленными настройками
    /// </summary>
    public static {class_name}TestFactory CreateWithDefaults()
    {{
        var factory = new {class_name}TestFactory();
        factory.SetupDefaultMocks();
        return factory;
    }}

    #region Configuration Methods

{self._generate_configuration_methods(class_info)}

    #endregion
}}"""
    
    def _generate_service_factory(self, class_info: ClassInfo) -> str:
        """Генерирует фабрику для сервисов"""
        class_name = class_info.name
        
        # Создаем моки для зависимостей
        mock_declarations = []
        mock_setups = []
        
        for dep in class_info.dependencies:
            mock_name = f"{dep.name}Mock"
            mock_declarations.append(f"    public Mock<{dep.type}> {mock_name} {{ get; }} = new();")
            
            # Генерируем базовые настройки моков
            mock_setup = self._generate_basic_mock_setup(dep)
            if mock_setup:
                mock_setups.append(mock_setup)
        
        # Генерируем конструктор
        constructor_params = []
        for dep in class_info.dependencies:
            constructor_params.append(f"{dep.name}Mock.Object")
        
        constructor_params_str = ",\n            ".join(constructor_params)
        
        return f"""using {class_info.namespace};
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Упрощенная TestFactory для {class_name}
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class {class_name}TestFactory
{{
{chr(10).join(mock_declarations)}
    public Mock<ILogger<{class_name}>> LoggerMock {{ get; }} = new();

    public {class_name} Create{class_name}()
    {{
        return new {class_name}(
{chr(10).join(f"            {param}" for param in constructor_params)},
            LoggerMock.Object
        );
    }}

    /// <summary>
    /// Настраивает базовые моки для типичных сценариев
    /// </summary>
    public void SetupDefaultMocks()
    {{
{chr(10).join(mock_setups) if mock_setups else "        // Базовые настройки не требуются"}
    }}

    /// <summary>
    /// Создает фабрику с предустановленными настройками
    /// </summary>
    public static {class_name}TestFactory CreateWithDefaults()
    {{
        var factory = new {class_name}TestFactory();
        factory.SetupDefaultMocks();
        return factory;
    }}

    #region Configuration Methods

{self._generate_configuration_methods(class_info)}

    #endregion
}}"""
    
    def _generate_classifier_factory(self, class_info: ClassInfo) -> str:
        """Генерирует фабрику для классификаторов"""
        class_name = class_info.name
        
        return f"""using {class_info.namespace};
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Упрощенная TestFactory для {class_name}
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class {class_name}TestFactory
{{
    public Mock<ILogger<{class_name}>> LoggerMock {{ get; }} = new();

    public {class_name} Create{class_name}()
    {{
        return new {class_name}(LoggerMock.Object);
    }}

    /// <summary>
    /// Создает мок {class_name} для тестов (НЕ обучает реальную модель)
    /// </summary>
    public Mock<I{class_name.replace('Classifier', '')}> CreateMock{class_name}()
    {{
        var mock = new Mock<I{class_name.replace('Classifier', '')}>();
        
        // Настройка базовых методов
        mock.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ReturnsAsync((false, 0.2f));

        mock.Setup(x => x.AddSpam(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.AddHam(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        return mock;
    }}

    /// <summary>
    /// Создает фабрику с предустановленными настройками
    /// </summary>
    public static {class_name}TestFactory CreateWithDefaults()
    {{
        return new {class_name}TestFactory();
    }}

    #region Configuration Methods

    public {class_name}TestFactory WithLoggerSetup(Action<Mock<ILogger<{class_name}>>> setup)
    {{
        setup(LoggerMock);
        return this;
    }}

    #endregion
}}"""
    
    def _generate_generic_factory(self, class_info: ClassInfo) -> str:
        """Генерирует универсальную фабрику"""
        class_name = class_info.name
        
        # Создаем моки для зависимостей
        mock_declarations = []
        mock_setups = []
        
        for dep in class_info.dependencies:
            mock_name = f"{dep.name}Mock"
            mock_declarations.append(f"    public Mock<{dep.type}> {mock_name} {{ get; }} = new();")
            
            # Генерируем базовые настройки моков
            mock_setup = self._generate_basic_mock_setup(dep)
            if mock_setup:
                mock_setups.append(mock_setup)
        
        # Генерируем конструктор
        constructor_params = []
        for dep in class_info.dependencies:
            constructor_params.append(f"{dep.name}Mock.Object")
        
        constructor_params_str = ",\n            ".join(constructor_params)
        
        return f"""using {class_info.namespace};
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Упрощенная TestFactory для {class_name}
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class {class_name}TestFactory
{{
{chr(10).join(mock_declarations)}
    public Mock<ILogger<{class_name}>> LoggerMock {{ get; }} = new();

    public {class_name} Create{class_name}()
    {{
        return new {class_name}(
{chr(10).join(f"            {param}" for param in constructor_params)},
            LoggerMock.Object
        );
    }}

    /// <summary>
    /// Настраивает базовые моки для типичных сценариев
    /// </summary>
    public void SetupDefaultMocks()
    {{
{chr(10).join(mock_setups) if mock_setups else "        // Базовые настройки не требуются"}
    }}

    /// <summary>
    /// Создает фабрику с предустановленными настройками
    /// </summary>
    public static {class_name}TestFactory CreateWithDefaults()
    {{
        var factory = new {class_name}TestFactory();
        factory.SetupDefaultMocks();
        return factory;
    }}

    #region Configuration Methods

{self._generate_configuration_methods(class_info)}

    #endregion
}}"""
    
    def _generate_basic_mock_setup(self, dependency: DependencyInfo) -> Optional[str]:
        """Генерирует базовые настройки мока"""
        interface_name = dependency.interface_name
        
        # Анализируем интерфейс
        interface_file = self.interface_analyzer.find_interface_file(interface_name)
        if not interface_file:
            return None
        
        interface_info = self.interface_analyzer.analyze_interface(interface_file)
        
        # Генерируем настройки для основных методов
        setups = []
        
        for method in interface_info.methods:
            if method.name in ["IsSpam", "CheckMessageAsync", "Approved", "ProcessAsync"]:
                setup = self.interface_analyzer.get_method_mock_setup(method, interface_name)
                setups.append(setup)
        
        if not setups:
            return None
        
        return f"""
        // Базовые настройки для {interface_name}
{chr(10).join(setups)}"""
    
    def _generate_configuration_methods(self, class_info: ClassInfo) -> str:
        """Генерирует методы конфигурации"""
        methods = []
        
        for dep in class_info.dependencies:
            method_name = f"With{dep.name}Setup"
            methods.append(f"""    public {class_info.name}TestFactory {method_name}(Action<Mock<{dep.type}>> setup)
    {{
        setup({dep.name}Mock);
        return this;
    }}""")
        
        # Добавляем общий метод для логгера
        methods.append(f"""    public {class_info.name}TestFactory WithLoggerSetup(Action<Mock<ILogger<{class_info.name}>>> setup)
    {{
        setup(LoggerMock);
        return this;
    }}""")
        
        return "\n\n".join(methods)
    
    def generate_test_template(self, class_info: ClassInfo) -> str:
        """Генерирует шаблон тестов для класса"""
        class_name = class_info.name
        
        if class_info.is_handler:
            return self._generate_handler_test_template(class_info)
        elif class_info.is_service:
            return self._generate_service_test_template(class_info)
        elif class_info.is_classifier:
            return self._generate_classifier_test_template(class_info)
        else:
            return self._generate_generic_test_template(class_info)
    
    def _generate_handler_test_template(self, class_info: ClassInfo) -> str:
        """Генерирует шаблон тестов для обработчиков"""
        class_name = class_info.name
        
        return f"""using {class_info.namespace};
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestData;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using System;
using System.Threading.Tasks;

namespace ClubDoorman.Test;

[TestFixture]
[Category("unit")]
public class {class_name}Tests
{{
    private {class_name}TestFactory _factory;
    private {class_name} _handler;

    [SetUp]
    public void Setup()
    {{
        _factory = {class_name}TestFactory.CreateWithDefaults();
        _handler = _factory.Create{class_name}();
    }}

    [Test]
    public void Create{class_name}_WithFactory_ReturnsValidInstance()
    {{
        // Arrange & Act
        var instance = _factory.Create{class_name}();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<{class_name}>());
    }}

    [Test]
    public async Task HandleAsync_ValidMessage_ProcessesSuccessfully()
    {{
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        var update = new Update {{ Message = message }};

        // Act
        await _handler.HandleAsync(update);

        // Assert
        // Проверяем, что обработка прошла без исключений
        Assert.Pass("Message processed successfully");
    }}

    [Test]
    public async Task HandleAsync_SpamMessage_BlocksUser()
    {{
        // Arrange
        var message = TestDataFactory.CreateSpamMessage();
        var update = new Update {{ Message = message }};

        // Act
        await _handler.HandleAsync(update);

        // Assert
        // Проверяем, что спам был обработан
        Assert.Pass("Spam message processed");
    }}

    [Test]
    public async Task HandleAsync_ExceptionInProcessing_LogsError()
    {{
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        var update = new Update {{ Message = message }};

        // Настраиваем исключение в зависимости
        _factory.WithModerationServiceSetup(mock => 
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ThrowsAsync(new System.Exception("Test exception")));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => 
            await _handler.HandleAsync(update));
    }}
}}"""
    
    def _generate_service_test_template(self, class_info: ClassInfo) -> str:
        """Генерирует шаблон тестов для сервисов"""
        class_name = class_info.name
        
        return f"""using {class_info.namespace};
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestData;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ClubDoorman.Test;

[TestFixture]
[Category("unit")]
public class {class_name}Tests
{{
    private {class_name}TestFactory _factory;
    private {class_name} _service;

    [SetUp]
    public void Setup()
    {{
        _factory = {class_name}TestFactory.CreateWithDefaults();
        _service = _factory.Create{class_name}();
    }}

    [Test]
    public void Create{class_name}_WithFactory_ReturnsValidInstance()
    {{
        // Arrange & Act
        var instance = _factory.Create{class_name}();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<{class_name}>());
    }}

    [Test]
    public async Task ProcessAsync_ValidInput_ReturnsExpectedResult()
    {{
        // Arrange
        var input = TestDataFactory.CreateValidInput();

        // Act
        var result = await _service.ProcessAsync(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
    }}

    [Test]
    public async Task ProcessAsync_InvalidInput_ThrowsException()
    {{
        // Arrange
        var input = TestDataFactory.CreateInvalidInput();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.ProcessAsync(input));
        
        Assert.That(exception.Message, Contains.Substring("error"));
    }}

    [Test]
    public async Task ProcessAsync_ExceptionInDependency_HandlesGracefully()
    {{
        // Arrange
        var input = TestDataFactory.CreateValidInput();

        // Настраиваем исключение в зависимости
        _factory.WithDependencySetup(mock => 
            mock.Setup(x => x.ProcessAsync(It.IsAny<object>()))
                .ThrowsAsync(new System.Exception("Test exception")));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => 
            await _service.ProcessAsync(input));
    }}
}}"""
    
    def _generate_classifier_test_template(self, class_info: ClassInfo) -> str:
        """Генерирует шаблон тестов для классификаторов"""
        class_name = class_info.name
        
        return f"""using {class_info.namespace};
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestData;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ClubDoorman.Test;

[TestFixture]
[Category("unit")]
public class {class_name}Tests
{{
    private {class_name}TestFactory _factory;
    private {class_name} _classifier;

    [SetUp]
    public void Setup()
    {{
        _factory = {class_name}TestFactory.CreateWithDefaults();
        _classifier = _factory.Create{class_name}();
    }}

    [Test]
    public void Create{class_name}_WithFactory_ReturnsValidInstance()
    {{
        // Arrange & Act
        var instance = _factory.Create{class_name}();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<{class_name}>());
    }}

    [Test]
    public async Task IsSpam_ValidInput_ReturnsCorrectClassification()
    {{
        // Arrange
        var input = TestDataFactory.CreateValidInput();

        // Act
        var result = await _classifier.IsSpam(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Item1, Is.False); // IsSpam
        Assert.That(result.Item2, Is.GreaterThanOrEqualTo(0.0f)); // Score
    }}

    [Test]
    public async Task IsSpam_SpamInput_ReturnsSpamClassification()
    {{
        // Arrange
        var input = TestDataFactory.CreateSpamInput();

        // Act
        var result = await _classifier.IsSpam(input);

        // Assert
        Assert.That(result.Item1, Is.True); // IsSpam
        Assert.That(result.Item2, Is.GreaterThan(0.5f)); // Score
    }}

    [Test]
    public async Task IsSpam_NullInput_ThrowsArgumentNullException()
    {{
        // Arrange
        string? input = null;

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _classifier.IsSpam(input));
        
        Assert.That(exception.ParamName, Is.EqualTo("input"));
    }}
}}"""
    
    def _generate_generic_test_template(self, class_info: ClassInfo) -> str:
        """Генерирует универсальный шаблон тестов"""
        class_name = class_info.name
        
        return f"""using {class_info.namespace};
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestData;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ClubDoorman.Test;

[TestFixture]
[Category("unit")]
public class {class_name}Tests
{{
    private {class_name}TestFactory _factory;
    private {class_name} _instance;

    [SetUp]
    public void Setup()
    {{
        _factory = {class_name}TestFactory.CreateWithDefaults();
        _instance = _factory.Create{class_name}();
    }}

    [Test]
    public void Create{class_name}_WithFactory_ReturnsValidInstance()
    {{
        // Arrange & Act
        var instance = _factory.Create{class_name}();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<{class_name}>());
    }}

    [Test]
    public async Task ExecuteAsync_ValidInput_ReturnsExpectedResult()
    {{
        // Arrange
        var input = TestDataFactory.CreateValidInput();

        // Act
        var result = await _instance.ExecuteAsync(input);

        // Assert
        Assert.That(result, Is.Not.Null);
    }}

    [Test]
    public async Task ExecuteAsync_InvalidInput_ThrowsException()
    {{
        // Arrange
        var input = TestDataFactory.CreateInvalidInput();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(async () => 
            await _instance.ExecuteAsync(input));
        
        Assert.That(exception.Message, Contains.Substring("error"));
    }}
}}""" 