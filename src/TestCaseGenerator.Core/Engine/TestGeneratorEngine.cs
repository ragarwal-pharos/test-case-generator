using Microsoft.Extensions.Logging;
using TestCaseGenerator.Core.Configuration;
using TestCaseGenerator.Core.Interfaces;
using TestCaseGenerator.Core.Models;
using System.Diagnostics;

namespace TestCaseGenerator.Core.Engine;

/// <summary>
/// Main engine for test case generation that orchestrates the entire process
/// </summary>
public class TestGeneratorEngine
{
    private readonly ILogger<TestGeneratorEngine> _logger;
    private readonly IFileProcessor _fileProcessor;
    private readonly IEnumerable<ICodeAnalyzer> _analyzers;
    private readonly IEnumerable<ITestGenerator> _generators;
    private readonly ITemplateEngine _templateEngine;
    private readonly IConfigurationProvider _configurationProvider;

    public TestGeneratorEngine(
        ILogger<TestGeneratorEngine> logger,
        IFileProcessor fileProcessor,
        IEnumerable<ICodeAnalyzer> analyzers,
        IEnumerable<ITestGenerator> generators,
        ITemplateEngine templateEngine,
        IConfigurationProvider configurationProvider)
    {
        _logger = logger;
        _fileProcessor = fileProcessor;
        _analyzers = analyzers;
        _generators = generators;
        _templateEngine = templateEngine;
        _configurationProvider = configurationProvider;
    }

    /// <summary>
    /// Generates test cases for a project based on the provided request
    /// </summary>
    /// <param name="request">Generation request containing project path and configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generation result with statistics and generated files</returns>
    public async Task<GenerationResult> GenerateTestsAsync(GenerationRequest request, CancellationToken cancellationToken = default)
    {
        return await GenerateTestsAsync(request, null, cancellationToken);
    }

    /// <summary>
    /// Generates test cases for a project based on the provided request with progress reporting
    /// </summary>
    /// <param name="request">Generation request containing project path and configuration</param>
    /// <param name="progress">Progress reporter for phase updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generation result with statistics and generated files</returns>
    public async Task<GenerationResult> GenerateTestsAsync(GenerationRequest request, IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new GenerationResult();
        var phaseTimings = new Dictionary<string, TimeSpan>();

        try
        {
            _logger.LogDebug("Starting test generation for project: {ProjectPath}", request.ProjectPath);

            // Phase 1: Analyze project structure
            var structureStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Phase 1: Analyzing project structure...");
            progress?.Report("📋 Analyzing project structure...");
            
            var projectStructure = await _fileProcessor.AnalyzeProjectStructureAsync(request.ProjectPath, cancellationToken);
            request.Configuration.Project.Name = projectStructure.ProjectName;
            
            phaseTimings["ProjectStructureAnalysis"] = structureStopwatch.Elapsed;
            _logger.LogDebug("Project structure analysis completed in {Duration}ms", structureStopwatch.ElapsedMilliseconds);

            // Phase 2: Analyze existing tests (if requested)
            ExistingTestInfo? existingTests = null;
            if (request.AnalyzeExistingTests)
            {
                var existingTestsStopwatch = Stopwatch.StartNew();
                _logger.LogDebug("Phase 2: Analyzing existing tests...");
                progress?.Report("🔍 Analyzing existing tests...");
                
                existingTests = await _fileProcessor.AnalyzeExistingTestsAsync(request.ProjectPath, cancellationToken);
                
                phaseTimings["ExistingTestsAnalysis"] = existingTestsStopwatch.Elapsed;
                _logger.LogDebug("Existing tests analysis completed in {Duration}ms. Found {TestCount} existing test files", 
                    existingTestsStopwatch.ElapsedMilliseconds, existingTests.TestFiles.Count);
            }

            // Phase 3: Discover files to analyze
            var discoveryStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Phase 3: Discovering files to analyze...");
            progress?.Report("📁 Discovering files to analyze...");
            
            var filesToAnalyze = request.FilesToAnalyze?.Any() == true 
                ? request.FilesToAnalyze 
                : await _fileProcessor.DiscoverFilesAsync(
                    request.ProjectPath, 
                    request.FileTypes, 
                    request.Configuration.Analysis.ExcludePatterns, 
                    cancellationToken);

            phaseTimings["FileDiscovery"] = discoveryStopwatch.Elapsed;
            _logger.LogDebug("File discovery completed in {Duration}ms. Found {FileCount} files to analyze", 
                discoveryStopwatch.ElapsedMilliseconds, filesToAnalyze.Count);

            result.Statistics.FilesAnalyzed = filesToAnalyze.Count;

            // Phase 4: Analyze source files
            var analysisStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Phase 4: Analyzing source files...");
            progress?.Report("🔬 Analyzing source files...");
            
            var analysisResults = await AnalyzeFilesAsync(filesToAnalyze, projectStructure, existingTests, cancellationToken);
            
            phaseTimings["SourceAnalysis"] = analysisStopwatch.Elapsed;
            _logger.LogDebug("Source analysis completed in {Duration}ms. Analyzed {ResultCount} files successfully", 
                analysisStopwatch.ElapsedMilliseconds, analysisResults.Count);

            // Phase 5: Generate test cases
            var generationStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Phase 5: Generating test cases...");
            progress?.Report("⚡ Generating test cases...");
            
            var generatedFiles = await GenerateTestFilesAsync(analysisResults, request.OutputPath, cancellationToken);
            
            phaseTimings["TestGeneration"] = generationStopwatch.Elapsed;
            _logger.LogDebug("Test generation completed in {Duration}ms. Generated {FileCount} test files", 
                generationStopwatch.ElapsedMilliseconds, generatedFiles.Count);

            // Phase 6: Validate generated tests (if requested)
            if (request.ValidateGenerated)
            {
                var validationStopwatch = Stopwatch.StartNew();
                _logger.LogDebug("Phase 6: Validating generated tests...");
                progress?.Report("✅ Validating generated tests...");
                
                await ValidateGeneratedTestsAsync(generatedFiles, cancellationToken);
                
                phaseTimings["Validation"] = validationStopwatch.Elapsed;
                _logger.LogDebug("Test validation completed in {Duration}ms", validationStopwatch.ElapsedMilliseconds);
            }

            // Populate result
            result.Success = true;
            result.GeneratedFiles = generatedFiles;
            result.TestCases = generatedFiles.SelectMany(f => f.TestCases).ToList();
            result.Statistics.TestCasesGenerated = result.TestCases.Count;
            result.Statistics.TestFilesCreated = generatedFiles.Count(f => f.IsNewFile);
            result.Statistics.TestFilesUpdated = generatedFiles.Count(f => !f.IsNewFile);
            result.Statistics.PhaseTimings = phaseTimings;

            // Calculate coverage statistics
            var uniqueClasses = analysisResults.SelectMany(r => r.Classes).Select(c => c.FullName).Distinct().Count();
            var uniqueMethods = analysisResults.SelectMany(r => r.Methods).Count();
            
            result.Statistics.ClassesCovered = uniqueClasses;
            result.Statistics.MethodsCovered = uniqueMethods;
            result.Statistics.MocksGenerated = result.TestCases.Count(tc => tc.MockSetup.Any());

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogDebug("Test generation completed successfully in {Duration}ms. Generated {TestCases} test cases across {Files} files", 
                stopwatch.ElapsedMilliseconds, result.Statistics.TestCasesGenerated, result.Statistics.TestFilesCreated + result.Statistics.TestFilesUpdated);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during test generation");
            result.Success = false;
            result.Errors.Add($"Test generation failed: {ex.Message}");
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    /// <summary>
    /// Analyzes source files using appropriate analyzers
    /// </summary>
    private async Task<List<AnalysisResult>> AnalyzeFilesAsync(
        List<string> filesToAnalyze, 
        ProjectStructure projectStructure, 
        ExistingTestInfo? existingTests,
        CancellationToken cancellationToken)
    {
        var results = new List<AnalysisResult>();
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

        var analysisTasks = filesToAnalyze.Select(async file =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var analyzer = _analyzers.FirstOrDefault(a => a.CanAnalyze(file));
                if (analyzer == null)
                {
                    _logger.LogWarning("No analyzer found for file: {FilePath}", file);
                    return null;
                }

                _logger.LogDebug("Analyzing file {FilePath} with {AnalyzerName}", file, analyzer.Name);
                var analysisResult = await analyzer.AnalyzeAsync(file, cancellationToken);
                
                // Enrich analysis result with project structure and existing test information
                analysisResult.ProjectStructure = projectStructure;
                analysisResult.ExistingTests = existingTests;

                return analysisResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing file {FilePath}", file);
                return null;
            }
            finally
            {
                semaphore.Release();
            }
        });

        var analysisResults = await Task.WhenAll(analysisTasks);
        results.AddRange(analysisResults.Where(r => r != null)!);

        return results;
    }

    /// <summary>
    /// Generates test files using appropriate generators
    /// </summary>
    private async Task<List<GeneratedTestFile>> GenerateTestFilesAsync(
        List<AnalysisResult> analysisResults, 
        string outputPath, 
        CancellationToken cancellationToken)
    {
        var generatedFiles = new List<GeneratedTestFile>();

        // Group analysis results by generator to avoid multiple cleanup calls
        var generatorGroups = analysisResults
            .Select(result => new { Result = result, Generator = _generators.FirstOrDefault(g => g.CanGenerate(result)) })
            .Where(item => item.Generator != null)
            .GroupBy(item => item.Generator)
            .ToList();

        foreach (var group in generatorGroups)
        {
            var generator = group.Key;
            var results = group.Select(item => item.Result).ToList();

            if (generator == null) continue; // Should not happen due to Where filter, but for safety

            try
            {
                _logger.LogDebug("Generating tests for {FileCount} files with {GeneratorName}", results.Count, generator.Name);
                var files = await generator.GenerateTestFilesAsync(results, outputPath, cancellationToken);
                generatedFiles.AddRange(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tests with {GeneratorName}", generator.Name);
            }
        }

        return generatedFiles;
    }

    /// <summary>
    /// Validates generated test files
    /// </summary>
    private async Task ValidateGeneratedTestsAsync(List<GeneratedTestFile> generatedFiles, CancellationToken cancellationToken)
    {
        foreach (var file in generatedFiles.Where(f => f.RequiresCompilation))
        {
            try
            {
                // Basic validation - check if the file can be parsed
                // TODO: Implement actual compilation validation
                _logger.LogDebug("Validating generated test file: {FilePath}", file.FilePath);
                
                if (string.IsNullOrWhiteSpace(file.Content))
                {
                    _logger.LogWarning("Generated test file is empty: {FilePath}", file.FilePath);
                    continue;
                }

                // Check for basic syntax issues
                if (!file.Content.Contains("namespace") || !file.Content.Contains("class"))
                {
                    _logger.LogWarning("Generated test file may have syntax issues: {FilePath}", file.FilePath);
                }

                await Task.Delay(1, cancellationToken); // Yield control
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating test file {FilePath}", file.FilePath);
            }
        }
    }
}
