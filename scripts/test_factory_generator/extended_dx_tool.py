#!/usr/bin/env python3
"""
Расширенный DX Tool - полная автоматизация тестирования
"""

import sys
from pathlib import Path
from typing import List, Dict, Optional
from dataclasses import dataclass

try:
    from csharp_analyzer import CSharpAnalyzer
    from factory_generator import TestFactoryGenerator
    from complexity_analyzer import ComplexityAnalyzer
    from test_data_generator import TestDataGenerator
    from .mock_scenario_generator import MockScenarioGenerator
    from .models import ClassInfo
except ImportError:
    from csharp_analyzer import CSharpAnalyzer
    from factory_generator import TestFactoryGenerator
    from complexity_analyzer import ComplexityAnalyzer
    from test_data_generator import TestDataGenerator
    from mock_scenario_generator import MockScenarioGenerator
    from models import ClassInfo


@dataclass
class TestSuite:
    """Полный набор тестов для класса"""
    factory: str
    test_data: str
    mock_scenarios: str
    test_methods: str
    asserts: str
    scenarios: str
    documentation: str


class ExtendedDXTool:
    """Расширенный DX Tool с полной автоматизацией тестирования"""
    
    def __init__(self, project_root: Path, test_project_root: Path):
        self.project_root = project_root
        self.test_project_root = test_project_root
        
        # Инициализация всех генераторов
        self.analyzer = CSharpAnalyzer(str(project_root))
        self.factory_generator = TestFactoryGenerator(test_project_root)
        self.complexity_analyzer = ComplexityAnalyzer()
        self.test_data_generator = TestDataGenerator(project_root / "ClubDoorman" / "Models")
        self.mock_scenario_generator = MockScenarioGenerator()
        
        # Конфигурация
        self.config = {
            "enabled_generators": [
                "test_factory",
                "test_data",
                "mock_scenarios"
            ],
            "test_data": {
                "generate_edge_cases": True,
                "generate_scenario_data": True,
                "unified_factory": True
            },
            "mock_scenarios": {
                "generate_happy_path": True,
                "generate_error_scenarios": True,
                "generate_edge_cases": True
            }
        }
    
    def generate_complete_test_suite(self, class_name: str) -> TestSuite:
        """Генерирует полный набор тестов для класса"""
        print(f"🔧 Генерация полного набора тестов для {class_name}...")
        
        # 1. Анализируем класс
        services = self.analyzer.find_service_classes()
        target_class = next((s for s in services if s.name == class_name), None)
        
        if not target_class:
            raise ValueError(f"Класс {class_name} не найден")
        
        # 2. Анализируем сложность
        complexity_report = self.analyzer.analyze_class_complexity(target_class)
        print(f"📊 Сложность: {complexity_report.complexity_score}/10 ({complexity_report.complexity_level})")
        
        # 3. Ищем маркеры
        source_file = self.analyzer.find_source_file(target_class)
        markers = []
        if source_file:
            markers = self.analyzer.find_test_markers(source_file)
        
        # 4. Генерируем компоненты
        factory = ""
        test_data = ""
        mock_scenarios = ""
        
        if "test_factory" in self.config["enabled_generators"]:
            print("🏭 Генерация TestFactory...")
            self.factory_generator.set_complexity_analysis(complexity_report, markers)
            factory = self.factory_generator.generate_test_factory(target_class)
        
        if "test_data" in self.config["enabled_generators"]:
            print("📊 Генерация тестовых данных...")
            test_data = self.test_data_generator.generate_test_data_factory()
        
        if "mock_scenarios" in self.config["enabled_generators"]:
            print("🎭 Генерация сценариев моков...")
            mock_scenarios = self._generate_mock_scenarios(target_class)
        
        # Заглушки для будущих генераторов
        test_methods = "// Test Method Generator - в разработке"
        asserts = "// Assert Generator - в разработке"
        scenarios = "// Test Scenario Generator - в разработке"
        documentation = "// Test Documentation Generator - в разработке"
        
        return TestSuite(
            factory=factory,
            test_data=test_data,
            mock_scenarios=mock_scenarios,
            test_methods=test_methods,
            asserts=asserts,
            scenarios=scenarios,
            documentation=documentation
        )
    
    def _generate_mock_scenarios(self, class_info: ClassInfo) -> str:
        """Генерирует сценарии моков для интерфейсов класса"""
        scenarios_code = ""
        
        # Находим интерфейсы среди параметров конструктора
        interface_params = [p for p in class_info.constructor_params if p.is_interface]
        
        for param in interface_params:
            interface_name = param.type
            interface_file = self._find_interface_file(interface_name)
            
            if interface_file:
                print(f"  📋 Генерация сценариев для {interface_name}...")
                scenarios = self.mock_scenario_generator.generate_scenarios_for_interface(interface_file)
                
                if scenarios:
                    scenarios_code += f"\n// Сценарии для {interface_name}\n"
                    scenarios_code += self.mock_scenario_generator.generate_scenario_methods(scenarios, interface_name)
                    scenarios_code += self.mock_scenario_generator.generate_fluent_api(scenarios, interface_name)
        
        return scenarios_code
    
    def _find_interface_file(self, interface_name: str) -> Optional[Path]:
        """Находит файл интерфейса"""
        # Убираем 'I' в начале для поиска файла
        if interface_name.startswith('I'):
            file_name = interface_name[1:] + ".cs"
        else:
            file_name = interface_name + ".cs"
        
        # Ищем в папке Services
        services_dir = self.project_root / "ClubDoorman" / "Services"
        interface_file = services_dir / file_name
        
        if interface_file.exists():
            return interface_file
        
        # Ищем в других папках
        for search_dir in [self.project_root / "ClubDoorman"]:
            for file_path in search_dir.rglob("*.cs"):
                if file_path.name == file_name:
                    return file_path
        
        return None
    
    def save_test_suite(self, test_suite: TestSuite, class_name: str) -> None:
        """Сохраняет полный набор тестов"""
        # Создаем директории
        test_infrastructure_dir = self.test_project_root / "TestInfrastructure"
        test_data_dir = self.test_project_root / "TestData"
        
        test_infrastructure_dir.mkdir(parents=True, exist_ok=True)
        test_data_dir.mkdir(parents=True, exist_ok=True)
        
        # Сохраняем TestFactory
        if test_suite.factory:
            factory_file = test_infrastructure_dir / f"{class_name}TestFactory.Generated.cs"
            with open(factory_file, 'w', encoding='utf-8') as f:
                f.write(test_suite.factory)
            print(f"✅ Сохранен: {factory_file}")
        
        # Сохраняем тестовые данные
        if test_suite.test_data:
            test_data_file = test_data_dir / "TestDataFactory.Generated.cs"
            with open(test_data_file, 'w', encoding='utf-8') as f:
                f.write(test_suite.test_data)
            print(f"✅ Сохранен: {test_data_file}")
        
        # Сохраняем сценарии моков
        if test_suite.mock_scenarios:
            scenarios_file = test_infrastructure_dir / f"{class_name}MockScenarios.Generated.cs"
            with open(scenarios_file, 'w', encoding='utf-8') as f:
                f.write(self._generate_mock_scenarios_wrapper(class_name, test_suite.mock_scenarios))
            print(f"✅ Сохранен: {scenarios_file}")
    
    def _generate_mock_scenarios_wrapper(self, class_name: str, scenarios_code: str) -> str:
        """Генерирует обертку для сценариев моков"""
        return f"""using Moq;
using System;
using System.Threading.Tasks;
using ClubDoorman.Services;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Сценарии моков для {class_name}
/// Автоматически сгенерировано
/// </summary>
public static class {class_name}MockScenarios
{{
{scenarios_code}
}}"""
    
    def list_available_classes(self) -> List[str]:
        """Возвращает список доступных классов"""
        services = self.analyzer.find_service_classes()
        return [s.name for s in services]
    
    def generate_for_all_classes(self) -> None:
        """Генерирует тесты для всех классов"""
        classes = self.list_available_classes()
        print(f"🔧 Генерация тестов для {len(classes)} классов...")
        
        for class_name in classes:
            try:
                print(f"\n📋 Обработка {class_name}...")
                test_suite = self.generate_complete_test_suite(class_name)
                self.save_test_suite(test_suite, class_name)
            except Exception as e:
                print(f"❌ Ошибка при обработке {class_name}: {e}")


def main():
    """Основная функция"""
    if len(sys.argv) < 2:
        print("Использование: python extended_dx_tool.py <команда> [параметры]")
        print("Команды:")
        print("  list                    - показать доступные классы")
        print("  generate <класс>        - сгенерировать тесты для класса")
        print("  generate-all            - сгенерировать тесты для всех классов")
        return
    
    command = sys.argv[1]
    
    # Определяем пути
    project_root = Path("../../ClubDoorman")
    test_project_root = Path("../../ClubDoorman.Test")
    
    if not project_root.exists():
        print(f"❌ Папка проекта не найдена: {project_root}")
        return
    
    if not test_project_root.exists():
        print(f"❌ Папка тестов не найдена: {test_project_root}")
        return
    
    tool = ExtendedDXTool(project_root, test_project_root)
    
    if command == "list":
        classes = tool.list_available_classes()
        print("📋 Доступные классы:")
        for class_name in classes:
            print(f"  - {class_name}")
    
    elif command == "generate":
        if len(sys.argv) < 3:
            print("❌ Укажите имя класса")
            return
        
        class_name = sys.argv[2]
        try:
            test_suite = tool.generate_complete_test_suite(class_name)
            tool.save_test_suite(test_suite, class_name)
            print(f"✅ Тесты для {class_name} сгенерированы успешно")
        except Exception as e:
            print(f"❌ Ошибка: {e}")
    
    elif command == "generate-all":
        tool.generate_for_all_classes()
        print("✅ Генерация завершена")
    
    else:
        print(f"❌ Неизвестная команда: {command}")


if __name__ == "__main__":
    main() 