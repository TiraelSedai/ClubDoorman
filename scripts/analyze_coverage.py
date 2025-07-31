#!/usr/bin/env python3
"""
Скрипт для анализа coverage отчетов ClubDoorman

Использование:
    python3 scripts/analyze_coverage.py <модуль> [путь_к_coverage.xml]

Параметры:
    модуль - имя модуля для анализа (например: MessageHandler, ModerationService, UserManager)

Примеры:
    python3 scripts/analyze_coverage.py MessageHandler
    python3 scripts/analyze_coverage.py ModerationService TestResults/*/coverage.cobertura.xml
    python3 scripts/analyze_coverage.py UserManager TestResults/eb31ed7e-5ff4-40a6-878d-2dd4cc93e02b/coverage.cobertura.xml
"""

import xml.etree.ElementTree as ET
import sys
import glob
import os
from typing import Dict, List, Tuple


def analyze_coverage_file(file_path: str, module_name: str) -> Dict:
    """Анализирует один файл coverage для указанного модуля"""
    try:
        tree = ET.parse(file_path)
        root = tree.getroot()
        
        # Общая статистика
        coverage = root
        lines_valid = int(coverage.get('lines-valid', 0))
        lines_covered = int(coverage.get('lines-covered', 0))
        branches_valid = int(coverage.get('branches-valid', 0))
        branches_covered = int(coverage.get('branches-covered', 0))
        
        line_coverage = (lines_covered / lines_valid * 100) if lines_valid > 0 else 0
        branch_coverage = (branches_covered / branches_valid * 100) if branches_valid > 0 else 0
        
        # Анализ указанного модуля
        module_classes = []
        module_methods = []
        
        for package in root.findall('.//package'):
            for class_elem in package.findall('.//class'):
                class_name = class_elem.get('name', '')
                filename = class_elem.get('filename', '')
                
                # Ищем классы, содержащие имя модуля
                if module_name in class_name:
                    line_rate = float(class_elem.get('line-rate', 0))
                    branch_rate = float(class_elem.get('branch-rate', 0))
                    
                    module_classes.append({
                        'name': class_name,
                        'filename': filename,
                        'line_coverage': line_rate * 100,
                        'branch_coverage': branch_rate * 100
                    })
                    
                    # Анализ методов (для MessageHandler - методы банов)
                    for method in class_elem.findall('.//method'):
                        method_name = method.get('name', '')
                        method_line_rate = float(method.get('line-rate', 0))
                        method_branch_rate = float(method.get('branch-rate', 0))
                        
                        # Специальная логика для MessageHandler
                        if module_name == 'MessageHandler':
                            if any(ban_method in method_name for ban_method in [
                                'BanUserForLongName', 'BanBlacklistedUser', 
                                'AutoBanChannel', 'AutoBan', 'HandleBlacklistBan'
                            ]):
                                module_methods.append({
                                    'name': method_name,
                                    'line_coverage': method_line_rate * 100,
                                    'branch_coverage': method_branch_rate * 100,
                                    'type': 'ban_method'
                                })
                        else:
                            # Для других модулей - все методы
                            module_methods.append({
                                'name': method_name,
                                'line_coverage': method_line_rate * 100,
                                'branch_coverage': method_branch_rate * 100,
                                'type': 'general'
                            })
        
        return {
            'file_path': file_path,
            'module_name': module_name,
            'general': {
                'lines_valid': lines_valid,
                'lines_covered': lines_covered,
                'line_coverage': line_coverage,
                'branches_valid': branches_valid,
                'branches_covered': branches_covered,
                'branch_coverage': branch_coverage
            },
            'module_classes': module_classes,
            'module_methods': module_methods
        }
        
    except Exception as e:
        return {"error": f"Ошибка при анализе {file_path}: {e}"}


def print_analysis(data: Dict):
    """Выводит анализ в красивом формате"""
    if 'error' in data:
        print(f"❌ {data['error']}")
        return
    
    module_name = data['module_name']
    print(f"=== АНАЛИЗ COVERAGE ОТЧЕТА ===")
    print(f"Модуль: {module_name}")
    print(f"Файл: {data['file_path']}")
    print()
    
    # Общая статистика
    general = data['general']
    print(f"ОБЩАЯ СТАТИСТИКА:")
    print(f"Строк кода: {general['lines_valid']}")
    print(f"Покрыто строк: {general['lines_covered']}")
    print(f"Покрытие строк: {general['line_coverage']:.1f}%")
    print(f"Веток: {general['branches_valid']}")
    print(f"Покрыто веток: {general['branches_covered']}")
    print(f"Покрытие веток: {general['branch_coverage']:.1f}%")
    print()
    
    # Классы модуля
    if data['module_classes']:
        print(f"АНАЛИЗ {module_name}:")
        for cls in data['module_classes']:
            print(f"Класс: {cls['name']}")
            print(f"Файл: {cls['filename']}")
            print(f"Покрытие строк: {cls['line_coverage']:.1f}%")
            print(f"Покрытие веток: {cls['branch_coverage']:.1f}%")
            print()
    
    # Методы модуля
    if data['module_methods']:
        if module_name == 'MessageHandler':
            print("МЕТОДЫ БАНОВ:")
        else:
            print("МЕТОДЫ МОДУЛЯ:")
            
        for method in data['module_methods']:
            status = "✅ ГОТОВ" if method['line_coverage'] >= 90 else "⚠️ ЧАСТИЧНО" if method['line_coverage'] >= 70 else "❌ НЕ ГОТОВ"
            print(f"{method['name']}:")
            print(f"  Покрытие строк: {method['line_coverage']:.1f}%")
            print(f"  Покрытие веток: {method['branch_coverage']:.1f}%")
            print(f"  Статус: {status}")
            print()
    
    # Оценка готовности к рефакторингу
    print("ОЦЕНКА ГОТОВНОСТИ К РЕФАКТОРИНГУ:")
    
    # Вычисляем среднее покрытие модуля
    if data['module_classes']:
        avg_line_coverage = sum(cls['line_coverage'] for cls in data['module_classes']) / len(data['module_classes'])
        avg_branch_coverage = sum(cls['branch_coverage'] for cls in data['module_classes']) / len(data['module_classes'])
    else:
        avg_line_coverage = general['line_coverage']
        avg_branch_coverage = general['branch_coverage']
    
    if avg_line_coverage >= 90 and avg_branch_coverage >= 80:
        print(f"✅ {module_name} ГОТОВ К БЕЗОПАСНОМУ РЕФАКТОРИНГУ")
        print(f"- Покрытие строк: {avg_line_coverage:.1f}%")
        print(f"- Покрытие веток: {avg_branch_coverage:.1f}%")
        print("- Рекомендация: можно рефакторить с уверенностью")
    elif avg_line_coverage >= 70 and avg_branch_coverage >= 60:
        print(f"⚠️ {module_name} ЧАСТИЧНО ГОТОВ К РЕФАКТОРИНГУ")
        print(f"- Покрытие строк: {avg_line_coverage:.1f}%")
        print(f"- Покрытие веток: {avg_branch_coverage:.1f}%")
        print("- Рекомендация: добавить тесты перед рефакторингом")
    else:
        print(f"❌ {module_name} НЕ ГОТОВ К РЕФАКТОРИНГУ")
        print(f"- Покрытие строк: {avg_line_coverage:.1f}%")
        print(f"- Покрытие веток: {avg_branch_coverage:.1f}%")
        print("- Рекомендация: значительно улучшить тестовое покрытие")


def find_latest_coverage() -> str:
    """Находит самый свежий coverage файл"""
    pattern = "TestResults/*/coverage.cobertura.xml"
    files = glob.glob(pattern)
    
    if not files:
        return None
    
    # Сортируем по времени модификации
    latest_file = max(files, key=os.path.getmtime)
    return latest_file


def main():
    if len(sys.argv) < 2:
        print("❌ Не указан модуль для анализа")
        print("Использование: python3 scripts/analyze_coverage.py <модуль> [путь_к_coverage.xml]")
        print("Примеры:")
        print("  python3 scripts/analyze_coverage.py MessageHandler")
        print("  python3 scripts/analyze_coverage.py ModerationService")
        return
    
    module_name = sys.argv[1]
    
    if len(sys.argv) > 2:
        # Используем переданный путь
        file_path = sys.argv[2]
        if '*' in file_path:
            # Если передан паттерн, берем самый свежий
            files = glob.glob(file_path)
            if not files:
                print(f"❌ Не найдены файлы по паттерну: {file_path}")
                return
            file_path = max(files, key=os.path.getmtime)
    else:
        # Ищем самый свежий coverage файл
        file_path = find_latest_coverage()
        if not file_path:
            print("❌ Не найден coverage файл. Запустите тесты с coverage:")
            print("   dotnet test --collect:\"XPlat Code Coverage\" --results-directory TestResults")
            return
    
    print(f"📊 Анализируем модуль '{module_name}' в файле: {file_path}")
    print()
    
    data = analyze_coverage_file(file_path, module_name)
    print_analysis(data)


if __name__ == "__main__":
    main() 