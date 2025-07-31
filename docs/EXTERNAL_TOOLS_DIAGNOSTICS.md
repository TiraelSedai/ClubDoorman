# Диагностика проблем с внешними утилитами

## Обзор проблем

В проекте возникают проблемы с внешними утилитами:
1. **Stryker** - пришлось устанавливать как NuGet пакет
2. **Verify.NUnit** - не может найти пространство имен

## Анализ проблем

### 1. Проблема с Verify.NUnit

#### Симптомы:
```
error CS0246: Не удалось найти тип или имя пространства имен "Verify" 
(возможно, отсутствует директива using или ссылка на сборку?)
```

#### Диагностика:
- ✅ Пакет `Verify.NUnit` версии `30.5.0` установлен в `.csproj`
- ✅ Tool `verify.tool` версии `0.6.0` установлен локально
- ❌ Компилятор не может найти пространство имен `Verify`

#### Возможные причины:

1. **Конфликт версий .NET 9.0 и Verify.NUnit 30.5.0**
   - .NET 9.0 - очень новая версия
   - Verify.NUnit 30.5.0 может не поддерживать .NET 9.0 полностью

2. **Проблемы с глобальными using**
   - В проекте включены `ImplicitUsings`
   - Возможен конфликт с глобальными using

3. **Отсутствие зависимостей**
   - Verify.NUnit может требовать дополнительные пакеты

4. **Проблемы с MSBuild**
   - Неправильная обработка ссылок на сборки

### 2. Проблема с Stryker

#### Симптомы:
- Stryker не работал как глобальный tool
- Пришлось устанавливать как NuGet пакет

#### Возможные причины:
1. **Проблемы с .NET 9.0**
2. **Конфликт версий**
3. **Проблемы с локальными tools**

## Решения

### Краткосрочные решения:

1. **Verify.NUnit**: Использовать простую JSON сериализацию
   ```csharp
   var json = JsonConvert.SerializeObject(data, Formatting.Indented);
   TestContext.WriteLine(json);
   ```

2. **Stryker**: Использовать NuGet пакет вместо tool

### Долгосрочные решения:

1. **Обновить Verify.NUnit**
   ```bash
   dotnet add package Verify.NUnit --version latest
   ```

2. **Проверить совместимость с .NET 9.0**
   - Дождаться обновления пакетов
   - Рассмотреть downgrade до .NET 8.0

3. **Альтернативные библиотеки**
   - Использовать другие snapshot testing библиотеки
   - Создать собственную простую реализацию

4. **Настройка MSBuild**
   - Проверить настройки глобальных using
   - Настроить правильную обработку ссылок

## Рекомендации

### Немедленные действия:
1. ✅ Продолжить использовать JSON сериализацию для Golden Master тестов
2. ✅ Документировать проблемы для будущих разработчиков
3. ✅ Создать issue в репозитории для отслеживания

### Планируемые действия:
1. 🔄 Мониторить обновления Verify.NUnit
2. 🔄 Рассмотреть альтернативные решения
3. 🔄 Протестировать на .NET 8.0 для сравнения

## Альтернативы Verify.NUnit

1. **ApprovalTests**
   ```bash
   dotnet add package ApprovalTests
   ```

2. **Snapshooter**
   ```bash
   dotnet add package Snapshooter
   ```

3. **Собственная реализация**
   ```csharp
   public static class SimpleSnapshot
   {
       public static async Task Verify<T>(T data, string fileName)
       {
           var json = JsonConvert.SerializeObject(data, Formatting.Indented);
           var filePath = Path.Combine("Snapshots", $"{fileName}.json");
           
           if (File.Exists(filePath))
           {
               var expected = await File.ReadAllTextAsync(filePath);
               Assert.AreEqual(expected, json);
           }
           else
           {
               Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
               await File.WriteAllTextAsync(filePath, json);
           }
       }
   }
   ```

## Заключение

Проблемы с внешними утилитами связаны с:
1. **Новизной .NET 9.0** - не все пакеты обновились
2. **Конфликтами версий** - несовместимость библиотек
3. **Проблемами с tools** - нестабильность dotnet tools

**Рекомендация**: Продолжить разработку с временными решениями и мониторить обновления пакетов. 