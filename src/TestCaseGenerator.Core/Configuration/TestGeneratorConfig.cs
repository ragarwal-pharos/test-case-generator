using System.ComponentModel.DataAnnotations;

namespace TestCaseGenerator.Core.Configuration;

/// <summary>
/// Main configuration class for the Test Case Generator
/// </summary>
public class TestGeneratorConfig
{
    /// <summary>
    /// Project configuration settings
    /// </summary>
    public ProjectConfig Project { get; set; } = new();

    /// <summary>
    /// Analysis configuration settings
    /// </summary>
    public AnalysisConfig Analysis { get; set; } = new();

    /// <summary>
    /// Generation configuration settings
    /// </summary>
    public GenerationConfig Generation { get; set; } = new();

    /// <summary>
    /// File type specific configurations
    /// </summary>
    public Dictionary<string, FileTypeConfig> FileTypes { get; set; } = new();

    /// <summary>
    /// Template configurations
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> Templates { get; set; } = new();

    /// <summary>
    /// Output configuration settings
    /// </summary>
    public OutputConfig Output { get; set; } = new();

    /// <summary>
    /// Logging configuration settings
    /// </summary>
    public LoggingConfig Logging { get; set; } = new();

    /// <summary>
    /// Performance configuration settings
    /// </summary>
    public PerformanceConfig Performance { get; set; } = new();

    /// <summary>
    /// Validation configuration settings
    /// </summary>
    public ValidationConfig Validation { get; set; } = new();
}

/// <summary>
/// Project-level configuration
/// </summary>
public class ProjectConfig
{
    /// <summary>
    /// Name of the project
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Root path of the project to analyze
    /// </summary>
    [Required]
    public string RootPath { get; set; } = "./";

    /// <summary>
    /// Path where generated tests will be saved
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = "./GeneratedTests";

    /// <summary>
    /// Test frameworks to use for different languages
    /// </summary>
    public Dictionary<string, string> TestFrameworks { get; set; } = new()
    {
        { "csharp", "xunit" },
        { "typescript", "jest" }
    };

    /// <summary>
    /// Source directories to analyze
    /// </summary>
    public List<string> SourceDirectories { get; set; } = new();
}

/// <summary>
/// Analysis configuration
/// </summary>
public class AnalysisConfig
{
    /// <summary>
    /// Whether to analyze private methods
    /// </summary>
    public bool IncludePrivateMethods { get; set; } = false;

    /// <summary>
    /// Whether to analyze internal methods
    /// </summary>
    public bool IncludeInternalMethods { get; set; } = true;

    /// <summary>
    /// Whether to generate mock objects for dependencies
    /// </summary>
    public bool GenerateMocks { get; set; } = true;

    /// <summary>
    /// Whether to analyze dependencies between classes
    /// </summary>
    public bool AnalyzeDependencies { get; set; } = true;

    /// <summary>
    /// Maximum depth for dependency analysis
    /// </summary>
    [Range(1, 10)]
    public int MaxDepth { get; set; } = 3;

    /// <summary>
    /// Patterns to exclude from analysis
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new()
    {
        "*.designer.cs",
        "*.generated.cs",
        "**/bin/**",
        "**/obj/**"
    };
}

/// <summary>
/// Test generation configuration
/// </summary>
public class GenerationConfig
{
    /// <summary>
    /// Naming convention for generated test methods
    /// </summary>
    public string TestNamingConvention { get; set; } = "MethodName_Scenario_ExpectedResult";

    /// <summary>
    /// Whether to include Arrange-Act-Assert comments in tests
    /// </summary>
    public bool IncludeArrangeActAssert { get; set; } = true;

    /// <summary>
    /// Whether to generate test data automatically
    /// </summary>
    public bool GenerateTestData { get; set; } = true;

    /// <summary>
    /// Whether to generate negative test cases
    /// </summary>
    public bool GenerateNegativeTests { get; set; } = true;

    /// <summary>
    /// Whether to include documentation in generated tests
    /// </summary>
    public bool IncludeDocumentation { get; set; } = true;

    /// <summary>
    /// Prefix for test method names
    /// </summary>
    public string TestMethodPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Mocking framework to use
    /// </summary>
    public string MockFramework { get; set; } = "Moq";
}

/// <summary>
/// File type specific configuration
/// </summary>
public class FileTypeConfig
{
    /// <summary>
    /// Whether this file type is enabled for processing
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// File extensions to process for this type
    /// </summary>
    public List<string> Extensions { get; set; } = new();

    /// <summary>
    /// Patterns to exclude for this file type
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new();

    /// <summary>
    /// Test-specific settings for this file type
    /// </summary>
    public Dictionary<string, object> TestSettings { get; set; } = new();
}

/// <summary>
/// Output configuration
/// </summary>
public class OutputConfig
{
    /// <summary>
    /// Whether to overwrite existing test files
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Whether to create backups of existing files before overwriting
    /// </summary>
    public bool CreateBackups { get; set; } = true;

    /// <summary>
    /// Whether to generate analysis reports
    /// </summary>
    public bool GenerateReports { get; set; } = true;

    /// <summary>
    /// Report output formats
    /// </summary>
    public List<string> ReportFormats { get; set; } = new() { "html", "json" };

    /// <summary>
    /// Whether to group tests by type in separate folders
    /// </summary>
    public bool GroupTestsByType { get; set; } = true;

    /// <summary>
    /// Naming pattern for test files
    /// </summary>
    public string TestFileNaming { get; set; } = "{SourceFileName}Tests.{Extension}";
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfig
{
    /// <summary>
    /// Logging level
    /// </summary>
    public string Level { get; set; } = "Information";

    /// <summary>
    /// Whether to log to file
    /// </summary>
    public bool LogToFile { get; set; } = true;

    /// <summary>
    /// Path to log file
    /// </summary>
    public string LogFilePath { get; set; } = "./logs/testgen.log";

    /// <summary>
    /// Whether to include timestamps in logs
    /// </summary>
    public bool IncludeTimestamp { get; set; } = true;

    /// <summary>
    /// Whether to include stack traces in error logs
    /// </summary>
    public bool IncludeStackTrace { get; set; } = false;
}

/// <summary>
/// Performance configuration
/// </summary>
public class PerformanceConfig
{
    /// <summary>
    /// Maximum number of concurrent operations
    /// </summary>
    [Range(1, 16)]
    public int MaxConcurrency { get; set; } = 4;

    /// <summary>
    /// Whether to enable caching of analysis results
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Directory for cache files
    /// </summary>
    public string CacheDirectory { get; set; } = "./.testgen-cache";

    /// <summary>
    /// Timeout for operations in seconds
    /// </summary>
    [Range(10, 3600)]
    public int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Validation configuration
/// </summary>
public class ValidationConfig
{
    /// <summary>
    /// Whether to compile generated tests for validation
    /// </summary>
    public bool CompileGeneratedTests { get; set; } = true;

    /// <summary>
    /// Whether to run basic validation on generated tests
    /// </summary>
    public bool RunBasicValidation { get; set; } = true;

    /// <summary>
    /// Whether to check code style of generated tests
    /// </summary>
    public bool CheckCodeStyle { get; set; } = true;

    /// <summary>
    /// Whether to generate coverage reports for generated tests
    /// </summary>
    public bool GenerateCoverageReport { get; set; } = false;
}
