#!/usr/bin/env python3
"""
Простой тест для DX Tool
"""
import sys
from pathlib import Path

# Добавляем путь к модулям
sys.path.insert(0, str(Path(__file__).parent / "scripts" / "test_factory_generator"))

try:
    from enhanced_dx_tool import EnhancedDXTool
    
    print("✅ Модули импортированы успешно")
    
    # Создаем инструмент
    project_root = Path(__file__).parent
    tool = EnhancedDXTool(project_root)
    
    print("✅ DX Tool создан успешно")
    
    # Тестируем поиск классов
    classes = tool.find_all_classes()
    print(f"✅ Найдено классов: {len(classes)}")
    
    # Тестируем анализ ModerationService
    if "ModerationService" in classes:
        print("✅ ModerationService найден")
        
        # Анализируем класс
        class_info = tool.csharp_analyzer.analyze_class("ModerationService")
        if class_info:
            print(f"✅ Зависимости: {class_info.dependencies}")
            
            # Анализируем интерфейсы
            interface_info = tool.analyze_interfaces("ModerationService")
            if interface_info:
                print(f"✅ Интерфейс найден: {interface_info.name}")
                print(f"✅ Методы: {[m.name for m in interface_info.methods]}")
            else:
                print("❌ Интерфейсы не найдены")
        else:
            print("❌ Класс не проанализирован")
    else:
        print("❌ ModerationService не найден")
    
    print("✅ Тест завершен успешно")
    
except Exception as e:
    print(f"❌ Ошибка: {e}")
    import traceback
    traceback.print_exc() 