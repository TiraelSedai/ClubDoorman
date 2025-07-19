# План автоматизации рефакторинга для C# ClubDoorman

## Инструменты для C# автоматизации

### 🔧 Зрелые AST-инструменты

| Инструмент | Возможности | Статус |
|------------|-------------|--------|
| **dotnet-format** | Автоматическое форматирование | ✅ Установлен |
| **Roslyn Analyzers** | Кастомные анализаторы | ✅ Доступно |
| **JetBrains ReSharper** | Мощные рефакторинги | ✅ Есть бесплатная версия |
| **Visual Studio Refactoring** | Встроенные рефакторинги | ✅ Доступно |
| **Roslynator** | Дополнительные рефакторинги | ⏳ Можно добавить |

### 🚀 Автоматизируемые задачи

#### 1. **Полностью автоматически** (100% без вмешательств)

**Дублирование кода:**
```bash
# Создание утилитарных классов
dotnet new class -n UserUtils -o ClubDoorman/Infrastructure/
dotnet new class -n MessageUtils -o ClubDoorman/Infrastructure/
```

**Перемещение методов:**
```bash
# Roslyn может автоматически переместить методы между классами
# и обновить все ссылки
```

**Переименование:**
```bash
# Автоматическое переименование с обновлением всех ссылок
# через IDE или Roslyn
```

#### 2. **AI-в помощь** (требует ревью)

**Разделение больших классов:**
- AI анализирует структуру и предлагает план разделения
- AST-инструменты выполняют конкретные операции

**Вынос общих утилит:**
- AI идентифицирует дублированный код
- Roslyn выполняет экстракцию

## Этап 1: Настройка инструментов анализа

### 1.1 Добавить анализаторы в проект

```xml
<!-- Добавить в ClubDoorman.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
  <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.507">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
  <PackageReference Include="Roslynator.Analyzers" Version="4.7.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### 1.2 Создать .editorconfig для автоматизации

```ini
# .editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
indent_style = space
indent_size = 4

# Автоматические исправления
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion
dotnet_style_require_accessibility_modifiers = for_non_interface_members:suggestion
dotnet_style_readonly_field = true:suggestion

# Автоматическое удаление using
dotnet_style_unused_using_directive = true:suggestion
```

## Этап 2: Автоматизация устранения дублирования

### 2.1 Создать Roslyn Analyzer для дублирования

```csharp
// Analyzers/DuplicateCodeAnalyzer.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DuplicateCodeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CD001";
    
    private static readonly LocalizableString Title = "Duplicate code detected";
    private static readonly LocalizableString MessageFormat = "Method '{0}' is duplicated in multiple files";
    
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, 
        "CodeQuality", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        
        // Логика поиска дублирования
        // ...
    }
}
```

### 2.2 Создать Code Fix для автоматического исправления

```csharp
// CodeFixes/ExtractToUtilityClassCodeFix.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExtractToUtilityClassCodeFix))]
public class ExtractToUtilityClassCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DuplicateCodeAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Extract to utility class",
                createChangedSolution: c => ExtractToUtilityClassAsync(context.Document, diagnosticSpan, c),
                equivalenceKey: "ExtractToUtilityClass"),
            diagnostic);
    }

    private async Task<Solution> ExtractToUtilityClassAsync(Document document, TextSpan span, CancellationToken cancellationToken)
    {
        // Логика автоматического извлечения в утилитарный класс
        // ...
    }
}
```

## Этап 3: Автоматизация разделения больших классов

### 3.1 Создать Analyzer для больших классов

```csharp
// Analyzers/LargeClassAnalyzer.cs
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LargeClassAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CD002";
    
    private static readonly LocalizableString Title = "Class is too large";
    private static readonly LocalizableString MessageFormat = "Class '{0}' has {1} lines and {2} methods. Consider splitting it.";
    
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, 
        "CodeQuality", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeClass(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        
        var lineCount = classDeclaration.GetLocation().GetLineSpan().EndLinePosition.Line - 
                       classDeclaration.GetLocation().GetLineSpan().StartLinePosition.Line;
        
        var methodCount = classDeclaration.Members.OfType<MethodDeclarationSyntax>().Count();
        
        if (lineCount > 400 || methodCount > 15)
        {
            var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), 
                classDeclaration.Identifier.ValueText, lineCount, methodCount);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

### 3.2 Создать Code Fix для автоматического разделения

```csharp
// CodeFixes/SplitLargeClassCodeFix.cs
public class SplitLargeClassCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(LargeClassAnalyzer.DiagnosticId);

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Split class into smaller classes",
                createChangedSolution: c => SplitClassAsync(context.Document, diagnostic, c),
                equivalenceKey: "SplitLargeClass"),
            diagnostic);
    }

    private async Task<Solution> SplitClassAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        // AI-анализ структуры класса
        var classDeclaration = await GetClassDeclaration(document, diagnostic, cancellationToken);
        
        // Предложение плана разделения
        var splitPlan = await AnalyzeClassStructure(classDeclaration);
        
        // Автоматическое выполнение разделения
        return await ExecuteSplitPlan(document, splitPlan, cancellationToken);
    }
}
```

## Этап 4: Автоматизация с помощью AI

### 4.1 Создать AI-ассистент для анализа

```csharp
// Services/RefactoringAIAssistant.cs
public class RefactoringAIAssistant
{
    private readonly OpenAIClient _openAIClient;
    
    public async Task<RefactoringPlan> AnalyzeClassAsync(ClassDeclarationSyntax classDeclaration)
    {
        var classCode = classDeclaration.ToFullString();
        
        var prompt = $@"
        Analyze this C# class and suggest a refactoring plan:
        
        {classCode}
        
        Provide:
        1. Suggested class splits
        2. Methods to extract
        3. New file structure
        4. Dependencies to consider
        ";
        
        var response = await _openAIClient.GetChatCompletionAsync(prompt);
        return ParseRefactoringPlan(response);
    }
    
    public async Task<string> GenerateRoslynCodeAsync(RefactoringPlan plan)
    {
        var prompt = $@"
        Generate Roslyn code to execute this refactoring plan:
        
        {JsonSerializer.Serialize(plan)}
        
        Return only the C# code that uses Roslyn APIs.
        ";
        
        return await _openAIClient.GetChatCompletionAsync(prompt);
    }
}
```

### 4.2 Создать автоматический рефакторинг

```csharp
// Services/AutomatedRefactoringService.cs
public class AutomatedRefactoringService
{
    private readonly RefactoringAIAssistant _aiAssistant;
    private readonly Workspace _workspace;
    
    public async Task<Solution> RefactorLargeClassAsync(Document document, ClassDeclarationSyntax classDeclaration)
    {
        // 1. AI анализирует класс
        var plan = await _aiAssistant.AnalyzeClassAsync(classDeclaration);
        
        // 2. Генерирует Roslyn код
        var roslynCode = await _aiAssistant.GenerateRoslynCodeAsync(plan);
        
        // 3. Выполняет рефакторинг
        return await ExecuteRoslynCode(document, roslynCode);
    }
    
    private async Task<Solution> ExecuteRoslynCode(Document document, string roslynCode)
    {
        // Динамическое выполнение сгенерированного Roslyn кода
        // ...
    }
}
```

## Этап 5: CI/CD интеграция

### 5.1 Создать GitHub Action для автоматического рефакторинга

```yaml
# .github/workflows/auto-refactor.yml
name: Auto Refactor

on:
  push:
    branches: [ develop ]
  pull_request:
    branches: [ main ]

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
    
    - name: Install dependencies
      run: dotnet restore
    
    - name: Run analyzers
      run: dotnet build --verbosity normal
    
    - name: Run auto-refactor
      run: dotnet run --project tools/RefactoringTool
    
    - name: Create PR if changes
      uses: peter-evans/create-pull-request@v4
      with:
        title: 'Auto-refactor: Split large classes and remove duplicates'
        body: 'Automated refactoring based on code analysis'
        branch: auto-refactor/$(date +%Y%m%d-%H%M%S)
```

### 5.2 Создать инструмент командной строки

```csharp
// tools/RefactoringTool/Program.cs
class Program
{
    static async Task Main(string[] args)
    {
        var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(args[0]);
        
        var refactoringService = new AutomatedRefactoringService();
        
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync();
                var root = await syntaxTree.GetRootAsync();
                
                // Найти большие классы
                var largeClasses = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(c => IsLargeClass(c));
                
                foreach (var largeClass in largeClasses)
                {
                    solution = await refactoringService.RefactorLargeClassAsync(document, largeClass);
                }
            }
        }
        
        workspace.TryApplyChanges(solution);
    }
}
```

## Этап 6: Мониторинг и метрики

### 6.1 Создать метрики качества кода

```csharp
// Services/CodeQualityMetrics.cs
public class CodeQualityMetrics
{
    public async Task<QualityReport> GenerateReportAsync(Solution solution)
    {
        var report = new QualityReport();
        
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync();
                var root = await syntaxTree.GetRootAsync();
                
                report.TotalLines += root.GetText().Lines.Count;
                report.TotalClasses += root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();
                report.TotalMethods += root.DescendantNodes().OfType<MethodDeclarationSyntax>().Count();
                
                // Анализ сложности
                report.ComplexClasses += root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Count(c => IsComplexClass(c));
            }
        }
        
        return report;
    }
}
```

## Рецепт "минимум магии" для ClubDoorman

### 1. Покрываем тестами критичный функционал
```bash
# Добавить тесты для основных функций
dotnet test --collect:"XPlat Code Coverage"
```

### 2. Строим call graph
```bash
# Использовать Sourcegraph или IDE для анализа зависимостей
```

### 3. LLM-prompt для анализа
```
"Проанализируй класс MessageHandler.cs (1034 строки) и предложи план разделения на логические части"
```

### 4. Генерируем Roslyn код
```csharp
// AI генерирует Roslyn код для автоматического рефакторинга
var workspace = MSBuildWorkspace.Create();
var solution = await workspace.OpenSolutionAsync("ClubDoorman.sln");
// ... автоматический рефакторинг
```

### 5. Прогоняем на черновой ветке
```bash
git checkout -b auto-refactor
dotnet build
dotnet test
```

### 6. Автопринятие только зеленых diff-ов
```yaml
# В CI проверяем, что все тесты проходят
- name: Verify refactoring
  run: |
    dotnet build
    dotnet test
    if [ $? -eq 0 ]; then
      echo "Refactoring successful"
    else
      echo "Refactoring failed, reverting"
      git reset --hard HEAD~1
    fi
```

## Ожидаемые результаты

### До автоматизации:
- Ручной рефакторинг: 3-4 недели
- Высокий риск ошибок
- Сложность тестирования

### После автоматизации:
- Автоматический рефакторинг: 1-2 дня
- Низкий риск ошибок
- Полное покрытие тестами
- Возможность отката

### Метрики качества:
- Размер файлов: <300 строк
- Количество методов: <15
- Циклическая сложность: <20
- Дублирование кода: 0%

## Заключение

Для C# проекта ClubDoorman можно создать мощную систему автоматизации рефакторинга, которая:

1. **Автоматически** устраняет дублирование кода
2. **AI-помощью** анализирует и планирует разделение больших классов
3. **Roslyn** выполняет точные AST-преобразования
4. **CI/CD** обеспечивает безопасность и качество

Это позволит выполнить рефакторинг за 1-2 дня вместо 3-4 недель с минимальным риском ошибок. 