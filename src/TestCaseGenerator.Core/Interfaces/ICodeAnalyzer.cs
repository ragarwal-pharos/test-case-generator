using TestCaseGenerator.Core.Models;
using TestCaseGenerator.Core.Configuration;

namespace TestCaseGenerator.Core.Interfaces;

/// <summary>
/// Interface for analyzing source code files
/// </summary>
public interface ICodeAnalyzer
{
    /// <summary>
    /// Determines if this analyzer can analyze the given file
    /// </summary>
    /// <param name="filePath">Path to the file to analyze</param>
    /// <returns>True if the analyzer can handle this file type</returns>
    bool CanAnalyze(string filePath);

    /// <summary>
    /// Analyzes a source code file and returns analysis results
    /// </summary>
    /// <param name="filePath">Path to the file to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis results</returns>
    Task<AnalysisResult> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes multiple files in batch
    /// </summary>
    /// <param name="filePaths">Paths to the files to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis results for each file</returns>
    Task<List<AnalysisResult>> AnalyzeBatchAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the supported file extensions for this analyzer
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }

    /// <summary>
    /// Gets the analyzer name
    /// </summary>
    string Name { get; }
}

/// <summary>
/// Interface for generating test cases
/// </summary>
public interface ITestGenerator
{
    /// <summary>
    /// Determines if this generator can generate tests for the given analysis result
    /// </summary>
    /// <param name="analysisResult">Analysis result to check</param>
    /// <returns>True if the generator can handle this analysis result</returns>
    bool CanGenerate(AnalysisResult analysisResult);

    /// <summary>
    /// Generates test cases based on analysis results
    /// </summary>
    /// <param name="analysisResult">Analysis result to generate tests for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated test cases</returns>
    Task<List<TestCase>> GenerateTestsAsync(AnalysisResult analysisResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates test files based on analysis results
    /// </summary>
    /// <param name="analysisResults">Analysis results to generate tests for</param>
    /// <param name="outputPath">Path where test files should be generated</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated test files</returns>
    Task<List<GeneratedTestFile>> GenerateTestFilesAsync(
        IEnumerable<AnalysisResult> analysisResults, 
        string outputPath, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the supported test framework for this generator
    /// </summary>
    string TestFramework { get; }

    /// <summary>
    /// Gets the generator name
    /// </summary>
    string Name { get; }
}

/// <summary>
/// Interface for processing files in a project
/// </summary>
public interface IFileProcessor
{
    /// <summary>
    /// Discovers files in a project that can be analyzed
    /// </summary>
    /// <param name="projectPath">Path to the project</param>
    /// <param name="fileTypes">File types to include</param>
    /// <param name="excludePatterns">Patterns to exclude</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of files to analyze</returns>
    Task<List<string>> DiscoverFilesAsync(
        string projectPath, 
        IEnumerable<string> fileTypes, 
        IEnumerable<string> excludePatterns, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes the project structure
    /// </summary>
    /// <param name="projectPath">Path to the project</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Project structure information</returns>
    Task<ProjectStructure> AnalyzeProjectStructureAsync(string projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers existing test files and analyzes them
    /// </summary>
    /// <param name="projectPath">Path to the project</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Information about existing tests</returns>
    Task<ExistingTestInfo> AnalyzeExistingTestsAsync(string projectPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for template engine operations
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// Renders a template with the given data
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="data">Data to render the template with</param>
    /// <returns>Rendered template content</returns>
    Task<string> RenderTemplateAsync(string templateName, object data);

    /// <summary>
    /// Registers a custom template
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="templateContent">Template content</param>
    void RegisterTemplate(string templateName, string templateContent);

    /// <summary>
    /// Loads templates from a directory
    /// </summary>
    /// <param name="templateDirectory">Directory containing templates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LoadTemplatesAsync(string templateDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available template names
    /// </summary>
    IEnumerable<string> AvailableTemplates { get; }
}

/// <summary>
/// Interface for configuration providers
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Loads configuration from a file
    /// </summary>
    /// <param name="configFilePath">Path to the configuration file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Loaded configuration</returns>
    Task<TestGeneratorConfig> LoadConfigurationAsync(string configFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves configuration to a file
    /// </summary>
    /// <param name="config">Configuration to save</param>
    /// <param name="configFilePath">Path where to save the configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveConfigurationAsync(TestGeneratorConfig config, string configFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a default configuration for a project
    /// </summary>
    /// <param name="projectPath">Path to the project</param>
    /// <param name="projectStructure">Project structure information</param>
    /// <returns>Default configuration</returns>
    TestGeneratorConfig CreateDefaultConfiguration(string projectPath, ProjectStructure projectStructure);

    /// <summary>
    /// Validates a configuration
    /// </summary>
    /// <param name="config">Configuration to validate</param>
    /// <returns>Validation results</returns>
    ValidationResult ValidateConfiguration(TestGeneratorConfig config);
}

/// <summary>
/// Validation result for configuration
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
