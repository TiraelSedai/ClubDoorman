#!/usr/bin/env python3
"""
Скрипт для проверки идентичности модульной и оригинальной версий TestFactory Generator
"""

import subprocess
import tempfile
import shutil
from pathlib import Path


def run_generator(script_path: str, project_root: str, force: bool = True) -> str:
    """Запускает генератор и возвращает вывод"""
    cmd = [script_path, project_root]
    if force:
        cmd.append("--force")
    
    result = subprocess.run(cmd, capture_output=True, text=True, cwd=project_root)
    return result.stdout


def compare_generated_files(original_output: str, modular_output: str) -> bool:
    """Сравнивает вывод генераторов"""
    # Убираем временные метки и пути, которые могут отличаться
    def clean_output(output: str) -> str:
        lines = output.split('\n')
        cleaned = []
        for line in lines:
            # Пропускаем строки с временными метками и путями
            if any(skip in line for skip in ['🔍 Анализируем проект:', '📦 Найдено', '🚀 Генерируем', '✅ Генерация завершена', '📝 Следующие шаги']):
                continue
            if line.strip():
                cleaned.append(line.strip())
        return '\n'.join(cleaned)
    
    return clean_output(original_output) == clean_output(modular_output)


def main():
    """Основная функция проверки"""
    project_root = Path.cwd()
    original_script = project_root / "scripts" / "generate_test_factory.py"
    modular_script = project_root / "scripts" / "generate_test_factory_new.py"
    
    print("🔍 Проверяем идентичность модульной и оригинальной версий...")
    
    # Запускаем оригинальную версию
    print("📦 Запускаем оригинальную версию...")
    original_output = run_generator(str(original_script), str(project_root))
    
    # Запускаем модульную версию
    print("📦 Запускаем модульную версию...")
    modular_output = run_generator(str(modular_script), str(project_root))
    
    # Сравниваем вывод
    print("🔍 Сравниваем результаты...")
    if compare_generated_files(original_output, modular_output):
        print("✅ Модульная версия работает идентично оригинальной!")
        return True
    else:
        print("❌ Обнаружены различия между версиями!")
        print("\nОригинальная версия:")
        print(original_output)
        print("\nМодульная версия:")
        print(modular_output)
        return False


if __name__ == "__main__":
    success = main()
    exit(0 if success else 1) 