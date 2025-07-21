#!/usr/bin/env python3
"""
Анализ покрытия кода и поиск проблемных областей
"""

import xml.etree.ElementTree as ET
import os
import sys
from pathlib import Path
from typing import Dict, List, Tuple

def parse_coverage_report(coverage_file: str) -> Dict:
    """Парсит отчет о покрытии и возвращает статистику"""
    tree = ET.parse(coverage_file)
    root = tree.getroot()
    
    # Общая статистика
    total_lines = int(root.get('lines-valid', 0))
    covered_lines = int(root.get('lines-covered', 0))
    total_branches = int(root.get('branches-valid', 0))
    covered_branches = int(root.get('branches-covered', 0))
    
    # Анализ классов
    uncovered_classes = []
    low_coverage_classes = []
    
    for package in root.findall('.//package'):
        for class_elem in package.findall('.//class'):
            class_name = class_elem.get('name', '')
            filename = class_elem.get('filename', '')
            line_rate = float(class_elem.get('line-rate', 0))
            branch_rate = float(class_elem.get('branch-rate', 0))
            
            # Пропускаем сгенерированные файлы
            if 'obj/' in filename or 'bin/' in filename:
                continue
                
            # Анализируем покрытие
            uncovered_lines = []
            for line_elem in class_elem.findall('.//line'):
                line_num = int(line_elem.get('number', 0))
                hits = int(line_elem.get('hits', 0))
                if hits == 0:
                    uncovered_lines.append(line_num)
            
            if line_rate == 0:
                uncovered_classes.append({
                    'name': class_name,
                    'filename': filename,
                    'line_rate': line_rate,
                    'uncovered_lines': uncovered_lines
                })
            elif line_rate < 0.5:  # Меньше 50% покрытия
                low_coverage_classes.append({
                    'name': class_name,
                    'filename': filename,
                    'line_rate': line_rate,
                    'uncovered_lines': uncovered_lines[:10]  # Первые 10 непокрытых строк
                })
    
    return {
        'total_lines': total_lines,
        'covered_lines': covered_lines,
        'total_branches': total_branches,
        'covered_branches': covered_branches,
        'uncovered_classes': uncovered_classes,
        'low_coverage_classes': low_coverage_classes
    }

def find_latest_coverage_report() -> str:
    """Находит самый свежий отчет о покрытии"""
    coverage_dir = Path('coverage')
    if not coverage_dir.exists():
        raise FileNotFoundError("Директория coverage не найдена")
    
    # Ищем самый свежий файл coverage.cobertura.xml
    latest_report = None
    latest_time = 0
    
    for subdir in coverage_dir.iterdir():
        if subdir.is_dir():
            report_file = subdir / 'coverage.cobertura.xml'
            if report_file.exists():
                mtime = report_file.stat().st_mtime
                if mtime > latest_time:
                    latest_time = mtime
                    latest_report = str(report_file)
    
    if not latest_report:
        raise FileNotFoundError("Отчет о покрытии не найден")
    
    return latest_report

def analyze_complexity_for_testing(classes: List[Dict]) -> List[Dict]:
    """Анализирует классы на предмет сложности тестирования"""
    priority_classes = []
    
    for class_info in classes:
        class_name = class_info['name']
        filename = class_info['filename']
        line_rate = class_info['line_rate']
        
        # Приоритетные классы для тестирования
        priority_keywords = [
            'Service', 'Handler', 'Manager', 'Controller',
            'Moderation', 'Captcha', 'User', 'Message'
        ]
        
        is_priority = any(keyword in class_name for keyword in priority_keywords)
        
        if is_priority and line_rate < 0.8:  # Меньше 80% покрытия
            priority_classes.append({
                'name': class_name,
                'filename': filename,
                'line_rate': line_rate,
                'priority': 'HIGH' if line_rate < 0.3 else 'MEDIUM',
                'uncovered_lines': class_info.get('uncovered_lines', [])
            })
    
    return sorted(priority_classes, key=lambda x: x['line_rate'])

def main():
    try:
        # Находим отчет о покрытии
        coverage_file = find_latest_coverage_report()
        print(f"📊 Анализируем отчет: {coverage_file}")
        
        # Парсим отчет
        stats = parse_coverage_report(coverage_file)
        
        # Выводим общую статистику
        print("\n" + "="*60)
        print("📈 ОБЩАЯ СТАТИСТИКА ПОКРЫТИЯ")
        print("="*60)
        
        line_coverage = (stats['covered_lines'] / stats['total_lines']) * 100 if stats['total_lines'] > 0 else 0
        branch_coverage = (stats['covered_branches'] / stats['total_branches']) * 100 if stats['total_branches'] > 0 else 0
        
        print(f"Строки кода: {stats['covered_lines']}/{stats['total_lines']} ({line_coverage:.1f}%)")
        print(f"Ветки кода: {stats['covered_branches']}/{stats['total_branches']} ({branch_coverage:.1f}%)")
        
        # Анализируем непокрытые классы
        print(f"\n🔴 НЕПОКРЫТЫЕ КЛАССЫ: {len(stats['uncovered_classes'])}")
        print("-" * 60)
        
        for class_info in stats['uncovered_classes'][:10]:  # Показываем первые 10
            print(f"❌ {class_info['name']}")
            print(f"   Файл: {class_info['filename']}")
            print(f"   Покрытие: {class_info['line_rate']*100:.1f}%")
            if class_info['uncovered_lines']:
                print(f"   Непокрытые строки: {class_info['uncovered_lines'][:5]}...")
            print()
        
        # Анализируем классы с низким покрытием
        print(f"\n🟡 КЛАССЫ С НИЗКИМ ПОКРЫТИЕМ: {len(stats['low_coverage_classes'])}")
        print("-" * 60)
        
        for class_info in stats['low_coverage_classes'][:10]:  # Показываем первые 10
            print(f"⚠️  {class_info['name']}")
            print(f"   Файл: {class_info['filename']}")
            print(f"   Покрытие: {class_info['line_rate']*100:.1f}%")
            if class_info['uncovered_lines']:
                print(f"   Непокрытые строки: {class_info['uncovered_lines']}")
            print()
        
        # Анализируем приоритетные классы для тестирования
        all_classes = stats['uncovered_classes'] + stats['low_coverage_classes']
        priority_classes = analyze_complexity_for_testing(all_classes)
        
        print(f"\n🎯 ПРИОРИТЕТНЫЕ КЛАССЫ ДЛЯ ТЕСТИРОВАНИЯ: {len(priority_classes)}")
        print("-" * 60)
        
        for class_info in priority_classes[:15]:  # Показываем топ-15
            priority_icon = "🔴" if class_info['priority'] == 'HIGH' else "🟡"
            print(f"{priority_icon} {class_info['name']}")
            print(f"   Файл: {class_info['filename']}")
            print(f"   Покрытие: {class_info['line_rate']*100:.1f}%")
            print(f"   Приоритет: {class_info['priority']}")
            if class_info['uncovered_lines']:
                print(f"   Непокрытые строки: {class_info['uncovered_lines'][:5]}...")
            print()
        
        # Рекомендации
        print("\n💡 РЕКОМЕНДАЦИИ")
        print("-" * 60)
        
        if priority_classes:
            print("1. Начните с классов с высоким приоритетом (🔴)")
            print("2. Используйте DX-утилиту для генерации базовой структуры тестов")
            print("3. Сфокусируйтесь на бизнес-логике и критических путях")
            print("4. Добавьте интеграционные тесты для сложных сценариев")
        
        if line_coverage < 50:
            print("5. Общее покрытие низкое - рассмотрите добавление unit-тестов")
        
        if branch_coverage < 40:
            print("6. Покрытие веток низкое - добавьте тесты для условных путей")
        
        print(f"\n📁 Полный отчет: {coverage_file}")
        
    except Exception as e:
        print(f"❌ Ошибка при анализе покрытия: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main() 