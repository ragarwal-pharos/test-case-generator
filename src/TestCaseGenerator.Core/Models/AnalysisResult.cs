namespace TestCaseGenerator.Core.Models;

/// <summary>
/// Represents the result of analyzing a source code file
/// </summary>
public class AnalysisResult
{
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<ClassInfo> Classes { get; set; } = new();
    public List<MethodInfo> Methods { get; set; } = new();
    public List<PropertyInfo> Properties { get; set; } = new();
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<string> UsingStatements { get; set; } = new();
    public ProjectStructure? ProjectStructure { get; set; }
    public ExistingTestInfo? ExistingTests { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Information about a class found during analysis
/// </summary>
public class ClassInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<MethodInfo> Methods { get; set; } = new();
    public List<PropertyInfo> Properties { get; set; } = new();
    public List<ConstructorInfo> Constructors { get; set; } = new();
    public List<string> BaseTypes { get; set; } = new();
    public List<string> Interfaces { get; set; } = new();
    public List<AttributeInfo> Attributes { get; set; } = new();
    public AccessModifier AccessModifier { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsSealed { get; set; }
    public bool IsStatic { get; set; }
    public bool IsController { get; set; }
    public bool IsService { get; set; }
    public bool IsRepository { get; set; }
}

/// <summary>
/// Information about a method found during analysis
/// </summary>
public class MethodInfo
{
    public string Name { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public List<ParameterInfo> Parameters { get; set; } = new();
    public List<AttributeInfo> Attributes { get; set; } = new();
    public AccessModifier AccessModifier { get; set; }
    public bool IsAsync { get; set; }
    public bool IsStatic { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsOverride { get; set; }
    public bool IsAbstract { get; set; }
    public string Documentation { get; set; } = string.Empty;
    public List<string> ThrownExceptions { get; set; } = new();
    public int CyclomaticComplexity { get; set; }
}

/// <summary>
/// Information about a property found during analysis
/// </summary>
public class PropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public AccessModifier AccessModifier { get; set; }
    public bool HasGetter { get; set; }
    public bool HasSetter { get; set; }
    public bool IsAutoProperty { get; set; }
    public List<AttributeInfo> Attributes { get; set; } = new();
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Information about a constructor found during analysis
/// </summary>
public class ConstructorInfo
{
    public List<ParameterInfo> Parameters { get; set; } = new();
    public AccessModifier AccessModifier { get; set; }
    public bool CallsBase { get; set; }
    public bool CallsThis { get; set; }
    public string Documentation { get; set; } = string.Empty;
}

/// <summary>
/// Information about a parameter
/// </summary>
public class ParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsOptional { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsParams { get; set; }
    public bool IsOut { get; set; }
    public bool IsRef { get; set; }
    public List<AttributeInfo> Attributes { get; set; } = new();
}

/// <summary>
/// Information about an attribute
/// </summary>
public class AttributeInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();
    public Dictionary<string, string> NamedArguments { get; set; } = new();
}

/// <summary>
/// Information about a dependency
/// </summary>
public class DependencyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string InterfaceType { get; set; } = string.Empty;
    public bool IsInjected { get; set; }
    public DependencyLifetime Lifetime { get; set; }
    public bool RequiresMock { get; set; }
}

/// <summary>
/// Information about existing tests
/// </summary>
public class ExistingTestInfo
{
    public List<string> TestFiles { get; set; } = new();
    public List<TestClassInfo> TestClasses { get; set; } = new();
    public string TestFramework { get; set; } = string.Empty;
    public List<string> TestLibraries { get; set; } = new();
    public Dictionary<string, List<string>> ExistingTestMethods { get; set; } = new();
}

/// <summary>
/// Information about an existing test class
/// </summary>
public class TestClassInfo
{
    public string Name { get; set; } = string.Empty;
    public string TestedClass { get; set; } = string.Empty;
    public List<string> TestMethods { get; set; } = new();
    public string Framework { get; set; } = string.Empty;
    public List<string> MockedDependencies { get; set; } = new();
}

/// <summary>
/// Access modifier enumeration
/// </summary>
public enum AccessModifier
{
    Private,
    Protected,
    Internal,
    Public,
    ProtectedInternal,
    PrivateProtected
}

/// <summary>
/// Dependency lifetime enumeration
/// </summary>
public enum DependencyLifetime
{
    Transient,
    Scoped,
    Singleton,
    Unknown
}
