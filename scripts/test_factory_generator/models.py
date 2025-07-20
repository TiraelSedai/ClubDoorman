"""
Модели данных для TestFactory Generator
"""

from dataclasses import dataclass
from typing import List


@dataclass
class ConstructorParam:
    """Параметр конструктора"""
    type: str
    name: str
    is_interface: bool
    is_logger: bool
    is_concrete: bool


@dataclass
class ClassInfo:
    """Информация о классе"""
    name: str
    namespace: str
    constructor_params: List[ConstructorParam]
    file_path: str 