# TestKit Duplication & Complexity Analysis

**Generated:** 2025-08-01T12:51:30.074736
**Total Components:** 29
**Total Methods:** 351

## 🔍 Duplication Analysis

**Found 21 potential duplication patterns:**
- 🔴 High severity: 7
- 🟡 Medium severity: 14

### 🔴 High Priority Duplications

#### Pattern: `Create*Chat`
**Files involved:** TestKit.Bogus.cs, TestKitBogus.cs, TestKit.Mocks.cs, TestKit.Facade.cs, TestKit.GoldenMaster.cs, TestKit.Builders.cs, TestKitBuilders.cs, TestKit.Main.cs
**Methods (13):** CreatePrivateChatBanScenario, CreateTestChat, CreateBotPermissionsServiceMockForChat, CreateGroupChat, CreateSupergroupChat ... and 8 more
**Optimization:** Consolidate 13 methods from 8 files into single location

#### Pattern: `Create*Message`
**Files involved:** TestKit.Bogus.cs, TestKitAutoFixture.cs, TestKitBogus.cs, TestKit.AutoFixture.cs, TestKit.Mocks.cs, TestKit.Facade.cs, TestKit.GoldenMaster.cs, TestKit.Telegram.cs, TestKit.Builders.cs, TestKitBuilders.cs, TestKitMockBuilders.cs, TestKit.Main.cs
**Methods (42):** CreateNullMessageBanScenario, CreateTestMessage, CreateMockMessageService, CreateMessageHandler, CreateRealisticMessages ... and 37 more
**Optimization:** Consolidate 42 methods from 12 files into single location

#### Pattern: `Create*Mock`
**Files involved:** TestKit.Mocks.cs, TestKitMockBuilders.cs
**Methods (22):** CreateMockBotClient, CreateMockBotClientWrapper, CreateMockSpamHamClassifier, CreateMockModerationService, CreateMockCaptchaService ... and 17 more
**Optimization:** Consolidate 22 methods from 2 files into single location

#### Pattern: `Create*User`
**Files involved:** TestKit.Bogus.cs, TestKitAutoFixture.cs, TestKitBogus.cs, TestKit.AutoFixture.cs, TestKit.Mocks.cs, TestKit.Facade.cs, TestKit.Builders.cs, TestKitBuilders.cs, TestKitMockBuilders.cs, TestKit.Main.cs
**Methods (28):** CreateTestUser, CreateMockUserManager, CreateMockUserBanService, CreateApprovedUserManager, CreateUnapprovedUserManager ... and 23 more
**Optimization:** Consolidate 28 methods from 10 files into single location

#### Pattern: `Create*Service`
**Files involved:** TestKitAutoFixture.cs, TestKit.AutoFixture.cs, TestKit.Mocks.cs, TestKit.Facade.cs, TestKitMockBuilders.cs
**Methods (26):** CreateMockModerationService, CreateMockCaptchaService, CreateMockUserBanService, CreateMockMessageService, CreateMockStatisticsService ... and 21 more
**Optimization:** Consolidate 26 methods from 5 files into single location

#### Pattern: `*Result`
**Files involved:** TestKitBuilders.cs, TestKit.Builders.cs, TestKit.Specialized.cs, TestKit.Main.cs
**Methods (12):** CorrectResult, IncorrectResult, BanResult, DeleteResult, AllowResult ... and 7 more
**Optimization:** Consolidate 12 methods from 4 files into single location

#### Pattern: `*Builder`
**Files involved:** TestKit.BuilderTests.cs, TestKit.Facade.cs
**Methods (14):** CreateMessageHandlerBuilder, CreateNotificationServiceBuilder, CreateUserJoinServiceBuilder, MessageHandlerBuilder_WithStandardMocks_CreatesValidHandler, MessageHandlerBuilder_WithBanMocks_CreatesHandlerWithBanConfiguration ... and 9 more
**Optimization:** Consolidate 14 methods from 2 files into single location

### 🟡 Medium Priority Duplications

#### Pattern: `Create*Factory`
**Impact:** 10 methods in 1 files
**Optimization:** Review for consolidation opportunity

#### Pattern: `tag:factory`
**Impact:** 10 methods in 13 files
**Optimization:** Consider consolidating 'factory' functionality

#### Pattern: `tag:message`
**Impact:** 10 methods in 20 files
**Optimization:** Consider consolidating 'message' functionality

#### Pattern: `tag:mock`
**Impact:** 10 methods in 14 files
**Optimization:** Consider consolidating 'mock' functionality

#### Pattern: `tag:ban`
**Impact:** 10 methods in 10 files
**Optimization:** Consider consolidating 'ban' functionality

#### Pattern: `tag:chat`
**Impact:** 10 methods in 12 files
**Optimization:** Consider consolidating 'chat' functionality

#### Pattern: `tag:collection`
**Impact:** 10 methods in 5 files
**Optimization:** Consider consolidating 'collection' functionality

#### Pattern: `tag:builder`
**Impact:** 10 methods in 19 files
**Optimization:** Consider consolidating 'builder' functionality

#### Pattern: `tag:user`
**Impact:** 10 methods in 19 files
**Optimization:** Consider consolidating 'user' functionality

#### Pattern: `tag:captcha`
**Impact:** 10 methods in 9 files
**Optimization:** Consider consolidating 'captcha' functionality

#### Pattern: `tag:moderation`
**Impact:** 10 methods in 9 files
**Optimization:** Consider consolidating 'moderation' functionality

#### Pattern: `tag:spam`
**Impact:** 10 methods in 10 files
**Optimization:** Consider consolidating 'spam' functionality

#### Pattern: `tag:valid`
**Impact:** 10 methods in 5 files
**Optimization:** Consider consolidating 'valid' functionality

#### Pattern: `tag:bogus`
**Impact:** 10 methods in 4 files
**Optimization:** Consider consolidating 'bogus' functionality

## 📊 Complexity Analysis

**Found 12 complexity issues:**

### Low Method Density

**TestKit.GoldenMaster.cs**
- Average 54.9 lines per method
- Suggestion: Methods might be too complex - consider simplification

**TestKit.UserJoinServiceBuilder.cs**
- Average 21.8 lines per method
- Suggestion: Methods might be too complex - consider simplification

**TestKit.MessageHandlerBuilder.cs**
- Average 29.9 lines per method
- Suggestion: Methods might be too complex - consider simplification

**TestKit.Specialized.cs**
- Average 20.8 lines per method
- Suggestion: Methods might be too complex - consider simplification

**TestKit.Telegram.cs**
- Average 55.0 lines per method
- Suggestion: Methods might be too complex - consider simplification

**TestKit.NotificationServiceBuilder.cs**
- Average 42.0 lines per method
- Suggestion: Methods might be too complex - consider simplification

**TestKitAutoFixture.cs**
- Average 26.5 lines per method
- Suggestion: Methods might be too complex - consider simplification

### High Method Count

**TestKit.Mocks.cs**
- Component has 33 methods
- Suggestion: Consider splitting into multiple specialized classes

**TestKit.Specialized.cs**
- Component has 36 methods
- Suggestion: Consider splitting into multiple specialized classes

**TestKit.Main.cs**
- Component has 43 methods
- Suggestion: Consider splitting into multiple specialized classes

**TestKit.Builders.cs**
- Component has 33 methods
- Suggestion: Consider splitting into multiple specialized classes

### High Line Count

**TestKit.Specialized.cs**
- Component has 747 lines
- Suggestion: Consider breaking into smaller modules

## 💡 Optimization Recommendations

### 🔥 High Impact Optimizations
**7 high-priority duplications** found.
**Impact:** Reduced maintenance, clearer API
**Effort:** Medium (refactoring required)
**Recommendation:** Address these first

## 📈 Architecture Health Metrics

- **Average methods per component:** 12.1
- **Total duplication patterns:** 21
- **Complexity issues:** 12
- **Architecture Health Score:** 0/100

🔴 **Needs optimization** - significant issues found
