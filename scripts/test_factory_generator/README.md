# TestFactory Generator - Модульная структура

## Обзор

Модульная версия TestFactory Generator разбита на логические компоненты для лучшей организации кода и упрощения поддержки.

## Структура модулей

```
scripts/test_factory_generator/
├── __init__.py              # Инициализация пакета
├── __main__.py              # Точка входа (CLI)
├── models.py                # Модели данных
├── csharp_analyzer.py       # Анализатор C# кода
├── factory_generator.py     # Генератор TestFactory
├── utils.py                 # Утилиты
└── README.md               # Эта документация
```

## Модули

### models.py
Содержит модели данных:
- `ConstructorParam` - параметр конструктора
- `ClassInfo` - информация о классе

### csharp_analyzer.py
Анализатор C# кода:
- `CSharpAnalyzer` - основной класс анализатора
- Поиск сервисных классов
- Парсинг конструкторов
- Определение типов параметров

### factory_generator.py
Генератор TestFactory:
- `TestFactoryGenerator` - основной класс генератора
- Генерация TestFactory кода
- Генерация тестов для TestFactory
- Сохранение файлов

### utils.py
Утилиты:
- `to_pascal_case()` - конвертация в PascalCase
- `split_params()` - разбиение параметров
- `is_interface_type()` - определение интерфейсов
- `get_base_type()` - получение базового типа
- `clean_constructor_params()` - очистка параметров

### __main__.py
Точка входа для CLI:
- Парсинг аргументов командной строки
- Координация работы анализатора и генератора
- Вывод результатов

## Использование

### Как модуль
```python
from test_factory_generator import CSharpAnalyzer, TestFactoryGenerator

analyzer = CSharpAnalyzer("./project")
services = analyzer.find_service_classes()

generator = TestFactoryGenerator(analyzer.test_project_root)
for service in services:
    factory_code = generator.generate_test_factory(service)
    tests_code = generator.generate_test_factory_tests(service)
    generator.save_files(service, factory_code, tests_code)
```

### Как CLI
```bash
# Через основной скрипт
python3 scripts/generate_test_factory_new.py . --force

# Или напрямую через модуль
python3 -m scripts.test_factory_generator . --force
```

## Преимущества модульной структуры

1. **Разделение ответственности** - каждый модуль отвечает за свою область
2. **Переиспользование** - модули можно использовать независимо
3. **Тестируемость** - каждый модуль можно тестировать отдельно
4. **Расширяемость** - легко добавлять новые функции
5. **Читаемость** - код организован логически

## Совместимость

Модульная версия полностью совместима с оригинальной версией и генерирует идентичный код. Для проверки совместимости используйте:

```bash
python3 scripts/verify_modular_version.py
```

## Разработка

При добавлении новых функций:

1. Определите, в какой модуль они должны попасть
2. Добавьте функции в соответствующий модуль
3. Обновите `__init__.py` если нужно экспортировать новые классы
4. Добавьте тесты для новых функций
5. Обновите документацию

## Тестирование

Для тестирования модулей:

```python
# Тест анализатора
from test_factory_generator.csharp_analyzer import CSharpAnalyzer
analyzer = CSharpAnalyzer("./test_project")
services = analyzer.find_service_classes()
assert len(services) > 0

# Тест генератора
from test_factory_generator.factory_generator import TestFactoryGenerator
generator = TestFactoryGenerator(Path("./test_project/Test"))
code = generator.generate_test_factory(services[0])
assert "TestFactory" in code
``` 