"""
Анализатор C# кода для извлечения информации о классах и конструкторах
"""

import re
from pathlib import Path
from typing import List, Dict, Optional

try:
    from .models import ClassInfo, ConstructorParam
    from .complexity_analyzer import ComplexityAnalyzer, ComplexityReport
except ImportError:
    from models import ClassInfo, ConstructorParam
    from complexity_analyzer import ComplexityAnalyzer, ComplexityReport


class CSharpAnalyzer:
    """Анализатор C# кода"""
    
    def __init__(self, project_root: str, force_overwrite: bool = False):
        self.project_root = Path(project_root)
        self.test_project_root = self.project_root / "ClubDoorman.Test"
        self.force_overwrite = force_overwrite
        self.complexity_analyzer = ComplexityAnalyzer()
        
    def find_service_classes(self) -> List[ClassInfo]:
        """Находит все сервисные классы с конструкторами"""
        services = []
        
        # Ищем в папке Services
        services_dir = self.project_root / "Services"
        if services_dir.exists():
            for cs_file in services_dir.glob("*.cs"):
                classes = self._parse_file(cs_file)
                services.extend(classes)
        
        # Ищем в папке Handlers
        handlers_dir = self.project_root / "Handlers"
        if handlers_dir.exists():
            for cs_file in handlers_dir.glob("*.cs"):
                classes = self._parse_file(cs_file)
                services.extend(classes)
        
        return services
    
    def _parse_file(self, file_path: Path) -> List[ClassInfo]:
        """Парсит C# файл и извлекает информацию о классах"""
        classes = []
        
        try:
            content = file_path.read_text(encoding='utf-8')
        except Exception as e:
            print(f"Ошибка чтения файла {file_path}: {e}")
            return classes
        
        # Ищем namespace
        namespace_match = re.search(r'namespace\s+(\S+)', content)
        namespace = namespace_match.group(1) if namespace_match else "Unknown"
        # Убираем лишние точки с запятой
        namespace = namespace.rstrip(';')
        
        # Ищем классы с конструкторами
        class_pattern = r'public\s+(?:sealed\s+)?class\s+(\w+)'
        class_matches = re.finditer(class_pattern, content)
        
        for class_match in class_matches:
            class_name = class_match.group(1)
            
            # Ищем конструктор для этого класса
            constructor_pattern = rf'public\s+{class_name}\s*\(([^)]*)\)'
            constructor_match = re.search(constructor_pattern, content)
            
            if constructor_match:
                constructor_params_str = constructor_match.group(1)
                
                # Парсим параметры конструктора
                params = self._parse_constructor_params(constructor_params_str)
                
                if params:  # Только если есть параметры
                    classes.append(ClassInfo(
                        name=class_name,
                        namespace=namespace,
                        constructor_params=params,
                        file_path=str(file_path.relative_to(self.project_root))
                    ))
        
        return classes
    
    def _parse_constructor_params(self, params_str: str) -> List[ConstructorParam]:
        """Парсит параметры конструктора"""
        params = []
        
        # Убираем комментарии и лишние пробелы
        params_str = re.sub(r'//.*$', '', params_str, flags=re.MULTILINE)
        params_str = params_str.strip()
        
        if not params_str:
            return params
        
        # Разбиваем по запятой, учитывая generic типы
        param_parts = self._split_params(params_str)
        
        for part in param_parts:
            if not part:
                continue
                
            # Ищем тип и имя параметра
            param_match = re.search(r'(\w+(?:<[^>]+>)?)\s+(\w+)', part)
            if param_match:
                param_type = param_match.group(1)
                param_name = param_match.group(2)
                
                # Улучшенная логика определения типов
                is_interface = self._is_interface_type(param_type)
                is_logger = 'ILogger' in param_type
                is_concrete = not is_interface and not is_logger
                
                params.append(ConstructorParam(
                    type=param_type,
                    name=param_name,
                    is_interface=is_interface,
                    is_logger=is_logger,
                    is_concrete=is_concrete
                ))
        
        return params
    
    def _split_params(self, params_str: str) -> List[str]:
        """Разбивает параметры с учетом generic типов"""
        parts = []
        current_part = ""
        bracket_count = 0
        
        for char in params_str:
            if char == '<':
                bracket_count += 1
            elif char == '>':
                bracket_count -= 1
            elif char == ',' and bracket_count == 0:
                parts.append(current_part.strip())
                current_part = ""
                continue
            
            current_part += char
        
        if current_part.strip():
            parts.append(current_part.strip())
        
        return parts
    
    def _is_interface_type(self, type_name: str) -> bool:
        """Определяет, является ли тип интерфейсом"""
        # Расширенный список известных интерфейсов
        known_interfaces = {
            'ITelegramBotClient', 'IUserManager', 'IModerationService', 
            'ICaptchaService', 'IStatisticsService', 'IUpdateDispatcher',
            'IUpdateHandler', 'ICommandHandler', 'ISpamHamClassifier',
            'IMimicryClassifier', 'IBadMessageManager', 'IAiChecks',
            'ISuspiciousUsersStorage', 'ILogger', 'IServiceProvider',
            'IEnumerable', 'ICollection', 'IList', 'IDictionary',
            'IComparable', 'IDisposable', 'IAsyncDisposable',
            'IEquatable', 'IFormattable', 'IConvertible'
        }
        
        # Убираем generic параметры для проверки
        base_type = re.sub(r'<[^>]+>', '', type_name)
        
        # Проверяем по паттерну I + заглавная буква
        if base_type.startswith('I') and len(base_type) > 1 and base_type[1:2].isupper():
            return True
        
        # Проверяем по списку известных интерфейсов
        return base_type in known_interfaces
    
    def analyze_class_complexity(self, class_info: ClassInfo) -> ComplexityReport:
        """Анализирует сложность класса"""
        # Конвертируем ConstructorParam в dict для анализатора
        params_dict = []
        for param in class_info.constructor_params:
            params_dict.append({
                'type': param.type,
                'name': param.name,
                'is_interface': param.is_interface,
                'is_logger': param.is_logger,
                'is_concrete': param.is_concrete
            })
        
        return self.complexity_analyzer.analyze_constructor(
            class_name=class_info.name,
            namespace=class_info.namespace,
            constructor_params=params_dict
        )
    
    def find_test_markers(self, file_path: Path) -> Dict[str, List[str]]:
        """Находит тестовые маркеры в файле"""
        markers = {}
        
        try:
            content = file_path.read_text(encoding='utf-8')
        except Exception as e:
            print(f"Ошибка чтения файла {file_path}: {e}")
            return markers
        
        # Ищем маркеры TestRequiresConcreteMock
        concrete_mock_pattern = r'\[TestRequiresConcreteMock\("([^"]+)"(?:,\s*"([^"]+)")*\)\]'
        concrete_matches = re.finditer(concrete_mock_pattern, content)
        for match in concrete_matches:
            class_name = self._extract_class_name_from_file(content)
            if class_name:
                markers[class_name] = ['TestRequiresConcreteMock']
        
        # Ищем маркеры TestRequiresCustomInitialization
        custom_init_pattern = r'\[TestRequiresCustomInitialization\("([^"]*)"\)\]'
        custom_matches = re.finditer(custom_init_pattern, content)
        for match in custom_matches:
            class_name = self._extract_class_name_from_file(content)
            if class_name:
                if class_name not in markers:
                    markers[class_name] = []
                markers[class_name].append('TestRequiresCustomInitialization')
        
        # Ищем маркеры TestRequiresUtility
        utility_pattern = r'\[TestRequiresUtility\("([^"]+)"\)\]'
        utility_matches = re.finditer(utility_pattern, content)
        for match in utility_matches:
            class_name = self._extract_class_name_from_file(content)
            if class_name:
                if class_name not in markers:
                    markers[class_name] = []
                markers[class_name].append('TestRequiresUtility')
        
        return markers
    
    def _extract_class_name_from_file(self, content: str) -> Optional[str]:
        """Извлекает имя класса из содержимого файла"""
        class_pattern = r'public\s+(?:sealed\s+)?class\s+(\w+)'
        match = re.search(class_pattern, content)
        return match.group(1) if match else None
    
    def get_enhanced_class_info(self, class_info: ClassInfo) -> Dict:
        """Получает расширенную информацию о классе с анализом сложности и маркерами"""
        # Анализируем сложность
        complexity_report = self.analyze_class_complexity(class_info)
        
        # Ищем маркеры в файле
        file_path = self.project_root / class_info.file_path
        markers = self.find_test_markers(file_path)
        
        return {
            'class_info': class_info,
            'complexity_report': complexity_report,
            'test_markers': markers.get(class_info.name, []),
            'suggested_markers': [marker.value for marker in complexity_report.suggested_markers]
        } 