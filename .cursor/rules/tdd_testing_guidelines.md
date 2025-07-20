# TDD Testing Guidelines for Cursor IDE (2025 v1.2.2)

## Core Testing Philosophy

### Primary Principle: Test Behavior, Not Implementation
- Tests should NOT break from changes that preserve behavior
- Tests should ONLY fail when behavior actually changes
- Focus on external contracts, not internal implementation details

### When Tests Should NOT Break
- Code refactoring (structure changes, same behavior)
- Internal implementation changes (algorithms, LINQ, loops)
- Mock/DI implementation changes
- Internal variable/function renaming
- Performance optimizations (same output)

### When Tests SHOULD Break
- Requirements/behavior changes
- Public API changes (method signatures, interfaces)
- Dependency removal/replacement
- Data model structure changes
- Breaking contract changes

## Test Architecture Patterns

### TestFactory Pattern Implementation
```csharp
// Recommended: Centralized test object creation
public class ModerationTestFactory
{
    public Mock<IUserManager> UserManagerMock { get; } = new();
    public Mock<ITelegramBotClient> TelegramMock { get; } = new();
    
    public ModerationService Create() => new ModerationService(
        UserManagerMock.Object,
        TelegramMock.Object,
        // additional dependencies
    );
}

// Avoid: Manual setup in every test
[Test]
public void TestSomething()
{
    var userManager = new Mock<IUserManager>();
    var telegram = new Mock<ITelegramBotClient>();
    // excessive setup code
}
```

### TestFactory Development Workflow
**Step-by-step approach for safe TestFactory implementation:**

#### Step 1: Create TestFactory
- Encapsulate service creation with all dependencies
- Configure mocks with basic setups
- Include logger configuration (NullLogger for tests)

#### Step 2: Test the TestFactory itself
```csharp
[Test]
public void Create_ReturnsWorkingService()
{
    var factory = new ModerationTestFactory();
    var service = factory.Create();
    
    Assert.NotNull(service);
    // Verify service can be instantiated without exceptions
}

[Test]
public void Create_ConfiguresAllDependencies()
{
    var factory = new ModerationTestFactory();
    var service = factory.Create();
    
    // Use reflection or expose internals to verify dependency injection
    // This protects against factory configuration errors
}
```

#### Step 3: Write unit tests using the factory
- One test per public method: happy path + failure + edge cases
- Use `var service = factory.Create();` consistently
- Keep tests homogeneous, readable, and concise

#### Step 4: Add DSL/configuration as project grows
```csharp
var service = factory
    .WithTelegramFailing()
    .WithMLTimeout()
    .Create();
```

### Test Isolation Requirements
- One test failure should not cascade to others
- Fresh mocks for each test
- No shared state between tests
- Independent test execution order

## Test Quality Standards

### Behavior-Focused Test Writing
```csharp
// Recommended: Tests observable behavior
[Test]
public async Task NullMessage_ThrowsArgumentNullException()
{
    var service = new ModerationTestFactory().Create();
    
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        service.CheckMessageAsync(null));
}

// Avoid: Testing implementation details
[Test]
public void UsesSpamClassifier_When_MessageReceived()
{
    // Do not test internal method calls
    // Test the observable behavior instead
}
```

### External Dependency Mocking
- Always mock external APIs (Telegram, OpenAI)
- Mock time-dependent operations
- Mock random number generators
- Mock file system operations

### Edge Case and Error Testing
```csharp
[Test]
public async Task ReturnsFallback_When_MLTimeout()
{
    var factory = new ModerationTestFactory();
    factory.SpamClassifierMock
        .Setup(x => x.IsSpam(It.IsAny<Message>()))
        .ThrowsAsync(new TaskCanceledException());
    
    var service = factory.Create();
    var result = await service.CheckMessageAsync(ValidMessage());
    
    Assert.Equal(ModerationAction.Allow, result.Action);
    Assert.Contains("timeout", result.Reason);
}
```

## Mutation Testing

### Purpose and Benefits
- Verify that tests actually protect against code defects
- Mutator introduces micro-bugs (e.g., `==` → `!=`) and checks if tests detect them
- Provides more honest assessment than line coverage alone

### Stryker.NET Implementation
```bash
# Install mutation testing tool
dotnet tool install -g dotnet-stryker

# Run mutation testing
cd ClubDoorman.Tests
dotnet stryker
```

### Expected Output and Metrics
```
- Killed mutants: 82% (good - tests caught the mutations)
- Survived mutants: 18% (tests missed these mutations)
- Timeout mutants: 0%
- Coverage: 91%
```

### Quality Targets
- **Excellent**: >90% killed mutants
- **Good**: 80-90% killed mutants  
- **Needs improvement**: <80% killed mutants

### Integration with CI/CD
- Can be integrated into continuous integration pipelines
- Use as quality gate for test effectiveness
- Monitor mutation score trends over time

## Cursor IDE 2025 Optimizations

### AI-Assisted Test Generation
- Generate test cases from method signatures
- Auto-create TestFactory patterns
- Suggest edge cases and error conditions
- Auto-generate mock setups

### Context Awareness Utilization
- Use file relationships for dependency mocking
- Auto-detect test patterns across project
- Suggest test improvements based on code changes
- Maintain test consistency across similar classes

### Performance Testing Standards
- Use LoggerMessage delegates for performance-critical logging
- Cache JsonSerializerOptions
- Use ReaderWriterLockSlim for concurrent access
- Avoid hardcoded credentials in tests

## Test Maintenance Procedures

### Test Refactoring Guidelines
1. Preserve test intent and behavior verification
2. Update only implementation-specific assertions
3. Keep test names descriptive and behavior-focused
4. Maintain test isolation and independence

### Test Documentation Requirements
- Use descriptive test names that explain the scenario
- Add comments for complex test setups
- Document test data and expected outcomes
- Keep test methods focused on single behavior

## Advanced Testing Patterns

### Fluent Test Builder Implementation
```csharp
var factory = TestFactory
    .WithMLThrowingTimeout()
    .WithAIReturning("suspicious")
    .WithUserManagerReturning(user)
    .Build();
```

### Test Data Factory Pattern
```csharp
public static class TestMessages
{
    public static Message ValidMessage() => new() { Text = "Hello" };
    public static Message SpamMessage() => new() { Text = "BUY NOW!!!" };
    public static Message NullMessage() => null!;
}
```

### Integration Test Boundaries
- Unit tests: Mock external dependencies
- Integration tests: Use real dependencies where safe
- End-to-end tests: Test complete user workflows
- Performance tests: Measure actual performance characteristics

## Quality Assessment Metrics

### Test Quality Indicators
- Tests pass consistently across environments
- Tests fail only when behavior actually changes
- Test execution time remains fast
- Test maintenance overhead is minimal
- Tests provide clear failure messages
- **Mutation testing score >80% killed mutants**

### Quality Warning Signs
- Tests break from internal refactoring
- Tests depend on implementation details
- Tests are brittle and hard to maintain
- Tests do not clearly indicate what they verify
- Tests have complex setup requirements
- **Low mutation testing score indicates weak test protection**

## Implementation Status Tracking

### Status Report Template
```
## Current Status
- Implemented: [specific features completed]
- Working: [tested and functional components]
- Issues: [known problems and limitations]
- Coverage: [X% tests passing]
- Mutation Score: [X% killed mutants]
- Next steps: [required improvements]
```

### Professional Communication Standards
- Use technical terminology
- Provide objective assessments
- Include specific metrics
- Identify limitations honestly
- Avoid exaggerated claims

---

**Note: The objective is to create tests that serve as living documentation of system behavior, not as a maintenance burden.** 