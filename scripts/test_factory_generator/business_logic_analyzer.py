#!/usr/bin/env python3
"""
Анализатор бизнес-логики для умной генерации TestFactory
Анализирует код сервисов и определяет какие методы нужны в TestFactory
"""

import re
from pathlib import Path
from typing import List, Dict, Set
from dataclasses import dataclass
from csharp_analyzer import ClassInfo


@dataclass
class BusinessLogicInfo:
    """Информация о бизнес-логике сервиса"""
    service_name: str
    has_telegram_client: bool
    has_async_methods: bool
    has_moderation_logic: bool
    has_captcha_logic: bool
    has_user_management: bool
    has_statistics: bool
    has_ai_checks: bool
    has_storage: bool
    has_external_apis: bool
    complexity_score: int = 0


class BusinessLogicAnalyzer:
    """Анализатор бизнес-логики сервисов"""
    
    def __init__(self, project_root: Path):
        self.project_root = project_root
        
    def analyze_service_logic(self, service: ClassInfo) -> BusinessLogicInfo:
        """Анализирует бизнес-логику сервиса"""
        info = BusinessLogicInfo(
            service_name=service.name,
            has_telegram_client=False,
            has_async_methods=False,
            has_moderation_logic=False,
            has_captcha_logic=False,
            has_user_management=False,
            has_statistics=False,
            has_ai_checks=False,
            has_storage=False,
            has_external_apis=False
        )
        
        # Анализируем параметры конструктора
        for param in service.constructor_params:
            param_type = param.type.lower()
            
            if "telegram" in param_type or "bot" in param_type:
                info.has_telegram_client = True
                info.complexity_score += 2
                
            if "moderation" in param_type:
                info.has_moderation_logic = True
                info.complexity_score += 3
                
            if "captcha" in param_type:
                info.has_captcha_logic = True
                info.complexity_score += 2
                
            if "user" in param_type and "manager" in param_type:
                info.has_user_management = True
                info.complexity_score += 2
                
            if "statistics" in param_type or "stats" in param_type:
                info.has_statistics = True
                info.complexity_score += 1
                
            if "ai" in param_type or "checks" in param_type:
                info.has_ai_checks = True
                info.complexity_score += 2
                
            if "storage" in param_type or "approved" in param_type or "suspicious" in param_type:
                info.has_storage = True
                info.complexity_score += 1
                
            if "http" in param_type or "api" in param_type or "client" in param_type:
                info.has_external_apis = True
                info.complexity_score += 2
        
        # Анализируем методы сервиса (если доступен исходный код)
        service_file = self._find_service_file(service.name)
        if service_file:
            self._analyze_service_methods(service_file, info)
        
        return info
    
    def _find_service_file(self, service_name: str) -> Path:
        """Находит файл сервиса"""
        possible_paths = [
            self.project_root / "ClubDoorman" / "Services" / f"{service_name}.cs",
            self.project_root / "ClubDoorman" / "Handlers" / f"{service_name}.cs",
            self.project_root / "ClubDoorman" / "Infrastructure" / f"{service_name}.cs",
        ]
        
        for path in possible_paths:
            if path.exists():
                return path
        return None
    
    def _analyze_service_methods(self, service_file: Path, info: BusinessLogicInfo):
        """Анализирует методы сервиса"""
        try:
            content = service_file.read_text(encoding='utf-8')
        except Exception:
            return
        
        # Проверяем наличие async методов
        if re.search(r'async\s+Task', content):
            info.has_async_methods = True
            info.complexity_score += 1
        
        # Анализируем бизнес-логику по ключевым словам
        if re.search(r'ModerationAction|Allow|Delete|Ban', content):
            info.has_moderation_logic = True
            
        if re.search(r'Captcha|Verify|Challenge', content):
            info.has_captcha_logic = True
            
        if re.search(r'User|Member|Approved|Suspicious', content):
            info.has_user_management = True
            
        if re.search(r'Statistics|Stats|Metrics', content):
            info.has_statistics = True
            
        if re.search(r'AI|OpenAI|GPT|Classification', content):
            info.has_ai_checks = True
    
    def generate_smart_test_factory_methods(self, service: ClassInfo, logic_info: BusinessLogicInfo) -> str:
        """Генерирует умные методы для TestFactory на основе бизнес-логики"""
        methods = []
        
        # Специфичные методы на основе бизнес-логики (НЕ базовые, они уже есть)
        if logic_info.has_telegram_client:
            methods.append(self._generate_fake_telegram_methods())
            
        if logic_info.has_moderation_logic and service.name != "ModerationService":
            methods.append(self._generate_moderation_methods())
            
        if logic_info.has_captcha_logic and service.name != "CaptchaService":
            methods.append(self._generate_captcha_methods())
            
        if logic_info.has_user_management and service.name != "UserManager":
            methods.append(self._generate_user_management_methods())
            
        if logic_info.has_async_methods:
            methods.append(self._generate_async_methods(service.name))
            
        if logic_info.complexity_score > 5:
            methods.append(self._generate_advanced_methods())
            
        # Специфичные методы для конкретных сервисов
        if service.name == "MessageHandler":
            methods.append(self._generate_message_handler_specific_methods())
            
        if service.name == "CaptchaService":
            methods.append(self._generate_captcha_service_specific_methods())
            
        if service.name == "SpamHamClassifier":
            methods.append(self._generate_spam_ham_classifier_specific_methods())
            
        # ChatMemberHandler обрабатывается в базовом методе
        
        return "\n".join(methods)
    
    def _generate_basic_create_method(self, service: ClassInfo) -> str:
        """Генерирует базовый метод создания"""
        if service.name == "ChatMemberHandler":
            return """
    public ChatMemberHandler CreateChatMemberHandler()
    {
        return new ChatMemberHandler(
            BotMock.Object,
            UserManagerMock.Object,
            LoggerMock.Object,
            new IntroFlowService(
                BotMock.Object,
                new Mock<ILogger<IntroFlowService>>().Object,
                new Mock<ICaptchaService>().Object,
                UserManagerMock.Object,
                new Mock<IAiChecks>().Object,
                new Mock<IStatisticsService>().Object,
                new Mock<GlobalStatsManager>().Object,
                new Mock<IModerationService>().Object
            )
        );
    }"""
        elif service.name.endswith("Exception"):
            # Для исключений используем строковые литералы вместо моков
            param_values = []
            for param in service.constructor_params:
                if param.type == "string":
                    param_values.append('"Test exception message"')
                else:
                    param_values.append(f'{param.name}Mock.Object')
            
            return f"""
    public {service.name} Create{service.name}()
    {{
        return new {service.name}(
            {', '.join(param_values)}
        );
    }}"""
        else:
            return f"""
    public {service.name} Create{service.name}()
    {{
        return new {service.name}(
            {', '.join(f'{param.name}Mock.Object' for param in service.constructor_params)}
        );
    }}"""
    
    def _generate_fake_telegram_methods(self) -> str:
        """Генерирует методы для работы с Telegram"""
        return """
    public FakeTelegramClient FakeTelegramClient => new FakeTelegramClient();
    
    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock => new Mock<ITelegramBotClientWrapper>();"""
    
    def _generate_moderation_methods(self) -> str:
        """Генерирует методы для модерации"""
        return """
    public ModerationService CreateModerationServiceWithFake()
    {
        return new ModerationService(
            new Mock<ISpamHamClassifier>().Object,
            new Mock<IMimicryClassifier>().Object,
            new Mock<IBadMessageManager>().Object,
            new Mock<IUserManager>().Object,
            new Mock<IAiChecks>().Object,
            new Mock<ISuspiciousUsersStorage>().Object,
            new Mock<ITelegramBotClient>().Object,
            new Mock<ILogger<ModerationService>>().Object
        );
    }"""
    
    def _generate_captcha_methods(self) -> str:
        """Генерирует методы для капчи"""
        return """
    public CaptchaService CreateCaptchaServiceWithFake()
    {
        return new CaptchaService(
            new Mock<ITelegramBotClientWrapper>().Object,
            new Mock<ILogger<CaptchaService>>().Object
        );
    }"""
    
    def _generate_user_management_methods(self) -> str:
        """Генерирует методы для управления пользователями"""
        return """
    public IUserManager CreateUserManagerWithFake()
    {
        return new Mock<IUserManager>().Object;
    }"""
    
    def _generate_async_methods(self, service_name: str) -> str:
        """Генерирует методы для async операций"""
        return f"""
    public async Task<{service_name}> CreateAsync()
    {{
        return await Task.FromResult(Create{service_name}());
    }}"""
    
    def _generate_advanced_methods(self) -> str:
        """Генерирует продвинутые методы"""
        return """
    public SpamHamClassifier CreateMockSpamHamClassifier()
    {
        return new SpamHamClassifier(
            new Mock<ILogger<SpamHamClassifier>>().Object
        );
    }"""
    
    def _generate_message_handler_specific_methods(self) -> str:
        """Генерирует специфичные методы для MessageHandler"""
        return """
    public MessageHandler CreateMessageHandlerWithFake()
    {
        return CreateMessageHandler();
    }
    
    public MessageHandler CreateMessageHandlerWithFake(FakeTelegramClient fakeClient)
    {
        return CreateMessageHandler();
    }
    
    public MessageHandler CreateMessageHandlerWithFake(Action<MessageHandlerTestFactory> setup)
    {
        setup(this);
        return CreateMessageHandler();
    }"""
    
    def _generate_captcha_service_specific_methods(self) -> str:
        """Генерирует специфичные методы для CaptchaService"""
        return """
    public CaptchaService CreateCaptchaServiceWithFake()
    {
        return CreateCaptchaService();
    }
    
    public CaptchaService CreateCaptchaServiceWithFake(FakeTelegramClient fakeClient)
    {
        return CreateCaptchaService();
    }
    
    public CaptchaService CreateCaptchaServiceWithFake(Action<CaptchaServiceTestFactory> setup)
    {
        setup(this);
        return CreateCaptchaService();
    }"""
    

    
    def _generate_spam_ham_classifier_specific_methods(self) -> str:
        """Генерирует специфичные методы для SpamHamClassifier"""
        return """
    public Mock<ISpamHamClassifier> CreateMockSpamHamClassifier()
    {
        return new Mock<ISpamHamClassifier>();
    }""" 