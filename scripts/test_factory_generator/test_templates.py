"""
Генератор шаблонов тестов для ClubDoorman
"""
from typing import List, Dict, Any
from dataclasses import dataclass
from pathlib import Path
import re


@dataclass
class TestTemplate:
    """Шаблон теста"""
    name: str
    template: str
    description: str


class TestTemplateGenerator:
    """Генератор шаблонов тестов"""
    
    def __init__(self):
        self.templates = self._create_templates()
    
    def _create_templates(self) -> Dict[str, TestTemplate]:
        """Создает базовые шаблоны тестов"""
        return {
            "service": TestTemplate(
                name="service",
                description="Базовый шаблон для сервисов",
                template="""
[Test]
public async Task {MethodName}_ValidInput_ReturnsExpectedResult()
{{
    // Arrange
    var input = TestDataFactory.CreateValid{InputType}();
    
    {MockSetup}

    // Act
    var result = await _service.{MethodName}Async(input);

    // Assert
    Assert.That(result, Is.Not.Null);
    {Assertions}
}}

[Test]
public async Task {MethodName}_InvalidInput_ThrowsException()
{{
    // Arrange
    var input = TestDataFactory.CreateInvalid{InputType}();
    
    {MockSetup}

    // Act & Assert
    var exception = Assert.ThrowsAsync<{ExceptionType}>(async () => 
        await _service.{MethodName}Async(input));
    
    Assert.That(exception.Message, Contains.Substring("{ExpectedError}"));
}}

[Test]
public async Task {MethodName}_ExceptionInDependency_HandlesGracefully()
{{
    // Arrange
    var input = TestDataFactory.CreateValid{InputType}();
    
    {ExceptionMockSetup}

    // Act & Assert
    Assert.DoesNotThrowAsync(async () => 
        await _service.{MethodName}Async(input));
    
    {ExceptionAssertions}
}}
"""
            ),
            
            "handler": TestTemplate(
                name="handler",
                description="Шаблон для обработчиков сообщений",
                template="""
[Test]
public async Task HandleAsync_ValidMessage_ProcessesSuccessfully()
{{
    // Arrange
    var message = TestDataFactory.CreateValidMessage();
    var update = new Update {{ Message = message }};
    
    {MockSetup}

    // Act
    await _handler.HandleAsync(update);

    // Assert
    {Assertions}
}}

[Test]
public async Task HandleAsync_SpamMessage_BlocksUser()
{{
    // Arrange
    var message = TestDataFactory.CreateSpamMessage();
    var update = new Update {{ Message = message }};
    
    {MockSetup}

    // Act
    await _handler.HandleAsync(update);

    // Assert
    {Assertions}
}}

[Test]
public async Task HandleAsync_UnapprovedUser_IgnoresMessage()
{{
    // Arrange
    var message = TestDataFactory.CreateValidMessage();
    var update = new Update {{ Message = message }};
    
    {MockSetup}

    // Act
    await _handler.HandleAsync(update);

    // Assert
    {Assertions}
}}

[Test]
public async Task HandleAsync_ExceptionInProcessing_LogsError()
{{
    // Arrange
    var message = TestDataFactory.CreateValidMessage();
    var update = new Update {{ Message = message }};
    
    {ExceptionMockSetup}

    // Act & Assert
    Assert.DoesNotThrowAsync(async () => 
        await _handler.HandleAsync(update));
    
    {ExceptionAssertions}
}}
"""
            ),
            
            "classifier": TestTemplate(
                name="classifier",
                description="Шаблон для классификаторов",
                template="""
[Test]
public async Task {MethodName}_ValidInput_ReturnsCorrectClassification()
{{
    // Arrange
    var input = TestDataFactory.CreateValid{InputType}();

    // Act
    var result = await _classifier.{MethodName}Async(input);

    // Assert
    Assert.That(result, Is.Not.Null);
    {Assertions}
}}

[Test]
public async Task {MethodName}_SpamInput_ReturnsSpamClassification()
{{
    // Arrange
    var input = TestDataFactory.CreateSpam{InputType}();

    // Act
    var result = await _classifier.{MethodName}Async(input);

    // Assert
    Assert.That(result.{SpamProperty}, Is.True);
    Assert.That(result.{ScoreProperty}, Is.GreaterThan(0.5));
}}

[Test]
public async Task {MethodName}_EmptyInput_HandlesGracefully()
{{
    // Arrange
    var input = TestDataFactory.CreateEmpty{InputType}();

    // Act
    var result = await _classifier.{MethodName}Async(input);

    // Assert
    Assert.That(result, Is.Not.Null);
    {EmptyAssertions}
}}

[Test]
public async Task {MethodName}_NullInput_ThrowsArgumentNullException()
{{
    // Arrange
    {InputType}? input = null;

    // Act & Assert
    var exception = Assert.ThrowsAsync<ArgumentNullException>(async () => 
        await _classifier.{MethodName}Async(input));
    
    Assert.That(exception.ParamName, Is.EqualTo("input"));
}}
"""
            )
        }
    
    def generate_test_file(self, class_info: Dict[str, Any], template_type: str = "service") -> str:
        """Генерирует файл тестов на основе шаблона"""
        template = self.templates.get(template_type)
        if not template:
            raise ValueError(f"Unknown template type: {template_type}")
        
        # Определяем тип класса
        class_name = class_info.get("name", "Unknown")
        is_handler = "Handler" in class_name
        is_classifier = "Classifier" in class_name
        
        if is_handler:
            template_type = "handler"
        elif is_classifier:
            template_type = "classifier"
        
        template = self.templates[template_type]
        
        # Заполняем шаблон
        test_content = template.template.format(
            MethodName=self._get_method_name(class_info),
            InputType=self._get_input_type(class_info),
            ExceptionType=self._get_exception_type(class_info),
            ExpectedError="error",
            MockSetup=self._generate_mock_setup(class_info),
            Assertions=self._generate_assertions(class_info),
            ExceptionMockSetup=self._generate_exception_mock_setup(class_info),
            ExceptionAssertions=self._generate_exception_assertions(class_info),
            SpamProperty=self._get_spam_property(class_info),
            ScoreProperty=self._get_score_property(class_info),
            EmptyAssertions=self._generate_empty_assertions(class_info)
        )
        
        # Создаем полный файл
        return self._create_full_test_file(class_info, test_content)
    
    def _get_method_name(self, class_info: Dict[str, Any]) -> str:
        """Получает имя основного метода для тестирования"""
        class_name = class_info.get("name", "")
        if "Handler" in class_name:
            return "Handle"
        elif "Classifier" in class_name:
            return "Classify"
        elif "Service" in class_name:
            return "Process"
        else:
            return "Execute"
    
    def _get_input_type(self, class_info: Dict[str, Any]) -> str:
        """Получает тип входных данных"""
        class_name = class_info.get("name", "")
        if "Message" in class_name:
            return "Message"
        elif "User" in class_name:
            return "User"
        else:
            return "Input"
    
    def _get_exception_type(self, class_info: Dict[str, Any]) -> str:
        """Получает тип исключения"""
        return "ArgumentException"
    
    def _get_spam_property(self, class_info: Dict[str, Any]) -> str:
        """Получает свойство для проверки спама"""
        return "IsSpam"
    
    def _get_score_property(self, class_info: Dict[str, Any]) -> str:
        """Получает свойство для проверки скора"""
        return "Score"
    
    def _generate_mock_setup(self, class_info: Dict[str, Any]) -> str:
        """Генерирует настройку моков"""
        dependencies = class_info.get("dependencies", [])
        setup_lines = []
        
        for dep in dependencies:
            dep_name = dep.get("name", "")
            if "Service" in dep_name:
                setup_lines.append(f"""
    _factory.With{dep_name}Setup(mock => 
        mock.Setup(x => x.ProcessAsync(It.IsAny<{self._get_input_type(class_info)}>()))
            .ReturnsAsync(new ProcessResult {{ Success = true }}));""")
        
        return "\n".join(setup_lines) if setup_lines else "    // No mocks needed"
    
    def _generate_assertions(self, class_info: Dict[str, Any]) -> str:
        """Генерирует проверки"""
        class_name = class_info.get("name", "")
        if "Handler" in class_name:
            return """
    _factory.ModerationServiceMock.Verify(
        x => x.CheckMessageAsync(It.IsAny<Message>()), 
        Times.Once);"""
        else:
            return """
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Success, Is.True);"""
    
    def _generate_exception_mock_setup(self, class_info: Dict[str, Any]) -> str:
        """Генерирует настройку моков для исключений"""
        dependencies = class_info.get("dependencies", [])
        setup_lines = []
        
        for dep in dependencies:
            dep_name = dep.get("name", "")
            if "Service" in dep_name:
                setup_lines.append(f"""
    _factory.With{dep_name}Setup(mock => 
        mock.Setup(x => x.ProcessAsync(It.IsAny<{self._get_input_type(class_info)}>()))
            .ThrowsAsync(new System.Exception("Test exception")));""")
        
        return "\n".join(setup_lines) if setup_lines else "    // No mocks needed"
    
    def _generate_exception_assertions(self, class_info: Dict[str, Any]) -> str:
        """Генерирует проверки для исключений"""
        return """
    _factory.LoggerMock.Verify(
        x => x.Log(
            It.IsAny<Microsoft.Extensions.Logging.LogLevel>(),
            It.IsAny<Microsoft.Extensions.Logging.EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.AtLeastOnce);"""
    
    def _generate_empty_assertions(self, class_info: Dict[str, Any]) -> str:
        """Генерирует проверки для пустых входных данных"""
        return """
    Assert.That(result, Is.Not.Null);"""
    
    def _create_full_test_file(self, class_info: Dict[str, Any], test_content: str) -> str:
        """Создает полный файл тестов"""
        class_name = class_info.get("name", "Unknown")
        namespace = class_info.get("namespace", "ClubDoorman")
        
        return f"""using {namespace};
using {namespace}.TestInfrastructure;
using {namespace}.Test.TestData;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace {namespace}.Test;

[TestFixture]
[Category("unit")]
public class {class_name}Tests
{{
    private {class_name}TestFactory _factory;
    private {class_name} _instance;

    [SetUp]
    public void Setup()
    {{
        _factory = new {class_name}TestFactory();
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

{test_content}
}}"""
    
    def generate_all_templates(self, classes_info: List[Dict[str, Any]]) -> Dict[str, str]:
        """Генерирует шаблоны тестов для всех классов"""
        result = {}
        
        for class_info in classes_info:
            class_name = class_info.get("name", "Unknown")
            template_type = "service"
            
            if "Handler" in class_name:
                template_type = "handler"
            elif "Classifier" in class_name:
                template_type = "classifier"
            
            test_file = self.generate_test_file(class_info, template_type)
            result[f"{class_name}Tests.cs"] = test_file
        
        return result


def main():
    """Основная функция для тестирования"""
    generator = TestTemplateGenerator()
    
    # Пример использования
    sample_class = {
        "name": "ModerationService",
        "namespace": "ClubDoorman.Services",
        "dependencies": [
            {"name": "SpamHamClassifier", "type": "ISpamHamClassifier"},
            {"name": "UserManager", "type": "IUserManager"}
        ]
    }
    
    test_file = generator.generate_test_file(sample_class, "service")
    print("Generated test file:")
    print(test_file)


if __name__ == "__main__":
    main() 