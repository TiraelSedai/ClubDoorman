# Test Commands - Удобные команды для запуска тестов

## 🎯 **Основные команды**

### **Обычный прогон (без демо-тестов)**
```bash
# С .runsettings (автоматически исключает демо)
dotnet test ClubDoorman.Test --settings ClubDoorman.Test/.runsettings --verbosity minimal

# Или явно через фильтр
dotnet test ClubDoorman.Test --filter="Category!=demo&Category!=Demo" --verbosity minimal
```

### **Только демо-тесты** (когда нужно проверить примеры)
```bash
# Запустить только демо-тесты
dotnet test ClubDoorman.Test --filter="Category=demo" --verbosity minimal

# Демо-тесты с детальным выводом
dotnet test ClubDoorman.Test --filter="Category=demo" --verbosity normal
```

### **Полный прогон** (включая демо)
```bash
# Все тесты кроме реальных API
dotnet test ClubDoorman.Test --filter="Category!=real-api" --verbosity minimal

# Вообще все тесты (включая real-api, если настроены)
dotnet test ClubDoorman.Test --verbosity minimal
```

## ⚡ **Быстрые команды**

### **Только быстрые тесты**
```bash
dotnet test ClubDoorman.Test --filter="Category=fast" --verbosity minimal
```

### **Только unit тесты**
```bash
dotnet test ClubDoorman.Test --filter="Category=unit" --verbosity minimal
```

### **BDD тесты**
```bash
dotnet test ClubDoorman.Test --filter="Category=BDD" --verbosity minimal
```

## 🎭 **Комбинированные фильтры**

### **Быстрые + Unit (без демо)**
```bash
dotnet test ClubDoorman.Test --filter="(Category=fast|Category=unit)&Category!=demo" --verbosity minimal
```

### **Все интеграционные (без демо и real-api)**
```bash
dotnet test ClubDoorman.Test --filter="Category=integration&Category!=demo&Category!=real-api" --verbosity minimal
```

## 📊 **Статистика**

### **Подсчет тестов по категориям**
```bash
# Все тесты
dotnet test ClubDoorman.Test --list-tests | grep -c "TestKit\|Test Methods"

# Без демо
dotnet test ClubDoorman.Test --filter="Category!=demo" --list-tests | grep -c "TestKit\|Test Methods" 

# Только демо
dotnet test ClubDoorman.Test --filter="Category=demo" --list-tests | grep -c "TestKit\|Test Methods"
```

## 🔧 **CI/CD команды**

### **CI Pipeline (рекомендуемая)**
```bash
# Основная команда для CI - исключаем демо и real-api
dotnet test ClubDoorman.Test --filter="Category!=demo&Category!=real-api" --logger trx --results-directory TestResults
```

### **Local Development (рекомендуемая)**
```bash
# Команда для разработки - быстро и без демо
dotnet test ClubDoorman.Test --filter="Category!=demo" --verbosity minimal --no-build
```

## 📝 **Примечания**

- **Демо-тесты** - показывают возможности TestKit, могут быть нестабильными
- **Real-API тесты** - требуют настроенные API ключи 
- **По умолчанию** рекомендуется исключать `Category!=demo`
- **В CI** также исключать `Category!=real-api`

## 🚀 **Алиасы для .bashrc**

Добавьте в `~/.bashrc` для удобства:

```bash
alias test-quick="dotnet test ClubDoorman.Test --filter='Category!=demo' --verbosity minimal --no-build"
alias test-demo="dotnet test ClubDoorman.Test --filter='Category=demo' --verbosity minimal"
alias test-full="dotnet test ClubDoorman.Test --filter='Category!=real-api' --verbosity minimal"
alias test-unit="dotnet test ClubDoorman.Test --filter='Category=unit&Category!=demo' --verbosity minimal"
```

---

**💡 Рекомендация:** Используйте `test-quick` для повседневной разработки!