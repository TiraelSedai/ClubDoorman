#!/usr/bin/env python3
"""
Enhanced DX Tool - Улучшенный инструмент для генерации тестовых фабрик
Интегрирует анализ реальных интерфейсов и упрощенные фабрики
"""
import argparse
import sys
import re
from pathlib import Path
from typing import List, Dict, Any, Optional

# Новые компоненты
from interface_analyzer import InterfaceAnalyzer, MethodInfo, InterfaceInfo, EnumInfo
from simple_factory_generator import SimpleFactoryGenerator, ClassInfo, DependencyInfo


class CSharpAnalyzer:
    """Упрощенный анализатор C# для обратной совместимости"""
    
    def __init__(self, project_root: Path):
        self.project_root = project_root
    
    def analyze_class(self, class_name: str) -> Optional[Any]:
        """Анализирует класс (упрощенная версия)"""
        # Ищем файл класса
        class_file = self._find_class_file(class_name)
        if not class_file:
            return None
        
        # Простой анализ конструктора
        content = class_file.read_text(encoding='utf-8')
        
        # Извлекаем namespace
        namespace_match = re.search(r'namespace\s+([^;]+)', content)
        namespace = namespace_match.group(1).strip() if namespace_match else "ClubDoorman"
        
        # Извлекаем зависимости из конструктора
        dependencies = self._extract_dependencies(content)
        
        # Извлекаем параметры конструктора
        constructor_params = self._extract_constructor_params(content)
        
        return type('ClassInfo', (), {
            'name': class_name,
            'namespace': namespace,
            'dependencies': dependencies,
            'constructor_params': constructor_params
        })()
    
    def find_all_classes(self) -> List[str]:
        """Находит все классы в проекте"""
        classes = []
        
        # Ищем в Services
        services_dir = self.project_root / "ClubDoorman" / "Services"
        if services_dir.exists():
            for file in services_dir.glob("*.cs"):
                if file.name.endswith(".cs") and not file.name.startswith("I"):
                    class_name = file.stem
                    classes.append(class_name)
        
        # Ищем в Handlers
        handlers_dir = self.project_root / "ClubDoorman" / "Handlers"
        if handlers_dir.exists():
            for file in handlers_dir.glob("*.cs"):
                if file.name.endswith(".cs") and not file.name.startswith("I"):
                    class_name = file.stem
                    classes.append(class_name)
        
        return classes
    
    def _find_class_file(self, class_name: str) -> Optional[Path]:
        """Находит файл класса"""
        # Ищем в Services
        services_dir = self.project_root / "ClubDoorman" / "Services"
        if services_dir.exists():
            class_file = services_dir / f"{class_name}.cs"
            if class_file.exists():
                return class_file
        
        # Ищем в Handlers
        handlers_dir = self.project_root / "ClubDoorman" / "Handlers"
        if handlers_dir.exists():
            class_file = handlers_dir / f"{class_name}.cs"
            if class_file.exists():
                return class_file
        
        return None
    
    def _extract_dependencies(self, content: str) -> List[str]:
        """Извлекает зависимости из конструктора"""
        dependencies = []
        
        # Ищем конструктор - более гибкий паттерн
        constructor_pattern = r'public\s+\w+\s*\(([^)]*)\)'
        constructor_matches = re.finditer(constructor_pattern, content)
        
        for match in constructor_matches:
            params_str = match.group(1)
            
            # Парсим параметры
            for param in params_str.split(','):
                param = param.strip()
                if param:
                    # Извлекаем тип
                    words = param.split()
                    if len(words) >= 2:
                        param_type = words[0]
                        if param_type.startswith('I'):
                            dependencies.append(param_type)
        
        return dependencies
    
    def _extract_constructor_params(self, content: str) -> List[str]:
        """Извлекает параметры конструктора"""
        params = []
        
        # Ищем конструктор
        constructor_match = re.search(r'public\s+\w+\s*\(([^)]*)\)', content)
        if constructor_match:
            params_str = constructor_match.group(1)
            
            # Парсим параметры
            for param in params_str.split(','):
                param = param.strip()
                if param:
                    # Извлекаем имя параметра
                    words = param.split()
                    if len(words) >= 2:
                        param_name = words[-1]
                        params.append(param_name)
        
        return params


class EnhancedDXTool:
    """Улучшенный DX Tool с анализом реальных интерфейсов"""
    
    def __init__(self, project_root: Path):
        self.project_root = project_root
        self.csharp_analyzer = CSharpAnalyzer(project_root)
        
        # Новые компоненты
        self.interface_analyzer = InterfaceAnalyzer(project_root)
        self.simple_factory_generator = SimpleFactoryGenerator(project_root)
    
    def analyze_interfaces(self, class_name: str) -> Optional[InterfaceInfo]:
        """Анализирует интерфейсы для класса"""
        # Ищем интерфейсы в зависимостях
        class_info = self.csharp_analyzer.analyze_class(class_name)
        if not class_info:
            print(f"DEBUG: Класс {class_name} не найден")
            return None
        
        print(f"DEBUG: Найдены зависимости: {class_info.dependencies}")
        
        # Анализируем каждый интерфейс
        for dependency in class_info.dependencies:
            print(f"DEBUG: Анализируем зависимость: {dependency}")
            if dependency.startswith('I'):
                interface_file = self.interface_analyzer.find_interface_file(dependency)
                print(f"DEBUG: Файл интерфейса: {interface_file}")
                if interface_file:
                    return self.interface_analyzer.analyze_interface(interface_file)
        
        return None
    
    def generate_simple_factory(self, class_name: str) -> str:
        """Генерирует упрощенную фабрику с анализом реальных интерфейсов"""
        # Анализируем класс
        old_class_info = self.csharp_analyzer.analyze_class(class_name)
        if not old_class_info:
            raise ValueError(f"Класс {class_name} не найден")
        
        # Конвертируем в новый формат
        class_info = self._convert_to_new_class_info(old_class_info)
        
        # Анализируем интерфейсы для правильных моков
        self._enhance_with_interface_analysis(class_info)
        
        # Генерируем упрощенную фабрику
        return self.simple_factory_generator.generate_simple_factory(class_info)
    
    def generate_test_template(self, class_name: str) -> str:
        """Генерирует шаблон тестов для класса"""
        # Анализируем класс
        old_class_info = self.csharp_analyzer.analyze_class(class_name)
        if not old_class_info:
            raise ValueError(f"Класс {class_name} не найден")
        
        # Конвертируем в новый формат
        class_info = self._convert_to_new_class_info(old_class_info)
        
        # Генерируем шаблон тестов
        return self.simple_factory_generator.generate_test_template(class_info)
    
    def validate_generated_code(self, generated_code: str) -> Dict[str, Any]:
        """Валидирует сгенерированный код"""
        validation_result = {
            "is_valid": True,
            "errors": [],
            "warnings": [],
            "suggestions": []
        }
        
        # Проверяем базовый синтаксис C#
        if not self._check_csharp_syntax(generated_code):
            validation_result["is_valid"] = False
            validation_result["errors"].append("Ошибка синтаксиса C#")
        
        # Проверяем использование правильных методов
        if "ClassifyAsync" in generated_code:
            validation_result["warnings"].append("Использован устаревший метод ClassifyAsync")
        
        if "ClassificationResult" in generated_code:
            validation_result["warnings"].append("Использован устаревший тип ClassificationResult")
        
        if "ModerationAction.Block" in generated_code:
            validation_result["errors"].append("Использовано несуществующее значение ModerationAction.Block")
        
        if "ModerationAction.Captcha" in generated_code:
            validation_result["errors"].append("Использовано несуществующее значение ModerationAction.Captcha")
        
        # Проверяем необязательные параметры
        if "It.IsAny<long>()" in generated_code and "Approved" in generated_code:
            validation_result["suggestions"].append("Рассмотрите использование явных параметров для необязательных аргументов")
        
        return validation_result
    
    def _convert_to_new_class_info(self, old_class_info: Any) -> ClassInfo:
        """Конвертирует старый формат ClassInfo в новый"""
        dependencies = []
        
        for dep in old_class_info.dependencies:
            # Извлекаем имя интерфейса
            interface_name = dep.replace('I', '') if dep.startswith('I') else dep
            
            dependency_info = DependencyInfo(
                name=interface_name,
                type=dep,
                interface_name=dep,
                is_optional=False
            )
            dependencies.append(dependency_info)
        
        return ClassInfo(
            name=old_class_info.name,
            namespace=old_class_info.namespace,
            dependencies=dependencies,
            constructor_params=old_class_info.constructor_params
        )
    
    def _enhance_with_interface_analysis(self, class_info: ClassInfo):
        """Улучшает информацию о классе анализом интерфейсов"""
        for dep in class_info.dependencies:
            # Анализируем интерфейс
            interface_file = self.interface_analyzer.find_interface_file(dep.interface_name)
            if interface_file:
                interface_info = self.interface_analyzer.analyze_interface(interface_file)
                
                # Обновляем информацию о зависимости
                dep.type = interface_info.name
                dep.interface_name = interface_info.name
    
    def _check_csharp_syntax(self, code: str) -> bool:
        """Проверяет базовый синтаксис C#"""
        # Простые проверки
        if code.count('{') != code.count('}'):
            return False
        
        if code.count('(') != code.count(')'):
            return False
        
        if code.count('[') != code.count(']'):
            return False
        
        return True
    
    # Методы для обратной совместимости
    def generate_test_factory(self, class_name: str) -> str:
        """Генерирует тестовую фабрику (старый метод)"""
        return self.generate_simple_factory(class_name)
    
    def find_all_classes(self) -> List[str]:
        """Находит все классы в проекте"""
        return self.csharp_analyzer.find_all_classes()


def main():
    """Основная функция CLI"""
    parser = argparse.ArgumentParser(description="Enhanced DX Tool - Улучшенный инструмент для генерации тестов")
    parser.add_argument("command", choices=[
        "generate", "generate-all", "dry-run", "analyze-interfaces", 
        "generate-templates", "validate", "generate-simple"
    ], help="Команда для выполнения")
    parser.add_argument("--class", dest="class_name", help="Имя класса для обработки")
    parser.add_argument("--project-root", default=".", help="Корневая директория проекта")
    parser.add_argument("--output", help="Файл для вывода результата")
    
    args = parser.parse_args()
    
    project_root = Path(args.project_root).resolve()
    if not project_root.exists():
        print(f"Ошибка: Директория проекта {project_root} не существует")
        sys.exit(1)
    
    tool = EnhancedDXTool(project_root)
    
    try:
        if args.command == "generate":
            if not args.class_name:
                print("Ошибка: Необходимо указать имя класса с --class")
                sys.exit(1)
            
            result = tool.generate_test_factory(args.class_name)
            if args.output:
                Path(args.output).write_text(result, encoding='utf-8')
            else:
                print(result)
        
        elif args.command == "generate-simple":
            if not args.class_name:
                print("Ошибка: Необходимо указать имя класса с --class")
                sys.exit(1)
            
            result = tool.generate_simple_factory(args.class_name)
            if args.output:
                Path(args.output).write_text(result, encoding='utf-8')
            else:
                print(result)
        
        elif args.command == "generate-all":
            classes = tool.find_all_classes()
            print(f"Найдено {len(classes)} классов:")
            
            for class_name in classes:
                print(f"  - {class_name}")
                try:
                    result = tool.generate_simple_factory(class_name)
                    output_file = project_root / "ClubDoorman.Test" / "TestInfrastructure" / f"{class_name}TestFactory.Generated.cs"
                    output_file.write_text(result, encoding='utf-8')
                    print(f"    ✅ Сгенерирована фабрика: {output_file}")
                except Exception as e:
                    print(f"    ❌ Ошибка: {e}")
        
        elif args.command == "dry-run":
            classes = tool.find_all_classes()
            print(f"Анализ {len(classes)} классов:")
            print()
            
            for class_name in classes:
                try:
                    print(f"{class_name}:")
                    class_info = tool.csharp_analyzer.analyze_class(class_name)
                    if class_info:
                        print(f"  Зависимости: {len(class_info.dependencies)}")
                        print(f"  Параметры конструктора: {len(class_info.constructor_params)}")
                    print()
                except Exception as e:
                    print(f"{class_name}: ❌ {e}")
        
        elif args.command == "analyze-interfaces":
            if not args.class_name:
                print("Ошибка: Необходимо указать имя класса с --class")
                sys.exit(1)
            
            interface_info = tool.analyze_interfaces(args.class_name)
            if interface_info:
                print(f"Интерфейс: {interface_info.name}")
                print(f"Namespace: {interface_info.namespace}")
                print("Методы:")
                for method in interface_info.methods:
                    params_str = ", ".join([f"{t} {n}" for t, n in method.parameters])
                    async_str = "async " if method.is_async else ""
                    print(f"  {async_str}{method.return_type} {method.name}({params_str})")
            else:
                print("Интерфейсы не найдены")
        
        elif args.command == "generate-templates":
            if not args.class_name:
                print("Ошибка: Необходимо указать имя класса с --class")
                sys.exit(1)
            
            result = tool.generate_test_template(args.class_name)
            if args.output:
                Path(args.output).write_text(result, encoding='utf-8')
            else:
                print(result)
        
        elif args.command == "validate":
            if not args.class_name:
                print("Ошибка: Необходимо указать имя класса с --class")
                sys.exit(1)
            
            # Генерируем код и валидируем его
            generated_code = tool.generate_simple_factory(args.class_name)
            validation = tool.validate_generated_code(generated_code)
            
            print(f"Валидация для {args.class_name}:")
            print(f"  Валидность: {'✅' if validation['is_valid'] else '❌'}")
            
            if validation['errors']:
                print("  Ошибки:")
                for error in validation['errors']:
                    print(f"    ❌ {error}")
            
            if validation['warnings']:
                print("  Предупреждения:")
                for warning in validation['warnings']:
                    print(f"    ⚠️ {warning}")
            
            if validation['suggestions']:
                print("  Рекомендации:")
                for suggestion in validation['suggestions']:
                    print(f"    💡 {suggestion}")
    
    except Exception as e:
        print(f"Ошибка: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main() 