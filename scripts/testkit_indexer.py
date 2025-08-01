#!/usr/bin/env python3
"""
TestKit Index Generator - Автоматическая генерация детального индекса TestKit
Анализирует все файлы, методы, комментарии и создает машинно-читаемый индекс
"""

import os
import re
import json
from datetime import datetime
from pathlib import Path
from collections import defaultdict
from dataclasses import dataclass, asdict
from typing import List, Dict, Set

@dataclass
class TestKitMethod:
    name: str
    return_type: str
    parameters: List[str]
    description: str
    tags: List[str]
    is_static: bool
    is_generic: bool
    signature: str
    line_number: int

@dataclass
class TestKitComponent:
    file_path: str
    file_name: str
    class_name: str
    class_description: str
    category: str
    methods: List[TestKitMethod]
    lines_count: int

class TestKitIndexer:
    def __init__(self, testkit_path: str):
        self.testkit_path = Path(testkit_path)
        self.components: List[TestKitComponent] = []
        self.all_tags: Set[str] = set()

    def scan_testkit(self):
        """Сканирует все C# файлы в TestKit"""
        if not self.testkit_path.exists():
            print(f"❌ TestKit path not found: {self.testkit_path}")
            return

        cs_files = list(self.testkit_path.rglob("*.cs"))
        cs_files = [f for f in cs_files if not f.name.endswith(".Generated.cs")]
        
        print(f"📁 Found {len(cs_files)} C# files to analyze")

        for file_path in cs_files:
            self.analyze_file(file_path)

    def analyze_file(self, file_path: Path):
        """Анализирует один C# файл"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                lines = content.split('\n')

            relative_path = file_path.relative_to(self.testkit_path)
            
            component = TestKitComponent(
                file_path=str(relative_path),
                file_name=file_path.name,
                class_name=self.extract_class_name(content),
                class_description=self.extract_class_summary(content),
                category=self.determine_category(str(relative_path)),
                methods=[],
                lines_count=len(lines)
            )

            # Анализируем методы
            methods = self.extract_methods(content)
            for method in methods:
                self.all_tags.update(method.tags)
                component.methods.append(method)

            if component.methods or component.class_description:
                self.components.append(component)
                
        except Exception as e:
            print(f"⚠️  Error analyzing {file_path}: {e}")

    def extract_class_name(self, content: str) -> str:
        """Извлекает имя класса"""
        match = re.search(r'public\s+(static\s+)?class\s+(\w+)', content)
        return match.group(2) if match else ""

    def extract_class_summary(self, content: str) -> str:
        """Извлекает summary комментарий класса"""
        # Ищем /// <summary> перед объявлением класса
        pattern = r'/// <summary>\s*\n(.*?)\n.*?/// </summary>\s*\n.*?public\s+(static\s+)?class'
        match = re.search(pattern, content, re.DOTALL)
        if match:
            return re.sub(r'///\s*', '', match.group(1)).strip().replace('\n', ' ')
        return ""

    def extract_methods(self, content: str) -> List[TestKitMethod]:
        """Извлекает все публичные методы"""
        methods = []
        lines = content.split('\n')
        
        # Ищем методы с помощью регулярного выражения
        method_pattern = r'public\s+(static\s+)?(\w+(?:<[^>]+>)?)\s+(\w+)\s*\([^)]*\)'
        
        for i, line in enumerate(lines):
            match = re.search(method_pattern, line)
            if match:
                is_static = match.group(1) is not None
                return_type = match.group(2)
                method_name = match.group(3)
                
                # Пропускаем конструкторы и служебные методы
                if method_name.startswith('_') or method_name == 'Main':
                    continue
                
                # Извлекаем параметры (упрощенно)
                params_match = re.search(r'\(([^)]*)\)', line)
                parameters = []
                if params_match and params_match.group(1).strip():
                    # Упрощенное извлечение параметров
                    params_str = params_match.group(1)
                    parameters = [p.strip() for p in params_str.split(',') if p.strip()]
                
                # Извлекаем summary метода (ищем в предыдущих строках)
                description = self.extract_method_summary(lines, i)
                
                # Определяем теги
                tags = self.determine_tags(method_name, return_type)
                
                method = TestKitMethod(
                    name=method_name,
                    return_type=return_type,
                    parameters=parameters,
                    description=description,
                    tags=tags,
                    is_static=is_static,
                    is_generic='<' in return_type,
                    signature=line.strip(),
                    line_number=i + 1
                )
                
                methods.append(method)
        
        return methods

    def extract_method_summary(self, lines: List[str], method_line: int) -> str:
        """Извлекает summary комментарий метода"""
        # Ищем комментарий в предыдущих строках
        for i in range(max(0, method_line - 10), method_line):
            line = lines[i].strip()
            if '/// <summary>' in line:
                # Собираем весь summary блок
                summary_lines = []
                for j in range(i + 1, method_line):
                    comment_line = lines[j].strip()
                    if '/// </summary>' in comment_line:
                        break
                    if comment_line.startswith('///'):
                        summary_lines.append(comment_line.replace('///', '').strip())
                return ' '.join(summary_lines)
        return ""

    def determine_tags(self, method_name: str, return_type: str) -> List[str]:
        """Определяет теги метода на основе имени и типа возврата"""
        tags = []
        
        # Анализ по имени метода
        name_tags = {
            'Create': 'factory',
            'Build': 'builder', 
            'Mock': 'mock',
            'User': 'user',
            'Message': 'message',
            'Chat': 'chat',
            'Moderation': 'moderation',
            'Captcha': 'captcha',
            'Spam': 'spam',
            'Ban': 'ban',
            'AI': 'ai',
            'Ai': 'ai',
            'Telegram': 'telegram',
            'Valid': 'valid',
            'Invalid': 'invalid',
            'Bad': 'invalid',
            'Realistic': 'bogus',
            'Scenario': 'scenario',
            'Setup': 'setup',
            'Test': 'test-infrastructure',
            'Golden': 'golden-master',
            'Fake': 'fake',
            'Fixture': 'autofixture'
        }
        
        for keyword, tag in name_tags.items():
            if keyword in method_name:
                tags.append(tag)
        
        # Анализ по типу возврата
        return_tags = {
            'Mock': 'mock',
            'User': 'user',
            'Message': 'message',
            'Chat': 'chat',
            'Builder': 'builder',
            'Factory': 'factory',
            'IEnumerable': 'collection',
            'List': 'collection'
        }
        
        for keyword, tag in return_tags.items():
            if keyword in return_type:
                tags.append(tag)
        
        return list(set(tags))  # Убираем дубликаты

    def determine_category(self, relative_path: str) -> str:
        """Определяет категорию файла по пути"""
        path_lower = relative_path.lower()
        
        categories = {
            'builders': 'builders',
            'mocks': 'mocks', 
            'scenarios': 'scenarios',
            'specialized': 'specialized',
            'infra': 'infrastructure',
            'autofixture': 'autofixture',
            'bogus': 'bogus',
            'telegram': 'telegram',
            'goldenmaster': 'golden-master',
            'factories': 'factories'
        }
        
        for keyword, category in categories.items():
            if keyword in path_lower:
                return category
                
        return 'core'

    def generate_markdown_index(self):
        """Генерирует полный Markdown индекс"""
        output_path = self.testkit_path / "INDEX_COMPLETE.md"
        
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write("# TestKit Complete Index - Auto-Generated\n\n")
            f.write(f"**Generated:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
            f.write(f"**Components:** {len(self.components)}\n")
            f.write(f"**Methods:** {sum(len(c.methods) for c in self.components)}\n")
            f.write(f"**Tags:** {len(self.all_tags)}\n\n")
            
            # Группировка по категориям
            by_category = defaultdict(list)
            for comp in self.components:
                by_category[comp.category].append(comp)
            
            f.write("## 📚 Components by Category\n\n")
            
            for category, components in sorted(by_category.items()):
                f.write(f"### 📁 {category.upper()}\n\n")
                
                for comp in sorted(components, key=lambda x: x.file_name):
                    f.write(f"#### 📄 {comp.file_name}\n")
                    if comp.class_description:
                        f.write(f"*{comp.class_description}*\n")
                    f.write(f"**Lines:** {comp.lines_count} | **Methods:** {len(comp.methods)}\n\n")
                    
                    if comp.methods:
                        f.write("**Methods:**\n")
                        for method in sorted(comp.methods, key=lambda x: x.name):
                            f.write(f"- `{method.name}()` → `{method.return_type}`")
                            if method.is_static:
                                f.write(" [static]")
                            f.write("\n")
                            if method.description:
                                f.write(f"  - {method.description}\n")
                            if method.tags:
                                tags_str = ", ".join([f"`{t}`" for t in sorted(method.tags)])
                                f.write(f"  - Tags: {tags_str}\n")
                        f.write("\n")
            
            # Индекс по тегам
            f.write("## 🏷️ Methods by Tags\n\n")
            
            methods_by_tag = defaultdict(list)
            for comp in self.components:
                for method in comp.methods:
                    for tag in method.tags:
                        methods_by_tag[tag].append((method, comp.file_name))
            
            for tag, methods in sorted(methods_by_tag.items()):
                f.write(f"### 🏷️ {tag}\n")
                for method, file_name in sorted(methods, key=lambda x: x[0].name):
                    f.write(f"- `{method.name}()` in *{file_name}*\n")
                f.write("\n")
        
        print(f"📝 Generated {output_path}")

    def generate_json_index(self):
        """Генерирует JSON индекс"""
        output_path = self.testkit_path / "index.json"
        
        index = {
            "generated": datetime.now().isoformat(),
            "summary": {
                "components_count": len(self.components),
                "methods_count": sum(len(c.methods) for c in self.components),
                "tags_count": len(self.all_tags),
                "categories": {cat: len(list(comps)) for cat, comps in 
                             defaultdict(list, {comp.category: [comp] for comp in self.components}).items()}
            },
            "components": [asdict(comp) for comp in self.components],
            "all_tags": sorted(list(self.all_tags))
        }
        
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(index, f, indent=2, ensure_ascii=False)
        
        print(f"📋 Generated {output_path}")

    def generate_ai_friendly_index(self):
        """Генерирует AI-friendly индекс"""
        output_path = self.testkit_path / "AI_INDEX.md"
        
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write("# TestKit AI-Friendly Index\n\n")
            f.write("## Quick Reference for AI Assistants\n\n")
            
            # Наиболее частые паттерны
            f.write("### Most Common Factory Methods:\n\n")
            
            factory_methods = []
            for comp in self.components:
                for method in comp.methods:
                    if 'factory' in method.tags or method.name.startswith('Create'):
                        factory_methods.append(method)
            
            for method in sorted(factory_methods, key=lambda x: x.name)[:30]:
                f.write(f"- `TK.{method.name}()` → `{method.return_type}`")
                if method.description:
                    f.write(f" - {method.description}")
                f.write("\n")
            
            f.write("\n### Tags Dictionary:\n")
            for tag in sorted(self.all_tags):
                count = sum(1 for comp in self.components for method in comp.methods if tag in method.tags)
                f.write(f"- `{tag}`: {count} methods\n")
            
            f.write("\n### Quick Search Guide:\n")
            f.write("- **Create objects:** Look for `factory` tag\n")
            f.write("- **Build complex objects:** Look for `builder` tag\n") 
            f.write("- **Mock dependencies:** Look for `mock` tag\n")
            f.write("- **Test scenarios:** Look for `scenario` tag\n")
            f.write("- **Realistic data:** Look for `bogus` tag\n")
        
        print(f"🤖 Generated {output_path}")

def main():
    import sys
    
    testkit_path = sys.argv[1] if len(sys.argv) > 1 else "ClubDoorman.Test/TestKit"
    indexer = TestKitIndexer(testkit_path)
    
    print("🔍 TestKit Index Generator - Scanning...")
    indexer.scan_testkit()
    
    print("📊 Generating reports...")
    indexer.generate_markdown_index()
    indexer.generate_json_index()
    indexer.generate_ai_friendly_index()
    
    print("✅ TestKit index generated successfully!")
    print(f"📋 Found {len(indexer.components)} components")
    print(f"🏷️  Found {len(indexer.all_tags)} unique tags")
    print(f"📊 Total methods: {sum(len(c.methods) for c in indexer.components)}")

if __name__ == "__main__":
    main()