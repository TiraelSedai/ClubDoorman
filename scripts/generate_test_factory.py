#!/usr/bin/env python3
"""
DX Tool для генерации TestFactory на основе C# конструкторов
Автоматически анализирует конструкторы и создает TestFactory с моками

Модульная версия
"""

import sys
from pathlib import Path

# Добавляем путь к модулям
sys.path.insert(0, str(Path(__file__).parent / "test_factory_generator"))

from test_factory_generator.__main__ import main

if __name__ == "__main__":
    main() 