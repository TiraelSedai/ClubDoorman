#!/usr/bin/env python3
"""
TestKit Duplication & Complexity Analyzer
Анализирует TestKit на предмет переусложнения, дублирования и возможностей оптимизации
"""

import json
import re
from collections import defaultdict, Counter
from pathlib import Path
from typing import Dict, List, Set, Tuple
from dataclasses import dataclass

@dataclass
class DuplicationPattern:
    pattern: str
    methods: List[str]
    files: List[str] 
    severity: str  # "high", "medium", "low"
    optimization_potential: str

@dataclass
class ComplexityIssue:
    component: str
    issue_type: str
    description: str
    methods_count: int
    lines_count: int
    optimization_suggestion: str

class TestKitAnalyzer:
    def __init__(self, index_path: str):
        self.index_path = Path(index_path)
        self.index_data = None
        self.duplications: List[DuplicationPattern] = []
        self.complexity_issues: List[ComplexityIssue] = []
        
    def load_index(self):
        """Загружает JSON индекс"""
        with open(self.index_path, 'r', encoding='utf-8') as f:
            self.index_data = json.load(f)
    
    def analyze_method_name_patterns(self):
        """Анализирует паттерны в именах методов для поиска дублирования"""
        method_patterns = defaultdict(list)
        
        for component in self.index_data['components']:
            for method in component['methods']:
                # Извлекаем базовые паттерны из имен методов
                base_patterns = self.extract_base_patterns(method['name'])
                for pattern in base_patterns:
                    method_patterns[pattern].append({
                        'method': method['name'],
                        'file': component['file_name'],
                        'return_type': method['return_type']
                    })
        
        # Ищем дублирование
        for pattern, methods in method_patterns.items():
            if len(methods) > 2:  # Больше 2х методов с похожим паттерном
                severity = self.assess_duplication_severity(methods)
                if severity != "ignore":
                    self.duplications.append(DuplicationPattern(
                        pattern=pattern,
                        methods=[m['method'] for m in methods],
                        files=list(set(m['file'] for m in methods)),
                        severity=severity,
                        optimization_potential=self.suggest_optimization(pattern, methods)
                    ))
    
    def extract_base_patterns(self, method_name: str) -> List[str]:
        """Извлекает базовые паттерны из имени метода"""
        patterns = []
        
        # Паттерны создания объектов
        if method_name.startswith('Create'):
            # CreateValidUser, CreateBotUser -> Create*User
            if 'User' in method_name:
                patterns.append('Create*User')
            if 'Message' in method_name:
                patterns.append('Create*Message')
            if 'Chat' in method_name:
                patterns.append('Create*Chat')
            if 'Mock' in method_name:
                patterns.append('Create*Mock')
            if 'Service' in method_name:
                patterns.append('Create*Service')
            if 'Factory' in method_name:
                patterns.append('Create*Factory')
                
        # Паттерны билдеров
        if 'Builder' in method_name:
            patterns.append('*Builder')
            
        # Паттерны результатов
        if 'Result' in method_name:
            patterns.append('*Result')
            
        return patterns
    
    def assess_duplication_severity(self, methods: List[Dict]) -> str:
        """Оценивает серьезность дублирования"""
        # Если методы в разных файлах - потенциальное дублирование
        files = set(m['file'] for m in methods)
        if len(files) > 1:
            return "high"
        
        # Если много методов с одинаковым типом возврата - возможно дублирование логики
        return_types = set(m['return_type'] for m in methods)
        if len(return_types) == 1 and len(methods) > 3:
            return "medium"
            
        # Если в одном файле много похожих методов - может быть переусложнение
        if len(files) == 1 and len(methods) > 5:
            return "medium"
            
        return "low"
    
    def suggest_optimization(self, pattern: str, methods: List[Dict]) -> str:
        """Предлагает способ оптимизации"""
        files = set(m['file'] for m in methods)
        
        if len(files) > 1:
            return f"Consolidate {len(methods)} methods from {len(files)} files into single location"
        
        if pattern == 'Create*User':
            return "Consider user builder with enum parameter: CreateUser(UserType.Valid|Bot|Anonymous)"
        elif pattern == 'Create*Message':
            return "Consider message builder with fluent API or message type enum"
        elif pattern == 'Create*Mock':
            return "Consider generic mock factory: CreateMock<T>() with configuration"
        elif pattern == '*Builder':
            return "Check if builders can share common base class or interface"
        else:
            return "Review for consolidation opportunity"
    
    def analyze_component_complexity(self):
        """Анализирует сложность компонентов"""
        for component in self.index_data['components']:
            methods_count = len(component['methods'])
            lines_count = component['lines_count']
            
            # Файлы с большим количеством методов
            if methods_count > 30:
                self.complexity_issues.append(ComplexityIssue(
                    component=component['file_name'],
                    issue_type="high_method_count",
                    description=f"Component has {methods_count} methods",
                    methods_count=methods_count,
                    lines_count=lines_count,
                    optimization_suggestion="Consider splitting into multiple specialized classes"
                ))
            
            # Файлы с большим количеством строк
            if lines_count > 500:
                self.complexity_issues.append(ComplexityIssue(
                    component=component['file_name'],
                    issue_type="high_line_count", 
                    description=f"Component has {lines_count} lines",
                    methods_count=methods_count,
                    lines_count=lines_count,
                    optimization_suggestion="Consider breaking into smaller modules"
                ))
            
            # Низкая плотность методов (много кода на метод)
            if methods_count > 0:
                lines_per_method = lines_count / methods_count
                if lines_per_method > 20:
                    self.complexity_issues.append(ComplexityIssue(
                        component=component['file_name'],
                        issue_type="low_method_density",
                        description=f"Average {lines_per_method:.1f} lines per method",
                        methods_count=methods_count,
                        lines_count=lines_count,
                        optimization_suggestion="Methods might be too complex - consider simplification"
                    ))
    
    def analyze_tag_overlap(self):
        """Анализирует пересечения тегов для поиска дублирования функциональности"""
        tag_to_methods = defaultdict(list)
        
        for component in self.index_data['components']:
            for method in component['methods']:
                for tag in method['tags']:
                    tag_to_methods[tag].append({
                        'method': method['name'],
                        'file': component['file_name'],
                        'component': component['file_name']
                    })
        
        # Ищем теги с большим количеством методов в разных файлах
        for tag, methods in tag_to_methods.items():
            files = set(m['file'] for m in methods)
            if len(files) > 3 and len(methods) > 10:  # Много методов в разных файлах
                self.duplications.append(DuplicationPattern(
                    pattern=f"tag:{tag}",
                    methods=[m['method'] for m in methods[:10]],  # Показываем первые 10
                    files=list(files),
                    severity="medium",
                    optimization_potential=f"Consider consolidating '{tag}' functionality"
                ))
    
    def generate_report(self):
        """Генерирует отчет об анализе"""
        output_path = self.index_path.parent / "DUPLICATION_ANALYSIS.md"
        
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write("# TestKit Duplication & Complexity Analysis\n\n")
            f.write(f"**Generated:** {self.index_data['generated']}\n")
            f.write(f"**Total Components:** {len(self.index_data['components'])}\n")
            f.write(f"**Total Methods:** {sum(len(c['methods']) for c in self.index_data['components'])}\n\n")
            
            # Статистика дублирования
            f.write("## 🔍 Duplication Analysis\n\n")
            if self.duplications:
                high_duplications = [d for d in self.duplications if d.severity == "high"]
                medium_duplications = [d for d in self.duplications if d.severity == "medium"]
                
                f.write(f"**Found {len(self.duplications)} potential duplication patterns:**\n")
                f.write(f"- 🔴 High severity: {len(high_duplications)}\n")
                f.write(f"- 🟡 Medium severity: {len(medium_duplications)}\n\n")
                
                if high_duplications:
                    f.write("### 🔴 High Priority Duplications\n\n")
                    for dup in high_duplications:
                        f.write(f"#### Pattern: `{dup.pattern}`\n")
                        f.write(f"**Files involved:** {', '.join(dup.files)}\n")
                        f.write(f"**Methods ({len(dup.methods)}):** {', '.join(dup.methods[:5])}")
                        if len(dup.methods) > 5:
                            f.write(f" ... and {len(dup.methods) - 5} more")
                        f.write("\n")
                        f.write(f"**Optimization:** {dup.optimization_potential}\n\n")
                
                if medium_duplications:
                    f.write("### 🟡 Medium Priority Duplications\n\n")
                    for dup in medium_duplications:
                        f.write(f"#### Pattern: `{dup.pattern}`\n")
                        f.write(f"**Impact:** {len(dup.methods)} methods in {len(dup.files)} files\n")
                        f.write(f"**Optimization:** {dup.optimization_potential}\n\n")
            else:
                f.write("✅ No significant duplication patterns found!\n\n")
            
            # Анализ сложности
            f.write("## 📊 Complexity Analysis\n\n")
            if self.complexity_issues:
                f.write(f"**Found {len(self.complexity_issues)} complexity issues:**\n\n")
                
                by_type = defaultdict(list)
                for issue in self.complexity_issues:
                    by_type[issue.issue_type].append(issue)
                
                for issue_type, issues in by_type.items():
                    f.write(f"### {issue_type.replace('_', ' ').title()}\n\n")
                    for issue in issues:
                        f.write(f"**{issue.component}**\n")
                        f.write(f"- {issue.description}\n")
                        f.write(f"- Suggestion: {issue.optimization_suggestion}\n\n")
            else:
                f.write("✅ No significant complexity issues found!\n\n")
            
            # Рекомендации
            f.write("## 💡 Optimization Recommendations\n\n")
            
            # Оценка impact vs effort
            total_issues = len(self.duplications) + len(self.complexity_issues)
            if total_issues == 0:
                f.write("### 🎉 TestKit is well-designed!\n")
                f.write("No significant duplication or complexity issues found.\n")
                f.write("Current architecture appears optimal.\n\n")
            else:
                high_impact = len([d for d in self.duplications if d.severity == "high"])
                if high_impact > 0:
                    f.write("### 🔥 High Impact Optimizations\n")
                    f.write(f"**{high_impact} high-priority duplications** found.\n")
                    f.write("**Impact:** Reduced maintenance, clearer API\n")
                    f.write("**Effort:** Medium (refactoring required)\n")
                    f.write("**Recommendation:** Address these first\n\n")
                else:
                    f.write("### ✅ Low Impact Optimizations Only\n")
                    f.write("Most issues found are low-priority.\n")
                    f.write("**Recommendation:** Current design is good, minor tweaks only\n\n")
            
            # Метрики здоровья архитектуры
            f.write("## 📈 Architecture Health Metrics\n\n")
            
            total_methods = sum(len(c['methods']) for c in self.index_data['components'])
            avg_methods_per_component = total_methods / len(self.index_data['components'])
            
            f.write(f"- **Average methods per component:** {avg_methods_per_component:.1f}\n")
            f.write(f"- **Total duplication patterns:** {len(self.duplications)}\n")
            f.write(f"- **Complexity issues:** {len(self.complexity_issues)}\n")
            
            # Оценка общего здоровья
            health_score = 100
            health_score -= len([d for d in self.duplications if d.severity == "high"]) * 10
            health_score -= len([d for d in self.duplications if d.severity == "medium"]) * 5
            health_score -= len(self.complexity_issues) * 3
            
            f.write(f"- **Architecture Health Score:** {max(0, health_score)}/100\n\n")
            
            if health_score >= 85:
                f.write("🟢 **Excellent architecture** - minimal issues\n")
            elif health_score >= 70:
                f.write("🟡 **Good architecture** - some optimization opportunities\n") 
            else:
                f.write("🔴 **Needs optimization** - significant issues found\n")
        
        print(f"📋 Generated duplication analysis: {output_path}")
        return health_score

def main():
    analyzer = TestKitAnalyzer("ClubDoorman.Test/TestKit/index.json")
    
    print("🔍 Loading TestKit index...")
    analyzer.load_index()
    
    print("🔎 Analyzing method name patterns...")
    analyzer.analyze_method_name_patterns()
    
    print("📊 Analyzing component complexity...")
    analyzer.analyze_component_complexity()
    
    print("🏷️ Analyzing tag overlaps...")
    analyzer.analyze_tag_overlap()
    
    print("📝 Generating report...")
    health_score = analyzer.generate_report()
    
    print(f"\n✅ Analysis complete!")
    print(f"🏥 Architecture Health Score: {health_score}/100")
    print(f"🔍 Found {len(analyzer.duplications)} duplication patterns")
    print(f"📊 Found {len(analyzer.complexity_issues)} complexity issues")

if __name__ == "__main__":
    main()