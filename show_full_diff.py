#!/usr/bin/env python3
import re

def extract_full_method(file_path, method_name):
    """Извлекает полный метод из файла"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Ищем метод по имени (включая сигнатуру)
    pattern = rf'public async Task {method_name}\(.*?\)\s*\{{(.*?)\n\s*\}}'
    match = re.search(pattern, content, re.DOTALL)
    
    if match:
        return match.group(0)  # Возвращаем весь метод
    return None

def extract_internal_method(file_path, method_name):
    """Извлекает internal метод из файла"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Ищем internal метод по имени
    pattern = rf'internal async Task {method_name}\(.*?\)\s*\{{(.*?)\n\s*\}}'
    match = re.search(pattern, content, re.DOTALL)
    
    if match:
        return match.group(0)  # Возвращаем весь метод
    return None

def show_full_diff():
    """Показывает полный diff методов"""
    
    print("🔍 ПОЛНЫЙ DIFF МЕТОДОВ BanUserForLongName")
    print("=" * 80)
    
    # Извлекаем методы
    mh_method = extract_internal_method('ClubDoorman/Handlers/MessageHandler.cs', 'BanUserForLongName')
    ubs_method = extract_full_method('ClubDoorman/Services/UserBanService.cs', 'BanUserForLongNameAsync')
    
    if not mh_method:
        print("❌ Не найден метод в MessageHandler")
        return
    
    if not ubs_method:
        print("❌ Не найден метод в UserBanService")
        return
    
    print("📊 СТАТИСТИКА:")
    print(f"   MessageHandler: {len(mh_method)} символов")
    print(f"   UserBanService: {len(ubs_method)} символов")
    print()
    
    print("📋 MESSAGEHANDLER (ОРИГИНАЛ):")
    print("-" * 40)
    print(mh_method)
    print()
    
    print("📋 USERBANSERVICE (МИГРИРОВАННЫЙ):")
    print("-" * 40)
    print(ubs_method)
    print()
    
    # Нормализуем для сравнения
    mh_clean = re.sub(r'internal async Task', 'public async Task', mh_method)
    mh_clean = re.sub(r'BanUserForLongName', 'BanUserForLongNameAsync', mh_clean)
    
    # Убираем различия в именах переменных
    mh_clean = re.sub(r'_\w+\.', '_.', mh_clean)
    ubs_clean = re.sub(r'_\w+\.', '_.', ubs_method) # Corrected: use ubs_method here
    
    # Убираем пробелы и переносы для точного сравнения
    mh_normalized = re.sub(r'\s+', ' ', mh_clean).strip()
    ubs_normalized = re.sub(r'\s+', ' ', ubs_clean).strip()
    
    if mh_normalized == ubs_normalized:
        print("✅ ЛОГИКА ПОЛНОСТЬЮ СОВПАДАЕТ!")
        print("   (исключая имена переменных и модификаторы доступа)")
    else:
        print("❌ ЛОГИКА НЕ СОВПАДАЕТ!")
        
        # Показываем различия по строкам
        mh_lines = mh_normalized.split(';')
        ubs_lines = ubs_normalized.split(';')
        
        print("\n📋 РАЗЛИЧИЯ:")
        for i, (mh_line, ubs_line) in enumerate(zip(mh_lines, ubs_lines)):
            if mh_line.strip() != ubs_line.strip():
                print(f"   Строка {i+1}:")
                print(f"     MH:  {mh_line.strip()}")
                print(f"     UBS: {ubs_line.strip()}")
                print()

if __name__ == "__main__":
    show_full_diff() 