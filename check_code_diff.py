#!/usr/bin/env python3
import re

def extract_method_content(file_path, method_name):
    """Извлекает содержимое метода из файла"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Ищем метод по имени
    pattern = rf'{method_name}\s*\([^)]*\)\s*\{{(.*?)\n\s*\}}'
    match = re.search(pattern, content, re.DOTALL)
    
    if match:
        return match.group(1).strip()
    return None

def normalize_code(code):
    """Нормализует код для сравнения (убирает пробелы, комментарии и т.д.)"""
    if not code:
        return ""
    
    # Убираем комментарии
    code = re.sub(r'//.*$', '', code, flags=re.MULTILINE)
    code = re.sub(r'/\*.*?\*/', '', code, flags=re.DOTALL)
    
    # Убираем лишние пробелы и переносы строк
    code = re.sub(r'\s+', ' ', code)
    code = code.strip()
    
    return code

def compare_methods():
    """Сравнивает методы BanUserForLongName"""
    
    # Извлекаем методы
    mh_method = extract_method_content('ClubDoorman/Handlers/MessageHandler.cs', 'BanUserForLongName')
    ubs_method = extract_method_content('ClubDoorman/Services/UserBanService.cs', 'BanUserForLongName')
    
    if not mh_method:
        print("❌ Не найден метод BanUserForLongName в MessageHandler")
        return
    
    if not ubs_method:
        print("❌ Не найден метод BanUserForLongName в UserBanService")
        return
    
    # Нормализуем код
    mh_normalized = normalize_code(mh_method)
    ubs_normalized = normalize_code(ubs_method)
    
    print("🔍 Сравнение методов BanUserForLongName:")
    print("=" * 60)
    
    # Убираем различия в именах переменных (logger, bot, messageService, userFlowLogger)
    mh_clean = re.sub(r'_\w+\.', '_.', mh_normalized)
    ubs_clean = re.sub(r'_\w+\.', '_.', ubs_normalized)
    
    if mh_clean == ubs_clean:
        print("✅ КОД СОВПАДАЕТ ДО БУКВОЧКИ!")
        print("   (исключая имена переменных)")
    else:
        print("❌ КОД НЕ СОВПАДАЕТ!")
        print("\n📋 Различия:")
        
        # Показываем различия
        mh_lines = mh_clean.split(';')
        ubs_lines = ubs_clean.split(';')
        
        for i, (mh_line, ubs_line) in enumerate(zip(mh_lines, ubs_lines)):
            if mh_line.strip() != ubs_line.strip():
                print(f"   Строка {i+1}:")
                print(f"     MH:  {mh_line.strip()}")
                print(f"     UBS: {ubs_line.strip()}")
                print()

if __name__ == "__main__":
    compare_methods() 