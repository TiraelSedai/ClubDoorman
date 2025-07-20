using System;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Безопасные маркеры для улучшения DX Tool
/// Эти атрибуты используются только для тестирования и не влияют на бизнес-логику
/// </summary>

#if TESTING
/// <summary>
/// Указывает, что конкретный класс требует мока в тестах
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor, AllowMultiple = false)]
public class TestRequiresConcreteMockAttribute : Attribute
{
    public string[] ConcreteTypes { get; }
    
    public TestRequiresConcreteMockAttribute(params string[] concreteTypes)
    {
        ConcreteTypes = concreteTypes;
    }
}

/// <summary>
/// Указывает, что класс требует кастомной инициализации в тестах
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor, AllowMultiple = false)]
public class TestRequiresCustomInitializationAttribute : Attribute
{
    public string Reason { get; }
    
    public TestRequiresCustomInitializationAttribute(string reason = "")
    {
        Reason = reason;
    }
}

/// <summary>
/// Указывает, что интерфейс лучше заменить тестовой утилитой
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class TestRequiresUtilityAttribute : Attribute
{
    public string UtilityType { get; }
    
    public TestRequiresUtilityAttribute(string utilityType)
    {
        UtilityType = utilityType;
    }
}

/// <summary>
/// Указывает, что класс требует специальной обработки в тестах
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor, AllowMultiple = false)]
public class TestRequiresSpecialHandlingAttribute : Attribute
{
    public string[] Reasons { get; }
    
    public TestRequiresSpecialHandlingAttribute(params string[] reasons)
    {
        Reasons = reasons;
    }
}

/// <summary>
/// Указывает, что параметр конструктора требует особого внимания в тестах
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class TestParameterAttribute : Attribute
{
    public string Handling { get; }
    public string[] Dependencies { get; }
    
    public TestParameterAttribute(string handling = "", params string[] dependencies)
    {
        Handling = handling;
        Dependencies = dependencies;
    }
}
#endif 