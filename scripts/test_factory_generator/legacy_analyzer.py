#!/usr/bin/env python3
"""
Анализатор legacy методов в существующих тестах
Определяет какие методы нужны в TestFactory на основе использования в тестах
"""

import re
from pathlib import Path
from typing import List, Set, Dict
from dataclasses import dataclass


@dataclass
class LegacyMethodInfo:
    """Информация о legacy методе"""
    method_name: str
    factory_class: str
    return_type: str = ""
    parameters: List[str] = None
    usage_count: int = 0
    
    def __post_init__(self):
        if self.parameters is None:
            self.parameters = []


class LegacyAnalyzer:
    """Анализатор legacy методов в тестах"""
    
    def __init__(self, test_project_root: Path):
        self.test_project_root = test_project_root
        
    def analyze_existing_tests(self) -> Dict[str, List[LegacyMethodInfo]]:
        """Анализирует существующие тесты и определяет недостающие методы"""
        legacy_methods = {}
        
        # Ищем все .cs файлы в тестах
        for cs_file in self.test_project_root.rglob("*.cs"):
            if "TestFactory" in cs_file.name or "TestFactoryTests" in cs_file.name:
                continue  # Пропускаем сами TestFactory
                
            methods = self._analyze_file(cs_file)
            for method in methods:
                if method.factory_class not in legacy_methods:
                    legacy_methods[method.factory_class] = []
                legacy_methods[method.factory_class].append(method)
        
        return legacy_methods
    
    def _analyze_file(self, file_path: Path) -> List[LegacyMethodInfo]:
        """Анализирует один файл на предмет использования TestFactory методов"""
        try:
            content = file_path.read_text(encoding='utf-8')
        except Exception:
            return []
        
        methods = []
        
        # Паттерны для поиска использования TestFactory методов
        patterns = [
            # factory.CreateMethodName()
            r'(\w+TestFactory)\.(\w+)\(\)',
            # factory.MethodName
            r'(\w+TestFactory)\.(\w+)(?!\()',
            # factory.MethodName(param)
            r'(\w+TestFactory)\.(\w+)\([^)]*\)',
        ]
        
        for pattern in patterns:
            matches = re.finditer(pattern, content)
            for match in matches:
                factory_class = match.group(1)
                method_name = match.group(2)
                
                # Определяем тип возвращаемого значения
                return_type = self._guess_return_type(method_name, content)
                
                # Ищем уже существующий метод
                existing = next((m for m in methods if m.method_name == method_name and m.factory_class == factory_class), None)
                if existing:
                    existing.usage_count += 1
                else:
                    methods.append(LegacyMethodInfo(
                        method_name=method_name,
                        factory_class=factory_class,
                        return_type=return_type,
                        usage_count=1
                    ))
        
        return methods
    
    def _guess_return_type(self, method_name: str, content: str) -> str:
        """Угадывает тип возвращаемого значения метода"""
        # Простые эвристики для определения типа
        if "Create" in method_name:
            # Извлекаем имя класса из метода Create
            class_name = method_name.replace("Create", "")
            if "WithFake" in class_name:
                class_name = class_name.replace("WithFake", "")
            return class_name
        
        if method_name in ["FakeTelegramClient", "TelegramBotClientWrapperMock"]:
            return "FakeTelegramClient"
        
        if "Mock" in method_name:
            return "Mock"
        
        return "object"
    
    def generate_legacy_methods(self, factory_class: str, legacy_methods: List[LegacyMethodInfo]) -> str:
        """Генерирует код для legacy методов"""
        if not legacy_methods:
            return ""
        
        code_lines = [
            "",
            "    #region Legacy Methods for Backward Compatibility",
            ""
        ]
        
        for method in legacy_methods:
            if method.method_name.startswith("Create"):
                # Методы создания объектов
                if "WithFake" in method.method_name:
                    # Создание с Fake клиентом
                    code_lines.append(f"    public {method.return_type} {method.method_name}()")
                    code_lines.append(f"    {{")
                    code_lines.append(f"        return Create{method.return_type}();")
                    code_lines.append(f"    }}")
                    code_lines.append("")
                else:
                    # Обычные методы создания
                    code_lines.append(f"    public {method.return_type} {method.method_name}()")
                    code_lines.append(f"    {{")
                    code_lines.append(f"        return Create{method.return_type}();")
                    code_lines.append(f"    }}")
                    code_lines.append("")
            else:
                # Свойства и другие методы
                if method.return_type == "FakeTelegramClient":
                    code_lines.append(f"    public FakeTelegramClient {method.method_name} => new FakeTelegramClient();")
                elif method.return_type == "Mock":
                    code_lines.append(f"    public Mock<ITelegramBotClientWrapper> {method.method_name} => BotMock;")
                else:
                    code_lines.append(f"    public {method.return_type} {method.method_name} => Create{method.return_type}();")
                code_lines.append("")
        
        code_lines.append("    #endregion")
        
        return "\n".join(code_lines) 