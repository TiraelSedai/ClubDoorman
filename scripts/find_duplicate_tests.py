#!/usr/bin/env python3
"""
Duplicate Test Finder - находит потенциально дублирующие тесты
"""

import os
import re
import difflib
from collections import defaultdict
import json

def find_duplicate_tests(test_dir="ClubDoorman.Test"):
    """Находит потенциально дублирующие тесты"""
    print("🔍 Scanning for duplicate tests...")
    
    test_methods = {}
    
    # Сканируем все тестовые файлы
    for root, dirs, files in os.walk(test_dir):
        for file in files:
            if file.endswith(".cs") and "Test" in file:
                filepath = os.path.join(root, file)
                analyze_test_file(filepath, test_methods)
    
    print(f"📊 Found {len(test_methods)} test methods")
    
    # Ищем дубли
    duplicates = find_similar_tests(test_methods)
    
    print(f"🔍 Found {len(duplicates)} potential duplicates")
    return duplicates, test_methods

def analyze_test_file(filepath, test_methods):
    """Анализирует файл на предмет тестовых методов"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except Exception as e:
        print(f"❌ Error reading {filepath}: {e}")
        return
        
    # Ищем тестовые методы (более гибкий паттерн)
    test_pattern = r'\[Test\][\s\S]*?public[\s\S]*?void\s+(\w+)\s*\([^)]*\)\s*\{([\s\S]*?)(?=\n\s*(?:\[Test\]|\[.*\]|public|private|protected|internal|\}|$))'
    matches = re.findall(test_pattern, content, re.MULTILINE)
    
    for method_name, method_body in matches:
        # Нормализуем тело метода (убираем лишние пробелы, комментарии)
        normalized_body = normalize_method_body(method_body)
        
        test_methods[f"{filepath}::{method_name}"] = {
            'name': method_name,
            'body': normalized_body,
            'file': filepath,
            'original_body': method_body.strip()
        }

def normalize_method_body(body):
    """Нормализует тело метода для сравнения"""
    # Убираем комментарии
    body = re.sub(r'//.*', '', body)
    body = re.sub(r'/\*.*?\*/', '', body, flags=re.DOTALL)
    
    # Убираем лишние пробелы и переносы
    body = re.sub(r'\s+', ' ', body)
    
    # Убираем строковые литералы (могут отличаться)
    body = re.sub(r'"[^"]*"', '"STRING"', body)
    
    # Убираем числовые литералы
    body = re.sub(r'\b\d+\b', 'NUMBER', body)
    
    return body.strip()

def find_similar_tests(test_methods):
    """Находит похожие тесты"""
    duplicates = []
    methods = list(test_methods.items())
    
    print("🔍 Comparing test methods for similarity...")
    
    for i in range(len(methods)):
        for j in range(i + 1, len(methods)):
            key1, method1 = methods[i]
            key2, method2 = methods[j]
            
            # Пропускаем сравнение методов из одного файла с очень похожими именами
            if method1['file'] == method2['file'] and \
               difflib.SequenceMatcher(None, method1['name'], method2['name']).ratio() > 0.8:
                continue
            
            # Сравниваем тела методов
            similarity = difflib.SequenceMatcher(
                None, method1['body'], method2['body']
            ).ratio()
            
            if similarity > 0.75:  # 75% похожести
                duplicates.append({
                    'method1': key1,
                    'method2': key2,
                    'similarity': similarity,
                    'file1': method1['file'],
                    'file2': method2['file'],
                    'name1': method1['name'],
                    'name2': method2['name']
                })
    
    # Сортируем по убыванию похожести
    duplicates.sort(key=lambda x: x['similarity'], reverse=True)
    
    return duplicates

def categorize_duplicates(duplicates):
    """Категоризирует дубли по типам"""
    categories = {
        'exact_duplicates': [],      # > 95% похожести
        'high_similarity': [],       # 90-95% похожести  
        'medium_similarity': [],     # 80-90% похожести
        'cross_file_duplicates': [], # Дубли между разными файлами
        'same_file_variants': []     # Варианты в одном файле
    }
    
    for dup in duplicates:
        similarity = dup['similarity']
        
        if similarity > 0.95:
            categories['exact_duplicates'].append(dup)
        elif similarity > 0.90:
            categories['high_similarity'].append(dup)
        elif similarity > 0.80:
            categories['medium_similarity'].append(dup)
            
        if dup['file1'] != dup['file2']:
            categories['cross_file_duplicates'].append(dup)
        else:
            categories['same_file_variants'].append(dup)
    
    return categories

def generate_report(duplicates, test_methods, output_file="duplicate_tests_report.md"):
    """Генерирует отчет о дублях"""
    categories = categorize_duplicates(duplicates)
    
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write("# 🔍 Duplicate Tests Analysis Report\n\n")
        f.write(f"**Generated:** {__import__('datetime').datetime.now()}\n\n")
        
        f.write("## 📊 Summary\n\n")
        f.write(f"- **Total test methods:** {len(test_methods)}\n")
        f.write(f"- **Potential duplicates:** {len(duplicates)}\n")
        f.write(f"- **Exact duplicates (>95%):** {len(categories['exact_duplicates'])}\n")
        f.write(f"- **High similarity (90-95%):** {len(categories['high_similarity'])}\n")
        f.write(f"- **Cross-file duplicates:** {len(categories['cross_file_duplicates'])}\n\n")
        
        # Exact duplicates (приоритет для удаления)
        if categories['exact_duplicates']:
            f.write("## 🚨 Exact Duplicates (Priority for removal)\n\n")
            for dup in categories['exact_duplicates']:
                f.write(f"### {dup['similarity']:.1%} similarity\n")
                f.write(f"- **File 1:** `{dup['file1']}::{dup['name1']}`\n")
                f.write(f"- **File 2:** `{dup['file2']}::{dup['name2']}`\n")
                f.write(f"- **Recommendation:** Remove one of them\n\n")
        
        # Cross-file duplicates
        if categories['cross_file_duplicates']:
            f.write("## 🔄 Cross-File Duplicates\n\n")
            for dup in categories['cross_file_duplicates'][:10]:  # Top 10
                f.write(f"### {dup['similarity']:.1%} similarity\n")
                f.write(f"- **File 1:** `{dup['file1']}::{dup['name1']}`\n")
                f.write(f"- **File 2:** `{dup['file2']}::{dup['name2']}`\n\n")
        
        # High similarity
        if categories['high_similarity']:
            f.write("## ⚠️ High Similarity Tests\n\n")
            for dup in categories['high_similarity'][:10]:  # Top 10
                f.write(f"### {dup['similarity']:.1%} similarity\n")
                f.write(f"- **File 1:** `{dup['file1']}::{dup['name1']}`\n")
                f.write(f"- **File 2:** `{dup['file2']}::{dup['name2']}`\n\n")
    
    print(f"📝 Report generated: {output_file}")

def main():
    """Главная функция"""
    print("🔍 Duplicate Test Finder")
    print("=" * 50)
    
    duplicates, test_methods = find_duplicate_tests()
    
    if duplicates:
        print("\n🚨 Top 10 most similar tests:")
        for i, dup in enumerate(duplicates[:10], 1):
            print(f"{i:2d}. {dup['similarity']:.1%} - {dup['name1']} <-> {dup['name2']}")
            print(f"    {dup['file1']}")
            print(f"    {dup['file2']}")
            print()
    
    # Генерируем отчет
    generate_report(duplicates, test_methods, "plans/current/test-reorganization/duplicate_tests_report.md")
    
    # Сохраняем данные в JSON для дальнейшего анализа
    with open("plans/current/test-reorganization/duplicate_tests_data.json", 'w', encoding='utf-8') as f:
        json.dump({
            'duplicates': duplicates,
            'summary': {
                'total_methods': len(test_methods),
                'total_duplicates': len(duplicates),
                'exact_duplicates': len([d for d in duplicates if d['similarity'] > 0.95])
            }
        }, f, indent=2, ensure_ascii=False)
    
    print("✅ Analysis complete!")

if __name__ == "__main__":
    main()