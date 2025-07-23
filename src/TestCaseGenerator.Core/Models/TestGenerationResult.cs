namespace TestCaseGenerator.Core.Models;

/// <summary>
/// Result of test generation operation
/// </summary>
public class TestGenerationResult
{
    /// <summary>
    /// Whether the generation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Generated test cases
    /// </summary>
    public List<TestCase> GeneratedTests { get; set; } = new();

    /// <summary>
    /// Analysis results for processed files
    /// </summary>
    public List<AnalysisResult> AnalysisResults { get; set; } = new();

    /// <summary>
    /// Errors encountered during generation
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Warnings encountered during generation
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Performance and statistical information
    /// </summary>
    public GenerationStatistics Statistics { get; set; } = new();

    /// <summary>
    /// Total duration of the generation process
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Timestamp when generation started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Timestamp when generation completed
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Configuration used for generation
    /// </summary>
    public string ConfigurationUsed { get; set; } = string.Empty;

    /// <summary>
    /// Output path where tests were generated
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Paths of generated test files
    /// </summary>
    public List<string> GeneratedFilePaths { get; set; } = new();

    /// <summary>
    /// Files that were backed up before overwriting
    /// </summary>
    public Dictionary<string, string> BackupFiles { get; set; } = new();

    /// <summary>
    /// Validation results for generated tests
    /// </summary>
    public Dictionary<string, bool> ValidationResults { get; set; } = new();
}
