namespace TestCaseGenerator.Core.Models;

/// <summary>
/// Statistics about test generation
/// </summary>
public class GenerationStatistics
{
    /// <summary>
    /// Number of files analyzed
    /// </summary>
    public int FilesAnalyzed { get; set; }

    /// <summary>
    /// Number of test cases generated
    /// </summary>
    public int TestCasesGenerated { get; set; }

    /// <summary>
    /// Number of test methods generated
    /// </summary>
    public int TestMethodsGenerated { get; set; }

    /// <summary>
    /// Estimated code coverage percentage
    /// </summary>
    public double CoveragePercentage { get; set; }

    /// <summary>
    /// Time spent on analysis
    /// </summary>
    public TimeSpan AnalysisTime { get; set; }

    /// <summary>
    /// Time spent on generation
    /// </summary>
    public TimeSpan GenerationTime { get; set; }

    /// <summary>
    /// Time spent on validation
    /// </summary>
    public TimeSpan ValidationTime { get; set; }

    /// <summary>
    /// Number of test files created
    /// </summary>
    public int TestFilesCreated { get; set; }

    /// <summary>
    /// Number of test files updated
    /// </summary>
    public int TestFilesUpdated { get; set; }

    /// <summary>
    /// Number of classes covered by tests
    /// </summary>
    public int ClassesCovered { get; set; }

    /// <summary>
    /// Number of methods covered by tests
    /// </summary>
    public int MethodsCovered { get; set; }

    /// <summary>
    /// Timing breakdown for different phases
    /// </summary>
    public Dictionary<string, TimeSpan> PhaseTimings { get; set; } = new();

    /// <summary>
    /// Number of existing test files found
    /// </summary>
    public int ExistingTestFiles { get; set; }

    /// <summary>
    /// Number of existing test methods found
    /// </summary>
    public int ExistingTestMethods { get; set; }

    /// <summary>
    /// Number of dependencies detected
    /// </summary>
    public int DependenciesDetected { get; set; }

    /// <summary>
    /// Number of mock objects created
    /// </summary>
    public int MocksGenerated { get; set; }

    /// <summary>
    /// Number of assertions generated
    /// </summary>
    public int AssertionsGenerated { get; set; }

    /// <summary>
    /// Number of files skipped due to errors
    /// </summary>
    public int FilesSkipped { get; set; }

    /// <summary>
    /// Number of warnings encountered
    /// </summary>
    public int WarningsCount { get; set; }

    /// <summary>
    /// Breakdown of file types analyzed
    /// </summary>
    public Dictionary<string, int> FileTypeBreakdown { get; set; } = new();

    /// <summary>
    /// Breakdown of test frameworks used
    /// </summary>
    public Dictionary<string, int> TestFrameworkBreakdown { get; set; } = new();

    /// <summary>
    /// Memory usage during generation
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// Peak memory usage during generation
    /// </summary>
    public long PeakMemoryUsageBytes { get; set; }
}
