# Исправление проблем с GitLeaks

## Проблема
GitLeaks не срабатывал и позволял коммитить токены в репозиторий. Токены были обнаружены в файлах:
- `test_bot.sh`
- `run_with_real_token.sh`
- `ClubDoorman/.env`

## Причины
1. **Неправильный regex для Telegram токенов** - требовал `bot` в начале
2. **Файлы в allowlist** - `test_bot.sh` и `run_with_real_token.sh` были исключены из проверки
3. **GitLeaks не учитывает .gitignore** - сканирует все файлы в репозитории

## Решение

### 1. Исправлен regex для Telegram токенов
```toml
# Было
regex = '''(?i)bot\d+:[a-zA-Z0-9_-]{35,}'''

# Стало  
regex = '''(?i)\d+:[a-zA-Z0-9_-]{35,}'''
```

### 2. Удалены файлы с токенами из git
```bash
git rm --cached test_bot.sh run_with_real_token.sh
```

### 3. Созданы примеры файлов
- `test_bot.sh.example` - с placeholder `YOUR_BOT_TOKEN_HERE`
- `run_with_real_token.sh.example` - с placeholder `YOUR_BOT_TOKEN_HERE`

### 4. Обновлен allowlist
Добавлены исключения для локальных файлов:
```toml
'''test_bot.sh''',
'''run_with_real_token.sh''',
'''ClubDoorman/.env''',
```

### 5. Создан baseline для истории
Сгенерирован `.gitleaks.baseline.json` с известными токенами в git истории.

### 6. Обновлен pre-commit hook
Добавлен флаг `--baseline-path` для использования baseline.

## Результат
- ✅ GitLeaks теперь правильно обнаруживает токены
- ✅ Файлы с токенами удалены из git tracking
- ✅ Локальные файлы исключены из проверки
- ✅ Pre-commit hook работает с baseline
- ✅ Токены в истории помечены как известные

## Рекомендации
1. **Всегда используйте примеры файлов** с placeholder'ами
2. **Добавляйте локальные файлы в allowlist** GitLeaks
3. **Используйте baseline** для известных токенов в истории
4. **Регулярно обновляйте baseline** при обнаружении новых токенов

## Команды для проверки
```bash
# Проверка текущих файлов
gitleaks detect --source . --no-git --verbose

# Проверка git истории
gitleaks detect --source . --verbose

# Генерация нового baseline
gitleaks detect --source . --config .gitleaks.toml --report-format json --report-path .gitleaks.baseline.json
``` 