namespace TestCaseGenerator.Core.Models;

/// <summary>
/// Represents a generated test case
/// </summary>
public class TestCase
{
    public string TestName { get; set; } = string.Empty;
    public string TestMethod { get; set; } = string.Empty;
    public string TargetMethod { get; set; } = string.Empty;
    public string TargetClass { get; set; } = string.Empty;
    public string TestCode { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
    public TestType Type { get; set; }
    public string Framework { get; set; } = string.Empty;
    public List<string> Assertions { get; set; } = new();
    public List<string> ArrangeCode { get; set; } = new();
    public List<string> MockSetup { get; set; } = new();
    public List<string> MockVerifications { get; set; } = new();
    public Dictionary<string, object> TestData { get; set; } = new();
    public bool IsAsync { get; set; }
    public int Priority { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Represents the result of test generation
/// </summary>
public class GenerationResult
{
    public bool Success { get; set; }
    public List<TestCase> TestCases { get; set; } = new();
    public List<GeneratedTestFile> GeneratedFiles { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public GenerationStatistics Statistics { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Represents a generated test file
/// </summary>
public class GeneratedTestFile
{
    public string FilePath { get; set; } = string.Empty;
    public string SourceFilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public List<TestCase> TestCases { get; set; } = new();
    public bool IsNewFile { get; set; }
    public bool RequiresCompilation { get; set; }
}

/// <summary>
/// Types of tests that can be generated
/// </summary>
public enum TestType
{
    Unit,
    Integration,
    Controller,
    Service,
    Repository,
    Model,
    Property,
    Constructor,
    Exception,
    AsyncMethod,
    StaticMethod,
    ExtensionMethod
}
