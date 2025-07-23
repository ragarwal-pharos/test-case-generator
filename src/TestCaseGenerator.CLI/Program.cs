using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using TestCaseGenerator.Analyzers.CSharp;
using TestCaseGenerator.Analyzers.Common;
using TestCaseGenerator.Core.Configuration;
using TestCaseGenerator.Core.Engine;
using TestCaseGenerator.Core.Interfaces;
using TestCaseGenerator.Core.Models;
using TestCaseGenerator.Generators.CSharp;
using TestCaseGenerator.Templates.Engine;

namespace TestCaseGenerator.CLI;

/// <summary>
/// Main entry point for the Test Case Generator CLI
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            // Configure services
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Parse command line arguments and execute
            var result = await Parser.Default.ParseArguments<GenerateOptions, InitOptions, ValidateOptions>(args)
                .MapResult(
                    (GenerateOptions opts) => HandleGenerateCommand(opts, serviceProvider),
                    (InitOptions opts) => HandleInitCommand(opts, serviceProvider),
                    (ValidateOptions opts) => HandleValidateCommand(opts, serviceProvider),
                    errs => Task.FromResult(1)
                );

            return result;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    /// <summary>
    /// Configures dependency injection services
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // Core services
        services.AddSingleton<TestGeneratorEngine>();
        services.AddSingleton<IFileProcessor, FileProcessor>();
        services.AddSingleton<ITemplateEngine, MustacheTemplateEngine>();
        services.AddSingleton<Core.Interfaces.IConfigurationProvider, JsonConfigurationProvider>();

        // Analyzers
        services.AddSingleton<ICodeAnalyzer, CSharpAnalyzer>();

        // Generators
        services.AddSingleton<ITestGenerator, CSharpTestGenerator>();

        // CLI services
        services.AddSingleton<ConsoleService>();
        services.AddSingleton<ProgressService>();
        services.AddSingleton<ReportingService>();
    }

    /// <summary>
    /// Handles the generate command
    /// </summary>
    private static async Task<int> HandleGenerateCommand(GenerateOptions options, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var engine = serviceProvider.GetRequiredService<TestGeneratorEngine>();
        var configProvider = serviceProvider.GetRequiredService<Core.Interfaces.IConfigurationProvider>();
        var consoleService = serviceProvider.GetRequiredService<ConsoleService>();
        var progressService = serviceProvider.GetRequiredService<ProgressService>();
        var reportingService = serviceProvider.GetRequiredService<ReportingService>();

        try
        {
            consoleService.WriteHeader("Test Case Generator");

            // Load configuration
            var config = await LoadConfigurationAsync(options, configProvider, logger, serviceProvider);
            if (config == null)
            {
                return 1;
            }

            // Validate options
            if (!ValidateGenerateOptions(options, consoleService))
            {
                return 1;
            }

            // Create generation request
            var request = CreateGenerationRequest(options, config);

            // Execute generation with detailed progress reporting
            var result = await progressService.ExecuteWithProgressAsync(
                "🧪 Generating test cases...",
                async (progress) =>
                {
                    return await engine.GenerateTestsAsync(request, progress);
                });

            // Report results
            await reportingService.ReportResultsAsync(result, options.OutputPath);

            if (result.Success)
            {
                consoleService.WriteSuccess($"✅ Successfully generated {result.Statistics.TestCasesGenerated} test cases");
                consoleService.WriteInfo($"📁 Output directory: {options.OutputPath}");
                consoleService.WriteInfo($"⏱️  Duration: {result.Duration.TotalSeconds:F2} seconds");
                return 0;
            }
            else
            {
                consoleService.WriteError("❌ Test generation failed");
                foreach (var error in result.Errors)
                {
                    consoleService.WriteError($"   • {error}");
                }
                return 1;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during test generation");
            consoleService.WriteError($"❌ Unexpected error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Handles the init command
    /// </summary>
    private static async Task<int> HandleInitCommand(InitOptions options, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var fileProcessor = serviceProvider.GetRequiredService<IFileProcessor>();
        var configProvider = serviceProvider.GetRequiredService<Core.Interfaces.IConfigurationProvider>();
        var consoleService = serviceProvider.GetRequiredService<ConsoleService>();

        try
        {
            consoleService.WriteHeader("Initialize Test Case Generator");

            var projectPath = options.ProjectPath ?? Directory.GetCurrentDirectory();
            var configPath = Path.Combine(projectPath, "testgen.config.json");

            if (File.Exists(configPath) && !options.Force)
            {
                consoleService.WriteWarning($"Configuration file already exists: {configPath}");
                consoleService.WriteInfo("Use --force to overwrite existing configuration");
                return 1;
            }

            consoleService.WriteInfo("🔍 Analyzing project structure...");
            var projectStructure = await fileProcessor.AnalyzeProjectStructureAsync(projectPath);

            consoleService.WriteInfo("⚙️  Creating default configuration...");
            var config = configProvider.CreateDefaultConfiguration(projectPath, projectStructure);

            consoleService.WriteInfo($"💾 Saving configuration to {configPath}...");
            await configProvider.SaveConfigurationAsync(config, configPath);

            consoleService.WriteSuccess($"✅ Configuration created successfully!");
            consoleService.WriteInfo($"📝 Edit {configPath} to customize settings");

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during initialization");
            consoleService.WriteError($"❌ Initialization failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Handles the validate command
    /// </summary>
    private static async Task<int> HandleValidateCommand(ValidateOptions options, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var configProvider = serviceProvider.GetRequiredService<Core.Interfaces.IConfigurationProvider>();
        var consoleService = serviceProvider.GetRequiredService<ConsoleService>();

        try
        {
            consoleService.WriteHeader("Validate Configuration");

            var configPath = options.ConfigPath ?? Path.Combine(Directory.GetCurrentDirectory(), "testgen.config.json");

            if (!File.Exists(configPath))
            {
                consoleService.WriteError($"❌ Configuration file not found: {configPath}");
                return 1;
            }

            consoleService.WriteInfo($"🔍 Validating configuration: {configPath}");
            var config = await configProvider.LoadConfigurationAsync(configPath);
            var validationResult = configProvider.ValidateConfiguration(config);

            if (validationResult.IsValid)
            {
                consoleService.WriteSuccess("✅ Configuration is valid");
                return 0;
            }
            else
            {
                consoleService.WriteError("❌ Configuration validation failed");
                
                if (validationResult.Errors.Any())
                {
                    consoleService.WriteError("Errors:");
                    foreach (var error in validationResult.Errors)
                    {
                        consoleService.WriteError($"   • {error}");
                    }
                }

                if (validationResult.Warnings.Any())
                {
                    consoleService.WriteWarning("Warnings:");
                    foreach (var warning in validationResult.Warnings)
                    {
                        consoleService.WriteWarning($"   • {warning}");
                    }
                }

                return 1;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during validation");
            consoleService.WriteError($"❌ Validation failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Loads configuration from file or creates default
    /// </summary>
    private static async Task<TestGeneratorConfig?> LoadConfigurationAsync(
        GenerateOptions options, 
        Core.Interfaces.IConfigurationProvider configProvider, 
        ILogger logger,
        IServiceProvider serviceProvider)
    {
        try
        {
            if (!string.IsNullOrEmpty(options.ConfigPath))
            {
                if (!File.Exists(options.ConfigPath))
                {
                    logger.LogError("Configuration file not found: {ConfigPath}", options.ConfigPath);
                    return null;
                }

                return await configProvider.LoadConfigurationAsync(options.ConfigPath);
            }

            var defaultConfigPath = Path.Combine(options.ProjectPath ?? Directory.GetCurrentDirectory(), "testgen.config.json");
            if (File.Exists(defaultConfigPath))
            {
                return await configProvider.LoadConfigurationAsync(defaultConfigPath);
            }

            // Create default configuration
            logger.LogDebug("No configuration file found, using default settings");
            var projectPath = options.ProjectPath ?? Directory.GetCurrentDirectory();
            var fileProcessor = new FileProcessor(serviceProvider.GetRequiredService<ILogger<FileProcessor>>());
            var projectStructure = await fileProcessor.AnalyzeProjectStructureAsync(projectPath);
            
            return configProvider.CreateDefaultConfiguration(projectPath, projectStructure);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading configuration");
            return null;
        }
    }

    /// <summary>
    /// Validates generate command options
    /// </summary>
    private static bool ValidateGenerateOptions(GenerateOptions options, ConsoleService consoleService)
    {
        var isValid = true;

        if (string.IsNullOrEmpty(options.ProjectPath) || !Directory.Exists(options.ProjectPath))
        {
            consoleService.WriteError($"❌ Project path does not exist: {options.ProjectPath}");
            isValid = false;
        }

        if (string.IsNullOrEmpty(options.OutputPath))
        {
            consoleService.WriteWarning("⚠️  No output path specified, using default: ./GeneratedTests");
            options.OutputPath = "./GeneratedTests";
        }

        return isValid;
    }

    /// <summary>
    /// Creates a generation request from options and configuration
    /// </summary>
    private static GenerationRequest CreateGenerationRequest(GenerateOptions options, TestGeneratorConfig config)
    {
        var request = new GenerationRequest
        {
            ProjectPath = options.ProjectPath ?? Directory.GetCurrentDirectory(),
            OutputPath = options.OutputPath ?? config.Project.OutputPath,
            Configuration = config,
            AnalyzeExistingTests = !options.IgnoreExistingTests,
            OverwriteExisting = options.Overwrite,
            CreateBackups = !options.NoBackup,
            ValidateGenerated = !options.SkipValidation
        };

        // Set file types to analyze
        if (options.FileTypes?.Any() == true)
        {
            request.FileTypes = options.FileTypes.ToList();
        }
        else
        {
            request.FileTypes = config.FileTypes.Where(ft => ft.Value.Enabled).Select(ft => ft.Key).ToList();
        }

        // Set specific files if provided
        if (options.Files?.Any() == true)
        {
            request.FilesToAnalyze = options.Files.ToList();
        }

        return request;
    }
}

/// <summary>
/// Command line options for the generate command
/// </summary>
[Verb("generate", HelpText = "Generate test cases for a project")]
public class GenerateOptions
{
    [Option('p', "project", Required = false, HelpText = "Path to the project to analyze")]
    public string? ProjectPath { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output directory for generated tests")]
    public string? OutputPath { get; set; }

    [Option('c', "config", Required = false, HelpText = "Path to configuration file")]
    public string? ConfigPath { get; set; }

    [Option('t', "types", Required = false, HelpText = "File types to analyze (comma-separated)")]
    public IEnumerable<string>? FileTypes { get; set; }

    [Option('f', "files", Required = false, HelpText = "Specific files to analyze (comma-separated)")]
    public IEnumerable<string>? Files { get; set; }

    [Option("overwrite", Required = false, HelpText = "Overwrite existing test files")]
    public bool Overwrite { get; set; }

    [Option("no-backup", Required = false, HelpText = "Don't create backups of existing files")]
    public bool NoBackup { get; set; }

    [Option("ignore-existing", Required = false, HelpText = "Don't analyze existing test files")]
    public bool IgnoreExistingTests { get; set; }

    [Option("skip-validation", Required = false, HelpText = "Skip validation of generated tests")]
    public bool SkipValidation { get; set; }

    [Option("silent", Required = false, HelpText = "Run in silent mode")]
    public bool Silent { get; set; }
}

/// <summary>
/// Command line options for the init command
/// </summary>
[Verb("init", HelpText = "Initialize configuration for a project")]
public class InitOptions
{
    [Option('p', "project", Required = false, HelpText = "Path to the project")]
    public string? ProjectPath { get; set; }

    [Option("force", Required = false, HelpText = "Overwrite existing configuration")]
    public bool Force { get; set; }
}

/// <summary>
/// Command line options for the validate command
/// </summary>
[Verb("validate", HelpText = "Validate configuration file")]
public class ValidateOptions
{
    [Option('c', "config", Required = false, HelpText = "Path to configuration file")]
    public string? ConfigPath { get; set; }
}
