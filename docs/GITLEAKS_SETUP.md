# Настройка GitLeaks для ClubDoorman

## Обзор

GitLeaks - это инструмент для обнаружения утечек секретов в Git репозиториях. В ClubDoorman настроен для обнаружения:
- Telegram Bot токенов
- OpenAI/OpenRouter API ключей
- Общих API ключей и токенов

## Установка

### Ubuntu/Debian
```bash
sudo apt install gitleaks
```

### macOS
```bash
brew install gitleaks
```

### Windows
```bash
# Скачать с https://github.com/gitleaks/gitleaks/releases
```

## Конфигурация

Файл `.gitleaks.toml` содержит настройки для проекта:

```toml
[global]
# Игнорируем тестовые файлы и примеры
allowlist = [
    '''bin/''',
    '''obj/''',
    '''*.example''',
    '''*.sample''',
    '''ClubDoorman.Test/''',
    '''plans/''',
    '''.env.sample''',
    # ... другие исключения
]

# Правила для обнаружения токенов
[[rules]]
id = "telegram-bot-token"
description = "Telegram bot token"
regex = '''(?i)bot\d+:[a-zA-Z0-9_-]{35,}'''
severity = "HIGH"

[[rules]]
id = "openai-api-key"
description = "OpenAI API key"
regex = '''sk-[a-zA-Z0-9]{48}'''
severity = "HIGH"

[[rules]]
id = "openrouter-api-key"
description = "OpenRouter API key"
regex = '''sk-or-v1-[a-zA-Z0-9]{48}'''
severity = "HIGH"
```

## Pre-commit хук

Автоматическая проверка при каждом коммите:

```bash
# Файл: .git/hooks/pre-commit
#!/bin/bash
gitleaks detect --source . --config .gitleaks.toml --verbose
```

## Ручная проверка

### Проверка всего репозитория
```bash
gitleaks detect --source . --config .gitleaks.toml --verbose
```

### Проверка только staged файлов
```bash
gitleaks detect --source . --config .gitleaks.toml --staged
```

### Проверка конкретного коммита
```bash
gitleaks detect --source . --config .gitleaks.toml --commit <commit-hash>
```

## Исключения

Следующие файлы и директории исключены из проверки:

- `bin/`, `obj/` - бинарные файлы
- `*.example`, `*.sample` - примеры конфигураций
- `ClubDoorman.Test/` - тестовые файлы
- `plans/` - планы разработки
- `.env.sample` - примеры переменных окружения
- `scripts/run_tests*.sh` - скрипты тестирования

## Ложные срабатывания

GitLeaks может обнаруживать ложные срабатывания на:
- Тестовые API ключи (`test-api-key-for-tests-only`)
- Примеры токенов (`your-api-key-here`)
- Документацию с примерами

Все такие случаи исключены в конфигурации.

## CI/CD интеграция

### GitHub Actions
```yaml
- name: Run GitLeaks
  uses: gitleaks/gitleaks-action@v2
  env:
    GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    GITLEAKS_ENABLE: true
```

### GitLab CI
```yaml
gitleaks:
  image: zricethezav/gitleaks:latest
  script:
    - gitleaks detect --source . --config .gitleaks.toml --verbose
```

## Мониторинг

### Регулярные проверки
```bash
# Еженедельная проверка всего репозитория
gitleaks detect --source . --config .gitleaks.toml --report-format json --report-path gitleaks-report.json
```

### Уведомления
- Pre-commit хук блокирует коммиты с утечками
- CI/CD пайплайны проверяют каждый push
- Регулярные отчеты отправляются в чат безопасности

## Безопасность

### Рекомендации
1. **Никогда не коммитьте реальные токены**
2. **Используйте переменные окружения**
3. **Регулярно ротируйте API ключи**
4. **Мониторьте логи GitLeaks**

### В случае обнаружения утечки
1. Немедленно отозвать скомпрометированный токен
2. Создать новый токен
3. Обновить переменные окружения
4. Проверить историю коммитов на другие утечки

## Поддержка

- Документация: https://github.com/gitleaks/gitleaks
- Правила: https://github.com/gitleaks/gitleaks-rules
- Обсуждения: https://github.com/gitleaks/gitleaks/discussions 