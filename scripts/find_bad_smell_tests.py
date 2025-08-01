#!/usr/bin/env python3
"""
Bad Smell Test Detector - находит тесты с плохими паттернами
"""

import os
import re
import json

BAD_SMELL_PATTERNS = {
    'line_coverage_only': {
        'description': 'Тесты, которые только проверяют что код не падает',
        'patterns': [
            r'Assert\.DoesNotThrow\s*\(',
            r'Assert\.That\([^,]+,\s*Is\.Not\.Null\s*\)',
            r'new \w+\([^)]*\);\s*$',  # Только создание объекта без проверок
            r'Assert\.DoesNotThrow\s*\(\s*\(\)\s*=>'
        ]
    },
    'mock_verification_only': {
        'description': 'Тесты, которые проверяют только вызовы моков, а не результат',
        'patterns': [
            r'\.Verify\s*\(',
            r'Times\.Once\b',
            r'Times\.Never\b',
            r'Times\.AtLeast\b',
            r'Times\.Exactly\b'
        ]
    },
    'property_assignment_only': {
        'description': 'Тесты, которые проверяют только присваивание свойств',
        'patterns': [
            r'\w+\.\w+\s*=\s*[^;]+;\s*Assert\.That\([^.]+\.\w+,\s*Is\.EqualTo\(',
            r'Assert\.That\([^.]+\.\w+,\s*Is\.EqualTo\([^)]+\)\s*\);?\s*$'
        ]
    },
    'constructor_only': {
        'description': 'Тесты, которые проверяют только что конструктор работает',
        'patterns': [
            r'Assert\.DoesNotThrow\s*\(\s*\(\)\s*=>\s*new\s+\w+\s*\(',
            r'var\s+\w+\s*=\s*new\s+\w+\([^)]*\);\s*Assert\.That\(\w+,\s*Is\.Not\.Null\)'
        ]
    },
    'object_mutation': {
        'description': 'Тесты с мутациями объектов после создания (кандидаты на builders)',
        'patterns': [
            r'\w+\.Text\s*=\s*[^;]+;',
            r'\w+\.From\s*=\s*[^;]+;',
            r'\w+\.Chat\s*=\s*[^;]+;',
            r'\w+\.IsBot\s*=\s*[^;]+;',
            r'\w+\.FirstName\s*=\s*[^;]+;',
            r'\w+\.Id\s*=\s*[^;]+;'
        ]
    }
}

def find_bad_smell_tests(test_dir="ClubDoorman.Test"):
    """Находит тесты с плохими паттернами"""
    print("🔍 Scanning for bad smell tests...")
    
    bad_smells = []
    
    for root, dirs, files in os.walk(test_dir):
        for file in files:
            if file.endswith(".cs") and "Test" in file:
                filepath = os.path.join(root, file)
                smells = analyze_file_for_smells(filepath)
                bad_smells.extend(smells)
    
    print(f"🚩 Found {len(bad_smells)} bad smell instances")
    return bad_smells

def analyze_file_for_smells(filepath):
    """Анализирует файл на bad smells"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except Exception as e:
        print(f"❌ Error reading {filepath}: {e}")
        return []
    
    smells = []
    
    # Находим все тестовые методы
    test_pattern = r'\[Test\][\s\S]*?public[\s\S]*?void\s+(\w+)\s*\([^)]*\)\s*\{([\s\S]*?)(?=\n\s*(?:\[Test\]|\[.*\]|public|private|protected|internal|\}|$))'
    matches = re.findall(test_pattern, content, re.MULTILINE)
    
    for method_name, method_body in matches:
        # Проверяем каждый паттерн bad smell
        for smell_type, smell_config in BAD_SMELL_PATTERNS.items():
            for pattern in smell_config['patterns']:
                if re.search(pattern, method_body, re.MULTILINE):
                    smells.append({
                        'file': filepath,
                        'method': method_name,
                        'smell_type': smell_type,
                        'description': smell_config['description'],
                        'pattern': pattern,
                        'severity': get_smell_severity(smell_type, method_body)
                    })
    
    return smells

def get_smell_severity(smell_type, method_body):
    """Определяет серьезность bad smell"""
    # Подсчитываем количество Assert'ов
    assert_count = len(re.findall(r'Assert\.', method_body))
    
    # Подсчитываем строки кода (примерно)
    code_lines = len([line for line in method_body.split('\n') if line.strip() and not line.strip().startswith('//')])
    
    if smell_type == 'line_coverage_only':
        return 'HIGH' if assert_count <= 1 else 'MEDIUM'
    elif smell_type == 'mock_verification_only':
        return 'HIGH' if assert_count <= 2 and 'Verify' in method_body else 'MEDIUM'
    elif smell_type == 'object_mutation':
        mutation_count = len(re.findall(r'\w+\.\w+\s*=', method_body))
        return 'HIGH' if mutation_count >= 3 else 'MEDIUM'
    else:
        return 'MEDIUM'

def categorize_smells(smells):
    """Категоризирует bad smells"""
    categories = {}
    
    for smell in smells:
        smell_type = smell['smell_type']
        if smell_type not in categories:
            categories[smell_type] = []
        categories[smell_type].append(smell)
    
    # Сортируем по серьезности
    for category in categories.values():
        category.sort(key=lambda x: (x['severity'] == 'HIGH', x['file'], x['method']))
    
    return categories

def generate_report(smells, output_file="bad_smell_tests_report.md"):
    """Генерирует отчет о bad smells"""
    categories = categorize_smells(smells)
    
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write("# 🚩 Bad Smell Tests Analysis Report\n\n")
        f.write(f"**Generated:** {__import__('datetime').datetime.now()}\n\n")
        
        f.write("## 📊 Summary\n\n")
        f.write(f"- **Total bad smell instances:** {len(smells)}\n")
        
        for smell_type, smell_list in categories.items():
            high_severity = len([s for s in smell_list if s['severity'] == 'HIGH'])
            f.write(f"- **{smell_type}:** {len(smell_list)} ({high_severity} high severity)\n")
        
        f.write("\n")
        
        # Детальный анализ по категориям
        for smell_type, smell_list in categories.items():
            config = BAD_SMELL_PATTERNS[smell_type]
            f.write(f"## 🚩 {smell_type.replace('_', ' ').title()}\n\n")
            f.write(f"**Description:** {config['description']}\n\n")
            f.write(f"**Count:** {len(smell_list)}\n\n")
            
            # Группируем по файлам
            by_file = {}
            for smell in smell_list:
                file = smell['file']
                if file not in by_file:
                    by_file[file] = []
                by_file[file].append(smell)
            
            for file, file_smells in sorted(by_file.items()):
                f.write(f"### {file}\n\n")
                for smell in file_smells:
                    severity_icon = "🔴" if smell['severity'] == 'HIGH' else "🟡"
                    f.write(f"- {severity_icon} **{smell['method']}**\n")
                f.write("\n")
    
    print(f"📝 Report generated: {output_file}")

def find_migration_candidates(smells):
    """Находит кандидатов для миграции на builders"""
    mutation_smells = [s for s in smells if s['smell_type'] == 'object_mutation']
    
    # Группируем по файлам
    by_file = {}
    for smell in mutation_smells:
        file = smell['file']
        if file not in by_file:
            by_file[file] = []
        by_file[file].append(smell)
    
    # Сортируем файлы по количеству мутаций
    candidates = []
    for file, file_smells in by_file.items():
        candidates.append({
            'file': file,
            'mutation_count': len(file_smells),
            'methods': [s['method'] for s in file_smells],
            'priority': 'HIGH' if len(file_smells) >= 5 else 'MEDIUM'
        })
    
    candidates.sort(key=lambda x: x['mutation_count'], reverse=True)
    return candidates

def main():
    """Главная функция"""
    print("🚩 Bad Smell Test Detector")
    print("=" * 50)
    
    smells = find_bad_smell_tests()
    
    if smells:
        categories = categorize_smells(smells)
        
        print("\n📊 Bad Smell Summary:")
        for smell_type, smell_list in categories.items():
            high_count = len([s for s in smell_list if s['severity'] == 'HIGH'])
            print(f"  {smell_type}: {len(smell_list)} ({high_count} high severity)")
        
        # Показываем топ файлов с мутациями (кандидаты на миграцию)
        candidates = find_migration_candidates(smells)
        if candidates:
            print(f"\n🔄 Top migration candidates (object mutations):")
            for i, candidate in enumerate(candidates[:5], 1):
                print(f"  {i}. {candidate['file']}: {candidate['mutation_count']} mutations")
                print(f"     Methods: {', '.join(candidate['methods'][:3])}{'...' if len(candidate['methods']) > 3 else ''}")
    
    # Генерируем отчет
    generate_report(smells, "plans/current/test-reorganization/bad_smell_tests_report.md")
    
    # Сохраняем кандидатов на миграцию
    candidates = find_migration_candidates(smells)
    with open("plans/current/test-reorganization/migration_candidates.json", 'w', encoding='utf-8') as f:
        json.dump(candidates, f, indent=2, ensure_ascii=False)
    
    print("✅ Analysis complete!")

if __name__ == "__main__":
    main()