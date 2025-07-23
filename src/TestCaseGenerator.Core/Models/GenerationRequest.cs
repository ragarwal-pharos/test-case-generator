using TestCaseGenerator.Core.Configuration;

namespace TestCaseGenerator.Core.Models;

/// <summary>
/// Request for generating test cases
/// </summary>
public class GenerationRequest
{
    /// <summary>
    /// Path to the project to analyze
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Output path for generated tests
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Configuration for test generation
    /// </summary>
    public TestGeneratorConfig Configuration { get; set; } = new();

    /// <summary>
    /// File types to analyze
    /// </summary>
    public List<string> FileTypes { get; set; } = new();

    /// <summary>
    /// Specific files to analyze (optional)
    /// </summary>
    public List<string>? FilesToAnalyze { get; set; }

    /// <summary>
    /// Whether to analyze existing test files
    /// </summary>
    public bool AnalyzeExistingTests { get; set; } = true;

    /// <summary>
    /// Whether to overwrite existing test files
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Whether to create backups of existing files
    /// </summary>
    public bool CreateBackups { get; set; } = true;

    /// <summary>
    /// Whether to validate generated test files
    /// </summary>
    public bool ValidateGenerated { get; set; } = true;

    /// <summary>
    /// Filters for excluding files or patterns
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new();

    /// <summary>
    /// Custom templates to use for generation
    /// </summary>
    public Dictionary<string, string> CustomTemplates { get; set; } = new();
}
