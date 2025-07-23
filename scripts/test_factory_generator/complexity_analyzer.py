#!/usr/bin/env python3
"""
ComplexityAnalyzer - анализатор сложности конструкторов
Автоматически определяет сложность конструкторов и предлагает маркеры
"""

import re
from dataclasses import dataclass
from typing import List, Dict, Set
from enum import Enum


class ComplexityLevel(Enum):
    """Уровни сложности"""
    LOW = "low"
    MEDIUM = "medium"
    HIGH = "high"


class MarkerType(Enum):
    """Типы маркеров"""
    REQUIRES_CONCRETE_MOCK = "requires_concrete_mock"
    REQUIRES_CUSTOM_INITIALIZATION = "requires_custom_initialization"
    REQUIRES_UTILITY = "requires_utility"
    REQUIRES_SPECIAL_HANDLING = "requires_special_handling"


@dataclass
class ComplexityReport:
    """Отчет о сложности конструктора"""
    class_name: str
    namespace: str
    complexity_level: ComplexityLevel
    complexity_score: int
    total_params: int
    concrete_params: int
    interface_params: int
    logger_params: int
    suggested_markers: List[MarkerType]
    reasoning: List[str]
    concrete_types: List[str]
    interface_types: List[str]


class ComplexityAnalyzer:
    """Анализатор сложности конструкторов"""
    
    def __init__(self):
        # Типы, которые всегда требуют моков
        self.always_mock_types = {
            'AiChecks', 'SpamHamClassifier', 'MimicryClassifier',
            'BadMessageManager', 'GlobalStatsManager', 'SuspiciousUsersStorage'
        }
        
        # Типы, которые требуют специальной инициализации
        self.special_init_types = {
            'AiChecks': 'Requires TelegramBotClient and ILogger',
            'SpamHamClassifier': 'Requires ILogger',
            'MimicryClassifier': 'Requires ILogger',
            'SuspiciousUsersStorage': 'Requires ILogger'
        }
        
        # Типы, которые лучше использовать как утилиты
        self.utility_types = {
            'ITelegramBotClientWrapper': 'FakeTelegramClient',
            'ITelegramBotClient': 'FakeTelegramClient'
        }
        
        # Интерфейсы, которые не нужно мокать
        self.interface_exceptions = {
            'ILogger', 'IServiceProvider'
        }
    
    def analyze_constructor(self, class_name: str, namespace: str, 
                          constructor_params: List[dict]) -> ComplexityReport:
        """Анализирует сложность конструктора"""
        
        # Подсчитываем параметры по типам
        concrete_params = [p for p in constructor_params if p.get('is_concrete', False)]
        interface_params = [p for p in constructor_params if p.get('is_interface', False)]
        logger_params = [p for p in constructor_params if p.get('is_logger', False)]
        
        # Извлекаем типы
        concrete_types = [p['type'] for p in concrete_params]
        interface_types = [p['type'] for p in interface_params]
        
        # Вычисляем оценку сложности
        complexity_score = self._calculate_complexity_score(
            len(constructor_params), len(concrete_params), 
            len(interface_params), concrete_types
        )
        
        # Определяем уровень сложности
        complexity_level = self._determine_complexity_level(complexity_score)
        
        # Предлагаем маркеры
        suggested_markers = self._suggest_markers(
            concrete_types, interface_types, complexity_score
        )
        
        # Формируем обоснование
        reasoning = self._generate_reasoning(
            complexity_score, concrete_types, interface_types, 
            len(constructor_params), suggested_markers
        )
        
        return ComplexityReport(
            class_name=class_name,
            namespace=namespace,
            complexity_level=complexity_level,
            complexity_score=complexity_score,
            total_params=len(constructor_params),
            concrete_params=len(concrete_params),
            interface_params=len(interface_params),
            logger_params=len(logger_params),
            suggested_markers=suggested_markers,
            reasoning=reasoning,
            concrete_types=concrete_types,
            interface_types=interface_types
        )
    
    def _calculate_complexity_score(self, total_params: int, concrete_count: int, 
                                  interface_count: int, concrete_types: List[str]) -> int:
        """Вычисляет оценку сложности (0-10)"""
        score = 0
        
        # Базовый счет за количество параметров (улучшено)
        if total_params > 8:
            score += 3
        elif total_params > 5:
            score += 2
        elif total_params > 3:
            score += 1
        
        # Счет за интерфейсы (новое!)
        if interface_count > 5:
            score += 2
        elif interface_count > 3:
            score += 1
        
        # Счет за конкретные классы
        for concrete_type in concrete_types:
            base_type = self._get_base_type(concrete_type)
            if base_type in self.always_mock_types:
                score += 3  # Высокий вес для типов, которые всегда нужно мокать
            elif base_type in self.special_init_types:
                score += 2  # Средний вес для типов со специальной инициализацией
            else:
                score += 1  # Базовый вес для других конкретных типов
        
        # Дополнительные факторы
        if concrete_count > 3:
            score += 1
        
        return min(score, 10)  # Ограничиваем максимумом 10
    
    def _determine_complexity_level(self, score: int) -> ComplexityLevel:
        """Определяет уровень сложности по оценке"""
        if score < 3:
            return ComplexityLevel.LOW
        elif score < 7:
            return ComplexityLevel.MEDIUM
        else:
            return ComplexityLevel.HIGH
    
    def _suggest_markers(self, concrete_types: List[str], interface_types: List[str], 
                        complexity_score: int) -> List[MarkerType]:
        """Предлагает маркеры на основе анализа"""
        markers = []
        
        # Проверяем конкретные типы
        for concrete_type in concrete_types:
            base_type = self._get_base_type(concrete_type)
            if base_type in self.always_mock_types:
                markers.append(MarkerType.REQUIRES_CONCRETE_MOCK)
            if base_type in self.special_init_types:
                markers.append(MarkerType.REQUIRES_CUSTOM_INITIALIZATION)
        
        # Проверяем интерфейсы на утилиты
        for interface_type in interface_types:
            base_type = self._get_base_type(interface_type)
            if base_type in self.utility_types:
                markers.append(MarkerType.REQUIRES_UTILITY)
        
        # Общие маркеры по сложности
        if complexity_score >= 7:
            markers.append(MarkerType.REQUIRES_SPECIAL_HANDLING)
        
        # Убираем дубликаты
        return list(set(markers))
    
    def _generate_reasoning(self, complexity_score: int, concrete_types: List[str], 
                          interface_types: List[str], total_params: int, 
                          suggested_markers: List[MarkerType]) -> List[str]:
        """Генерирует обоснование анализа"""
        reasoning = []
        
        # Общая оценка
        reasoning.append(f"Общая оценка сложности: {complexity_score}/10")
        
        # Анализ параметров
        if total_params > 8:
            reasoning.append(f"Много параметров ({total_params}) - может потребовать кастомной инициализации")
        
        # Анализ конкретных типов
        if concrete_types:
            reasoning.append(f"Конкретные классы ({len(concrete_types)}): {', '.join(concrete_types)}")
            for concrete_type in concrete_types:
                base_type = self._get_base_type(concrete_type)
                if base_type in self.always_mock_types:
                    reasoning.append(f"  - {concrete_type}: рекомендуется мок (сложная инициализация)")
                elif base_type in self.special_init_types:
                    reasoning.append(f"  - {concrete_type}: требует специальной инициализации")
        
        # Анализ интерфейсов
        if interface_types:
            reasoning.append(f"Интерфейсы ({len(interface_types)}): {', '.join(interface_types)}")
            for interface_type in interface_types:
                base_type = self._get_base_type(interface_type)
                if base_type in self.utility_types:
                    reasoning.append(f"  - {interface_type}: рекомендуется использовать {self.utility_types[base_type]}")
        
        # Предлагаемые маркеры
        if suggested_markers:
            marker_names = [marker.value for marker in suggested_markers]
            reasoning.append(f"Предлагаемые маркеры: {', '.join(marker_names)}")
        
        return reasoning
    
    def _get_base_type(self, type_name: str) -> str:
        """Извлекает базовый тип без generic параметров"""
        return re.sub(r'<[^>]+>', '', type_name)
    
    def generate_marker_suggestions(self, report: ComplexityReport) -> str:
        """Генерирует предложения по маркерам в C# коде"""
        if not report.suggested_markers:
            return ""
        
        marker_code = []
        marker_code.append("    // Рекомендуемые маркеры для улучшения DX Tool:")
        
        for marker in report.suggested_markers:
            if marker == MarkerType.REQUIRES_CONCRETE_MOCK:
                marker_code.append("    [TestRequiresConcreteMock]")
            elif marker == MarkerType.REQUIRES_CUSTOM_INITIALIZATION:
                marker_code.append("    [TestRequiresCustomInitialization]")
            elif marker == MarkerType.REQUIRES_UTILITY:
                marker_code.append("    [TestRequiresUtility]")
            elif marker == MarkerType.REQUIRES_SPECIAL_HANDLING:
                marker_code.append("    [TestRequiresSpecialHandling]")
        
        return "\n".join(marker_code)
    
    def print_report(self, report: ComplexityReport):
        """Выводит отчет в консоль"""
        print(f"\n🔍 Анализ сложности: {report.class_name}")
        print("=" * 60)
        print(f"📊 Оценка сложности: {report.complexity_score}/10 ({report.complexity_level.value})")
        print(f"📋 Параметров: {report.total_params} (конкретные: {report.concrete_params}, интерфейсы: {report.interface_params}, логгеры: {report.logger_params})")
        
        if report.concrete_types:
            print(f"🔧 Конкретные классы: {', '.join(report.concrete_types)}")
        if report.interface_types:
            print(f"🔌 Интерфейсы: {', '.join(report.interface_types)}")
        
        print(f"\n💡 Обоснование:")
        for reason in report.reasoning:
            print(f"  - {reason}")
        
        if report.suggested_markers:
            print(f"\n🏷️ Предлагаемые маркеры:")
            for marker in report.suggested_markers:
                print(f"  - {marker.value}")
            
            print(f"\n📝 Код маркеров:")
            print(self.generate_marker_suggestions(report))


def test_complexity_analyzer():
    """Тестирует анализатор сложности"""
    analyzer = ComplexityAnalyzer()
    
    # Тестовые данные для MessageHandler
    test_params = [
        {'type': 'ITelegramBotClientWrapper', 'name': 'bot', 'is_interface': True, 'is_logger': False, 'is_concrete': False},
        {'type': 'IModerationService', 'name': 'moderationService', 'is_interface': True, 'is_logger': False, 'is_concrete': False},
        {'type': 'ICaptchaService', 'name': 'captchaService', 'is_interface': True, 'is_logger': False, 'is_concrete': False},
        {'type': 'IUserManager', 'name': 'userManager', 'is_interface': True, 'is_logger': False, 'is_concrete': False},
        {'type': 'SpamHamClassifier', 'name': 'classifier', 'is_interface': False, 'is_logger': False, 'is_concrete': True},
        {'type': 'BadMessageManager', 'name': 'badMessageManager', 'is_interface': False, 'is_logger': False, 'is_concrete': True},
        {'type': 'AiChecks', 'name': 'aiChecks', 'is_interface': False, 'is_logger': False, 'is_concrete': True},
        {'type': 'GlobalStatsManager', 'name': 'globalStatsManager', 'is_interface': False, 'is_logger': False, 'is_concrete': True},
        {'type': 'IStatisticsService', 'name': 'statisticsService', 'is_interface': True, 'is_logger': False, 'is_concrete': False},
        {'type': 'IServiceProvider', 'name': 'serviceProvider', 'is_interface': True, 'is_logger': False, 'is_concrete': False},
        {'type': 'ILogger<MessageHandler>', 'name': 'logger', 'is_interface': True, 'is_logger': True, 'is_concrete': False},
    ]
    
    report = analyzer.analyze_constructor(
        class_name="MessageHandler",
        namespace="ClubDoorman.Handlers",
        constructor_params=test_params
    )
    
    analyzer.print_report(report)


if __name__ == "__main__":
    test_complexity_analyzer() 