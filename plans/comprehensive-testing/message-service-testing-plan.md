# План тестирования MessageService

## Проблемы, которые нужно решить

### 1. MessageTemplates не является интерфейсом
- **Проблема**: MessageTemplates - это класс, а не интерфейс, что не позволяет его мокать
- **Решение**: Создать интерфейс IMessageTemplates и заставить MessageTemplates его реализовывать
- **Альтернатива**: Использовать реальный экземпляр MessageTemplates в тестах

### 2. Сложные зависимости
- **Проблема**: MessageService зависит от множества сервисов (ITelegramBotClientWrapper, ILoggingConfigurationService, IServiceChatDispatcher)
- **Решение**: Создать комплексную TestFactory с моками всех зависимостей

### 3. Telegram Bot API типы
- **Проблема**: Некоторые типы Telegram Bot API имеют read-only свойства
- **Решение**: Использовать конструкторы или фабричные методы для создания тестовых объектов

## Методы для тестирования

### Основные методы:
1. `SendAdminNotificationAsync` - отправка админских уведомлений
2. `SendLogNotificationAsync` - отправка лог-уведомлений  
3. `SendUserNotificationAsync` - отправка пользовательских уведомлений
4. `SendWelcomeMessageAsync` - отправка приветственных сообщений
5. `SendCaptchaMessageAsync` - отправка капчи
6. `ForwardToAdminWithNotificationAsync` - пересылка в админ-чат
7. `ForwardToLogWithNotificationAsync` - пересылка в лог-чат
8. `SendErrorNotificationAsync` - отправка уведомлений об ошибках
9. `SendAiProfileAnalysisAsync` - отправка AI анализа профиля

### Сценарии тестирования:
- Успешная отправка сообщений
- Обработка ошибок Telegram Bot API
- Проверка конфигурации (включены/выключены уведомления)
- Проверка отмены операций (CancellationToken)
- Проверка различных типов уведомлений
- Проверка форматирования шаблонов

## Приоритет
**Низкий** - MessageService является вспомогательным сервисом, основная логика уже покрыта тестами в других компонентах.

## Зависимости
- Создание интерфейса IMessageTemplates
- Обновление MessageService для использования интерфейса
- Создание комплексной TestFactory 