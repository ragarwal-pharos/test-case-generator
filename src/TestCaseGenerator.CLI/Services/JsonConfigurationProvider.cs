using System.Text.Json;
using TestCaseGenerator.Core.Configuration;
using TestCaseGenerator.Core.Interfaces;
using TestCaseGenerator.Core.Models;

namespace TestCaseGenerator.CLI;

/// <summary>
/// JSON-based configuration provider
/// </summary>
public class JsonConfigurationProvider : Core.Interfaces.IConfigurationProvider
{
    /// <summary>
    /// Loads configuration from a JSON file
    /// </summary>
    public async Task<TestGeneratorConfig> LoadConfigurationAsync(string configPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var json = await File.ReadAllTextAsync(configPath, cancellationToken);
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var config = JsonSerializer.Deserialize<TestGeneratorConfig>(json, options);
        
        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize configuration");
        }

        return config;
    }

    /// <summary>
    /// Saves configuration to a JSON file
    /// </summary>
    public async Task SaveConfigurationAsync(TestGeneratorConfig config, string configPath, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(config, options);
        
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(configPath, json, cancellationToken);
    }

    /// <summary>
    /// Creates a default configuration based on project analysis
    /// </summary>
    public TestGeneratorConfig CreateDefaultConfiguration(string projectPath, ProjectStructure projectStructure)
    {
        var config = new TestGeneratorConfig
        {
            Project = new ProjectConfig
            {
                Name = projectStructure.ProjectName,
                RootPath = projectPath,
                OutputPath = "./GeneratedTests",
                TestFrameworks = new Dictionary<string, string>
                {
                    { "csharp", DetermineTestFramework(projectStructure) },
                    { "typescript", "jest" }
                },
                SourceDirectories = new List<string> { "." }
            },
            
            FileTypes = CreateFileTypeConfigs(),
            
            Generation = new GenerationConfig
            {
                TestNamingConvention = "MethodName_Scenario_ExpectedResult",
                IncludeArrangeActAssert = true,
                GenerateTestData = true,
                GenerateNegativeTests = true,
                IncludeDocumentation = true,
                MockFramework = DetermineMockFramework(projectStructure)
            },
            
            Templates = new Dictionary<string, Dictionary<string, string>>(),
            
            Analysis = new AnalysisConfig
            {
                IncludePrivateMethods = false,
                IncludeInternalMethods = true,
                GenerateMocks = true,
                AnalyzeDependencies = true,
                MaxDepth = 3
            },
            
            Output = new OutputConfig
            {
                OverwriteExisting = false,
                CreateBackups = true,
                GenerateReports = true,
                ReportFormats = new List<string> { "json" },
                GroupTestsByType = true,
                TestFileNaming = "{SourceFileName}Tests.{Extension}"
            },
            
            Performance = new PerformanceConfig
            {
                MaxConcurrency = Environment.ProcessorCount,
                EnableCaching = true,
                CacheDirectory = "./.testgen-cache",
                TimeoutSeconds = 300
            }
        };

        return config;
    }

    /// <summary>
    /// Validates a configuration
    /// </summary>
    public ValidationResult ValidateConfiguration(TestGeneratorConfig config)
    {
        var result = new ValidationResult { IsValid = true };

        // Validate project configuration
        if (string.IsNullOrEmpty(config.Project.Name))
        {
            result.Errors.Add("Project name is required");
            result.IsValid = false;
        }

        if (string.IsNullOrEmpty(config.Project.RootPath) || !Directory.Exists(config.Project.RootPath))
        {
            result.Errors.Add("Project root path must exist");
            result.IsValid = false;
        }

        if (string.IsNullOrEmpty(config.Project.OutputPath))
        {
            result.Errors.Add("Output path is required");
            result.IsValid = false;
        }

        // Validate file types
        if (!config.FileTypes.Any(ft => ft.Value.Enabled))
        {
            result.Warnings.Add("No file types are enabled for analysis");
        }

        // Validate generation settings - removed invalid property check
        
        // Validate performance settings
        if (config.Performance.MaxConcurrency <= 0)
        {
            result.Errors.Add("MaxConcurrency must be greater than 0");
            result.IsValid = false;
        }

        if (config.Performance.TimeoutSeconds <= 0)
        {
            result.Errors.Add("TimeoutSeconds must be greater than 0");
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// Creates default file type configurations
    /// </summary>
    private Dictionary<string, FileTypeConfig> CreateFileTypeConfigs()
    {
        return new Dictionary<string, FileTypeConfig>
        {
            ["cs"] = new FileTypeConfig
            {
                Enabled = true,
                Extensions = new List<string> { ".cs" },
                ExcludePatterns = new List<string> { "**/bin/**", "**/obj/**", "**/*Tests.cs", "**/*Test.cs" }
            },
            ["ts"] = new FileTypeConfig
            {
                Enabled = true,
                Extensions = new List<string> { ".ts" },
                ExcludePatterns = new List<string> { "**/node_modules/**", "**/*.d.ts", "**/*.spec.ts", "**/*.test.ts" }
            },
            ["html"] = new FileTypeConfig
            {
                Enabled = true,
                Extensions = new List<string> { ".html" },
                ExcludePatterns = new List<string> { "**/node_modules/**", "**/dist/**" }
            },
            ["less"] = new FileTypeConfig
            {
                Enabled = true,
                Extensions = new List<string> { ".less" },
                ExcludePatterns = new List<string> { "**/node_modules/**", "**/dist/**" }
            },
            ["css"] = new FileTypeConfig
            {
                Enabled = true,
                Extensions = new List<string> { ".css" },
                ExcludePatterns = new List<string> { "**/node_modules/**", "**/dist/**" }
            }
        };
    }

    /// <summary>
    /// Determines the best test framework based on project structure
    /// </summary>
    private string DetermineTestFramework(ProjectStructure projectStructure)
    {
        if (projectStructure.NuGetInfo.TestFrameworks.Any(tf => tf.Type == TestFrameworkType.XUnit))
            return "xUnit";
        
        if (projectStructure.NuGetInfo.TestFrameworks.Any(tf => tf.Type == TestFrameworkType.NUnit))
            return "NUnit";
            
        if (projectStructure.NuGetInfo.TestFrameworks.Any(tf => tf.Type == TestFrameworkType.MSTest))
            return "MSTest";

        // Default to xUnit
        return "xUnit";
    }

    /// <summary>
    /// Determines the best mocking framework based on project structure
    /// </summary>
    private string DetermineMockFramework(ProjectStructure projectStructure)
    {
        if (projectStructure.NuGetInfo.MockingFrameworks.Any(mf => mf.Type == MockingFrameworkType.Moq))
            return "Moq";
            
        if (projectStructure.NuGetInfo.MockingFrameworks.Any(mf => mf.Type == MockingFrameworkType.NSubstitute))
            return "NSubstitute";
            
        if (projectStructure.NuGetInfo.MockingFrameworks.Any(mf => mf.Type == MockingFrameworkType.FakeItEasy))
            return "FakeItEasy";

        // Default to Moq
        return "Moq";
    }
}
