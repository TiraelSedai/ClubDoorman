"""
Улучшенный анализатор интерфейсов для DX Tool
Анализирует реальные методы, enum'ы и типы
"""
import re
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple
from dataclasses import dataclass


@dataclass
class MethodInfo:
    """Информация о методе интерфейса"""
    name: str
    return_type: str
    parameters: List[Tuple[str, str]]  # (type, name)
    is_async: bool
    is_optional: bool = False


@dataclass
class EnumInfo:
    """Информация об enum"""
    name: str
    values: List[str]
    namespace: str


@dataclass
class InterfaceInfo:
    """Информация об интерфейсе"""
    name: str
    namespace: str
    methods: List[MethodInfo]
    properties: List[Tuple[str, str]]  # (type, name)


class InterfaceAnalyzer:
    """Анализатор интерфейсов C#"""
    
    def __init__(self, project_root: Path):
        self.project_root = project_root
        self._enum_cache: Dict[str, EnumInfo] = {}
        self._interface_cache: Dict[str, InterfaceInfo] = {}
    
    def analyze_interface(self, interface_path: Path) -> InterfaceInfo:
        """Анализирует интерфейс и возвращает информацию о нем"""
        if str(interface_path) in self._interface_cache:
            return self._interface_cache[str(interface_path)]
        
        content = interface_path.read_text(encoding='utf-8')
        
        # Извлекаем namespace
        namespace_match = re.search(r'namespace\s+([^;]+)', content)
        namespace = namespace_match.group(1).strip() if namespace_match else "ClubDoorman"
        
        # Извлекаем имя интерфейса
        interface_match = re.search(r'public\s+interface\s+(\w+)', content)
        interface_name = interface_match.group(1) if interface_match else "Unknown"
        
        # Анализируем методы
        methods = self._extract_methods(content)
        
        # Анализируем свойства
        properties = self._extract_properties(content)
        
        interface_info = InterfaceInfo(
            name=interface_name,
            namespace=namespace,
            methods=methods,
            properties=properties
        )
        
        self._interface_cache[str(interface_path)] = interface_info
        return interface_info
    
    def analyze_enum(self, enum_path: Path) -> EnumInfo:
        """Анализирует enum и возвращает информацию о нем"""
        if str(enum_path) in self._enum_cache:
            return self._enum_cache[str(enum_path)]
        
        content = enum_path.read_text(encoding='utf-8')
        
        # Извлекаем namespace
        namespace_match = re.search(r'namespace\s+([^;]+)', content)
        namespace = namespace_match.group(1).strip() if namespace_match else "ClubDoorman"
        
        # Извлекаем имя enum
        enum_match = re.search(r'public\s+enum\s+(\w+)', content)
        enum_name = enum_match.group(1) if enum_match else "Unknown"
        
        # Извлекаем значения
        values = self._extract_enum_values(content)
        
        enum_info = EnumInfo(
            name=enum_name,
            values=values,
            namespace=namespace
        )
        
        self._enum_cache[str(enum_path)] = enum_info
        return enum_info
    
    def _extract_methods(self, content: str) -> List[MethodInfo]:
        """Извлекает методы из содержимого интерфейса"""
        methods = []
        
        # Паттерн для методов (включая async и кортежи)
        method_pattern = r"""
            (?P<async>async\s+)?
            (?P<return_type>[^)]+?)\s+
            (?P<name>\w+)\s*
            \(
            (?P<parameters>[^)]*)
            \)
        """
        
        for match in re.finditer(method_pattern, content, re.VERBOSE | re.MULTILINE):
            async_keyword = match.group('async')
            return_type = match.group('return_type').strip()
            method_name = match.group('name')
            parameters_str = match.group('parameters')
            
            # Парсим параметры
            parameters = self._parse_parameters(parameters_str)
            
            # Определяем async
            is_async = bool(async_keyword) or 'Task<' in return_type or return_type == 'Task'
            
            # Обрабатываем кортежи в возвращаемом типе
            return_type = self._normalize_return_type(return_type)
            
            method_info = MethodInfo(
                name=method_name,
                return_type=return_type,
                parameters=parameters,
                is_async=is_async
            )
            
            methods.append(method_info)
        
        return methods
    
    def _extract_properties(self, content: str) -> List[Tuple[str, str]]:
        """Извлекает свойства из содержимого интерфейса"""
        properties = []
        
        # Паттерн для свойств
        property_pattern = r'(?P<type>[^;]+?)\s+(?P<name>\w+)\s*\{\s*get;\s*(?:set;\s*)?\}'
        
        for match in re.finditer(property_pattern, content):
            prop_type = match.group('type').strip()
            prop_name = match.group('name')
            
            properties.append((prop_type, prop_name))
        
        return properties
    
    def _extract_enum_values(self, content: str) -> List[str]:
        """Извлекает значения enum"""
        values = []
        
        # Паттерн для значений enum
        value_pattern = r'(\w+)(?:\s*=\s*[^,]+)?,?'
        
        # Ищем блок enum
        enum_block_match = re.search(r'\{([^}]+)\}', content)
        if enum_block_match:
            enum_block = enum_block_match.group(1)
            
            for match in re.finditer(value_pattern, enum_block):
                value = match.group(1).strip()
                if value and not value.startswith('//'):
                    values.append(value)
        
        return values
    
    def _parse_parameters(self, parameters_str: str) -> List[Tuple[str, str]]:
        """Парсит параметры метода"""
        if not parameters_str.strip():
            return []
        
        parameters = []
        param_parts = parameters_str.split(',')
        
        for part in param_parts:
            part = part.strip()
            if not part:
                continue
            
            # Обрабатываем необязательные параметры
            if '=' in part:
                part = part.split('=')[0].strip()
            
            # Извлекаем тип и имя
            words = part.split()
            if len(words) >= 2:
                param_type = ' '.join(words[:-1])
                param_name = words[-1]
                parameters.append((param_type, param_name))
        
        return parameters
    
    def _normalize_return_type(self, return_type: str) -> str:
        """Нормализует возвращаемый тип (обрабатывает кортежи)"""
        # Убираем лишние пробелы
        return_type = return_type.strip()
        
        # Обрабатываем кортежи
        if '(' in return_type and ')' in return_type:
            # Это кортеж, оставляем как есть
            return return_type
        
        # Обрабатываем Task<T>
        if return_type.startswith('Task<') and return_type.endswith('>'):
            inner_type = return_type[5:-1]  # Убираем Task< и >
            return inner_type
        
        return return_type
    
    def find_interface_file(self, interface_name: str) -> Optional[Path]:
        """Находит файл интерфейса по имени"""
        # Ищем в Services
        services_dir = self.project_root / "ClubDoorman" / "Services"
        if services_dir.exists():
            interface_file = services_dir / f"I{interface_name}.cs"
            if interface_file.exists():
                return interface_file
        
        # Ищем в других директориях
        for root, dirs, files in os.walk(self.project_root / "ClubDoorman"):
            for file in files:
                if file == f"I{interface_name}.cs":
                    return Path(root) / file
        
        return None
    
    def find_enum_file(self, enum_name: str) -> Optional[Path]:
        """Находит файл enum по имени"""
        # Ищем в Models
        models_dir = self.project_root / "ClubDoorman" / "Models"
        if models_dir.exists():
            for file in models_dir.glob("*.cs"):
                content = file.read_text(encoding='utf-8')
                if f"enum {enum_name}" in content:
                    return file
        
        # Ищем в других директориях
        for root, dirs, files in os.walk(self.project_root / "ClubDoorman"):
            for file in files:
                if file.endswith(".cs"):
                    file_path = Path(root) / file
                    content = file_path.read_text(encoding='utf-8')
                    if f"enum {enum_name}" in content:
                        return file_path
        
        return None
    
    def get_method_mock_setup(self, method_info: MethodInfo, interface_name: str) -> str:
        """Генерирует настройку мока для метода"""
        method_name = method_info.name
        return_type = method_info.return_type
        parameters = method_info.parameters
        
        # Генерируем параметры для мока
        mock_params = []
        call_params = []
        
        for param_type, param_name in parameters:
            # Для необязательных параметров добавляем null
            if param_name.endswith('?') or '=' in param_name:
                mock_params.append(f"It.IsAny<{param_type}>()")
                call_params.append("null")
            else:
                mock_params.append(f"It.IsAny<{param_type}>()")
                call_params.append(f"It.IsAny<{param_type}>()")
        
        mock_params_str = ", ".join(mock_params)
        call_params_str = ", ".join(call_params)
        
        # Генерируем возвращаемое значение
        if return_type == "void":
            return_value = ""
        elif return_type == "bool":
            return_value = ".Returns(true)"
        elif return_type == "int":
            return_value = ".Returns(1)"
        elif return_type == "string":
            return_value = '.Returns("test")'
        elif return_type == "double":
            return_value = ".Returns(0.5)"
        elif return_type == "float":
            return_value = ".Returns(0.5f)"
        elif "(" in return_type and ")" in return_type:
            # Кортеж
            if "bool" in return_type and "float" in return_type:
                return_value = ".ReturnsAsync((false, 0.2f))"
            else:
                return_value = ".ReturnsAsync((\"test\", 1))"
        elif method_info.is_async:
            if return_type == "Task":
                return_value = ".Returns(Task.CompletedTask)"
            else:
                return_value = f".ReturnsAsync(new {return_type}())"
        else:
            return_value = f".Returns(new {return_type}())"
        
        return f"""
        mock.Setup(x => x.{method_name}({mock_params_str}))
            {return_value};"""
    
    def get_enum_values(self, enum_name: str) -> List[str]:
        """Получает значения enum"""
        enum_file = self.find_enum_file(enum_name)
        if enum_file:
            enum_info = self.analyze_enum(enum_file)
            return enum_info.values
        
        # Fallback значения для известных enum'ов
        fallback_values = {
            "ModerationAction": ["Allow", "Delete", "Ban", "Report", "RequireManualReview"],
            "ChatType": ["Private", "Group", "Supergroup", "Channel"],
            "MessageType": ["Text", "Photo", "Video", "Document"]
        }
        
        return fallback_values.get(enum_name, [])


# Импорт для os.walk
import os 