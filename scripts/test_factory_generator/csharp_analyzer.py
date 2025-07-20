"""
Анализатор C# кода для извлечения информации о классах и конструкторах
"""

import re
from pathlib import Path
from typing import List

from .models import ClassInfo, ConstructorParam


class CSharpAnalyzer:
    """Анализатор C# кода"""
    
    def __init__(self, project_root: str, force_overwrite: bool = False):
        self.project_root = Path(project_root)
        self.test_project_root = self.project_root / "ClubDoorman.Test"
        self.force_overwrite = force_overwrite
        
    def find_service_classes(self) -> List[ClassInfo]:
        """Находит все сервисные классы с конструкторами"""
        services = []
        
        # Ищем в папке Services
        services_dir = self.project_root / "ClubDoorman" / "Services"
        if services_dir.exists():
            for cs_file in services_dir.glob("*.cs"):
                classes = self._parse_file(cs_file)
                services.extend(classes)
        
        # Ищем в папке Handlers
        handlers_dir = self.project_root / "ClubDoorman" / "Handlers"
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
        class_pattern = r'public\s+(?:sealed\s+)?class\s+(\w+)[^{]*?{.*?public\s+\1\s*\(([^)]*)\)[^{]*?{'
        class_matches = re.finditer(class_pattern, content, re.DOTALL)
        
        for match in class_matches:
            class_name = match.group(1)
            constructor_params_str = match.group(2)
            
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