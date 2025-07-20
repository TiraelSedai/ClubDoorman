#!/usr/bin/env python3
"""
Генератор сценариев для моков
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
class MethodInfo:
    """Информация о методе интерфейса"""
    name: str
    return_type: str
    parameters: List[str]
    is_async: bool = False
    is_void: bool = False


@dataclass
class MockScenario:
    """Сценарий настройки мока"""
    name: str
    description: str
    setup_code: str
    category: str  # happy_path, error, edge_case


class MockScenarioGenerator:
    """Генератор сценариев для моков"""
    
    def __init__(self):
        self.common_scenarios = {
            'success': {
                'description': 'Успешное выполнение',
                'category': 'happy_path'
            },
            'exception': {
                'description': 'Выброс исключения',
                'category': 'error'
            },
            'timeout': {
                'description': 'Таймаут операции',
                'category': 'error'
            },
            'null_result': {
                'description': 'Возврат null',
                'category': 'edge_case'
            },
            'empty_result': {
                'description': 'Возврат пустого результата',
                'category': 'edge_case'
            }
        }
    
    def _parse_interface_file(self, file_path: Path) -> List[MethodInfo]:
        """Парсит файл интерфейса и извлекает методы"""
        methods = []
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
        except FileNotFoundError:
            return methods
        
        # Поиск методов интерфейса
        method_pattern = r'(?:Task<([^>]+)>|Task|void)\s+(\w+)\s*\(([^)]*)\)'
        for match in re.finditer(method_pattern, content):
            return_type = match.group(1) or 'void'
            method_name = match.group(2)
            params_str = match.group(3)
            
            # Определяем характеристики метода
            is_async = 'Task' in match.group(0)
            is_void = return_type == 'void'
            
            # Парсим параметры
            parameters = self._parse_method_parameters(params_str)
            
            methods.append(MethodInfo(
                name=method_name,
                return_type=return_type,
                parameters=parameters,
                is_async=is_async,
                is_void=is_void
            ))
        
        return methods
    
    def _parse_method_parameters(self, params_str: str) -> List[str]:
        """Парсит параметры метода"""
        if not params_str.strip():
            return []
        
        # Разбиваем параметры по запятой
        params = [p.strip() for p in params_str.split(',')]
        
        # Извлекаем типы параметров
        parameter_types = []
        for param in params:
            if param:
                # Ищем тип параметра
                type_match = re.search(r'(\w+(?:<[^>]+>)?(?:\?)?)', param)
                if type_match:
                    parameter_types.append(type_match.group(1))
        
        return parameter_types
    
    def _generate_success_scenario(self, method: MethodInfo) -> MockScenario:
        """Генерирует сценарий успешного выполнения"""
        if method.is_void:
            setup_code = f"""
        mock.Setup(x => x.{method.name}(It.IsAny<{'>(), It.IsAny<'.join(method.parameters)}>()))
            .Returns(Task.CompletedTask);"""
        elif method.is_async:
            return_value = self._get_default_return_value(method.return_type)
            setup_code = f"""
        mock.Setup(x => x.{method.name}(It.IsAny<{'>(), It.IsAny<'.join(method.parameters)}>()))
            .ReturnsAsync({return_value});"""
        else:
            return_value = self._get_default_return_value(method.return_type)
            setup_code = f"""
        mock.Setup(x => x.{method.name}(It.IsAny<{'>(), It.IsAny<'.join(method.parameters)}>()))
            .Returns({return_value});"""
        
        return MockScenario(
            name=f"{method.name}_Success",
            description=f"Успешное выполнение {method.name}",
            setup_code=setup_code,
            category="happy_path"
        )
    
    def _generate_exception_scenario(self, method: MethodInfo) -> MockScenario:
        """Генерирует сценарий исключения"""
        setup_code = f"""
        mock.Setup(x => x.{method.name}(It.IsAny<{'>(), It.IsAny<'.join(method.parameters)}>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));"""
        
        return MockScenario(
            name=f"{method.name}_Exception",
            description=f"Исключение в {method.name}",
            setup_code=setup_code,
            category="error"
        )
    
    def _generate_timeout_scenario(self, method: MethodInfo) -> MockScenario:
        """Генерирует сценарий таймаута"""
        if method.is_async:
            setup_code = f"""
        mock.Setup(x => x.{method.name}(It.IsAny<{'>(), It.IsAny<'.join(method.parameters)}>()))
            .Returns(async () => {{
                await Task.Delay(5000); // Имитация таймаута
                throw new TimeoutException("Operation timed out");
            }});"""
        else:
            setup_code = f"""
        mock.Setup(x => x.{method.name}(It.IsAny<{'>(), It.IsAny<'.join(method.parameters)}>()))
            .Throws(new TimeoutException("Operation timed out"));"""
        
        return MockScenario(
            name=f"{method.name}_Timeout",
            description=f"Таймаут в {method.name}",
            setup_code=setup_code,
            category="error"
        )
    
    def _generate_null_result_scenario(self, method: MethodInfo) -> MockScenario:
        """Генерирует сценарий возврата null"""
        if method.is_void or method.return_type in ['bool', 'int', 'long', 'double', 'float']:
            return None  # Не применимо для void и примитивных типов
        
        if method.is_async:
            setup_code = f"""
        mock.Setup(x => x.{method.name}(It.IsAny<{'>(), It.IsAny<'.join(method.parameters)}>()))
            .ReturnsAsync(({method.return_type})null);"""
        else:
            setup_code = f"""
        mock.Setup(x => x.{method.name}(It.IsAny<{'>(), It.IsAny<'.join(method.parameters)}>()))
            .Returns(({method.return_type})null);"""
        
        return MockScenario(
            name=f"{method.name}_NullResult",
            description=f"Возврат null из {method.name}",
            setup_code=setup_code,
            category="edge_case"
        )
    
    def _get_default_return_value(self, return_type: str) -> str:
        """Возвращает значение по умолчанию для типа"""
        type_lower = return_type.lower()
        
        if 'bool' in type_lower:
            return "true"
        elif 'int' in type_lower or 'long' in type_lower:
            return "1"
        elif 'double' in type_lower or 'float' in type_lower:
            return "0.5"
        elif 'string' in type_lower:
            return '"test"'
        elif 'list' in type_lower or 'ienumerable' in type_lower:
            return "new List<object>()"
        elif 'dictionary' in type_lower:
            return "new Dictionary<string, object>()"
        elif return_type.endswith('?'):
            return "null"
        else:
            return f"new {return_type}()"
    
    def generate_scenarios_for_interface(self, interface_path: Path) -> List[MockScenario]:
        """Генерирует сценарии для интерфейса"""
        methods = self._parse_interface_file(interface_path)
        scenarios = []
        
        for method in methods:
            # Генерируем сценарии для каждого метода
            scenarios.append(self._generate_success_scenario(method))
            scenarios.append(self._generate_exception_scenario(method))
            
            if method.is_async:
                scenarios.append(self._generate_timeout_scenario(method))
            
            null_scenario = self._generate_null_result_scenario(method)
            if null_scenario:
                scenarios.append(null_scenario)
        
        return scenarios
    
    def generate_scenario_methods(self, scenarios: List[MockScenario], interface_name: str) -> str:
        """Генерирует методы для настройки сценариев"""
        code = f"""
    #region Mock Scenarios

"""
        
        # Группируем сценарии по категориям
        scenarios_by_category = {}
        for scenario in scenarios:
            if scenario.category not in scenarios_by_category:
                scenarios_by_category[scenario.category] = []
            scenarios_by_category[scenario.category].append(scenario)
        
        # Генерируем методы для каждой категории
        for category, category_scenarios in scenarios_by_category.items():
            category_name = category.replace('_', ' ').title().replace(' ', '')
            
            code += f"    #region {category_name} Scenarios\n\n"
            
            for scenario in category_scenarios:
                method_name = f"With{scenario.name}"
                code += f"""    /// <summary>
    /// {scenario.description}
    /// </summary>
    public Mock<{interface_name}> {method_name}()
    {{
        {scenario.setup_code.strip()}
        return mock;
    }}

"""
            
            code += "    #endregion\n\n"
        
        code += "    #endregion"
        return code
    
    def generate_fluent_api(self, scenarios: List[MockScenario], interface_name: str) -> str:
        """Генерирует Fluent API для настройки сценариев"""
        code = f"""
    #region Fluent API

    /// <summary>
    /// Настройка мока для успешных сценариев
    /// </summary>
    public Mock<{interface_name}> WithSuccessScenarios()
    {{
"""
        
        # Добавляем все success сценарии
        success_scenarios = [s for s in scenarios if s.category == "happy_path"]
        for scenario in success_scenarios:
            code += f"        {scenario.setup_code.strip()}\n"
        
        code += """        return mock;
    }

    /// <summary>
    /// Настройка мока для сценариев с ошибками
    /// </summary>
    public Mock<{interface_name}> WithErrorScenarios()
    {{
"""
        
        # Добавляем все error сценарии
        error_scenarios = [s for s in scenarios if s.category == "error"]
        for scenario in error_scenarios:
            code += f"        {scenario.setup_code.strip()}\n"
        
        code += """        return mock;
    }

    /// <summary>
    /// Настройка мока для edge cases
    /// </summary>
    public Mock<{interface_name}> WithEdgeCaseScenarios()
    {{
"""
        
        # Добавляем все edge case сценарии
        edge_scenarios = [s for s in scenarios if s.category == "edge_case"]
        for scenario in edge_scenarios:
            code += f"        {scenario.setup_code.strip()}\n"
        
        code += """        return mock;
    }

    #endregion"""
        
        return code


def test_mock_scenario_generator():
    """Тестирует генератор сценариев моков"""
    print("🔧 Тестирование Mock Scenario Generator...")
    
    # Создаем тестовый интерфейс
    test_interface = """
namespace ClubDoorman.Services;

public interface ITestService
{
    Task<bool> CheckAsync(string input);
    Task<string> GetDataAsync(int id);
    void DoSomething(string param);
    Task<List<string>> GetListAsync();
    string GetValue(int id);
}
"""
    
    # Сохраняем тестовый интерфейс
    test_file = Path("test_interface.cs")
    with open(test_file, 'w', encoding='utf-8') as f:
        f.write(test_interface)
    
    try:
        generator = MockScenarioGenerator()
        
        # Генерируем сценарии
        scenarios = generator.generate_scenarios_for_interface(test_file)
        
        print(f"✅ Сгенерировано {len(scenarios)} сценариев:")
        for scenario in scenarios:
            print(f"  - {scenario.name}: {scenario.description} ({scenario.category})")
        
        # Генерируем методы
        methods_code = generator.generate_scenario_methods(scenarios, "ITestService")
        fluent_api_code = generator.generate_fluent_api(scenarios, "ITestService")
        
        print("\n✅ Методы сценариев:")
        print("=" * 80)
        print(methods_code)
        print("=" * 80)
        
        print("\n✅ Fluent API:")
        print("=" * 80)
        print(fluent_api_code)
        print("=" * 80)
        
    finally:
        # Удаляем тестовый файл
        test_file.unlink(missing_ok=True)


if __name__ == "__main__":
    test_mock_scenario_generator() 