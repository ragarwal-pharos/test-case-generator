using Spectre.Console;
using TestCaseGenerator.Core.Models;

namespace TestCaseGenerator.CLI;

/// <summary>
/// Service for generating and displaying reports
/// </summary>
public class ReportingService
{
    private readonly ConsoleService _consoleService;

    public ReportingService(ConsoleService consoleService)
    {
        _consoleService = consoleService;
    }

    /// <summary>
    /// Reports the results of test generation
    /// </summary>
    public async Task ReportResultsAsync(GenerationResult result, string outputPath)
    {
        await Task.Run(() =>
        {
            _consoleService.WriteRule("Generation Results");

            // Summary statistics
            DisplaySummaryStatistics(result);

            // Generated test files
            if (result.TestCases.Any())
            {
                DisplayGeneratedTests(result.TestCases);
            }

            // Analysis results - not available in GenerationResult
            // if (result.AnalysisResults.Any())
            // {
            //     DisplayAnalysisResults(result.AnalysisResults);
            // }

            // Errors and warnings
            if (result.Errors.Any() || result.Warnings.Any())
            {
                DisplayErrorsAndWarnings(result.Errors, result.Warnings);
            }

            // Performance metrics
            DisplayPerformanceMetrics(result);

            // File output summary
            DisplayFileOutputSummary(outputPath, result.GeneratedFiles.Count);
        });
    }

    /// <summary>
    /// Displays summary statistics
    /// </summary>
    private void DisplaySummaryStatistics(GenerationResult result)
    {
        var table = _consoleService.CreateTable();
        table.AddColumn("Metric");
        table.AddColumn(new TableColumn("Value").RightAligned());

        table.AddRow("Files Analyzed", result.Statistics.FilesAnalyzed.ToString());
        table.AddRow("Test Cases Generated", result.Statistics.TestCasesGenerated.ToString());
        table.AddRow("Test Methods Created", result.Statistics.TestMethodsGenerated.ToString());
        table.AddRow("Success Rate", result.Success ? "[green]100%[/]" : "[red]Failed[/]");
        table.AddRow("Duration", $"{result.Duration.TotalSeconds:F2}s");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays generated test files
    /// </summary>
    private void DisplayGeneratedTests(IEnumerable<TestCase> generatedTests)
    {
        _consoleService.WriteRule("Generated Test Files");

        var tree = new Tree("📁 Generated Tests");

        var groupedTests = generatedTests.GroupBy(t => t.TargetClass);
        
        foreach (var classGroup in groupedTests)
        {
            var classNode = tree.AddNode($"🏛️  {classGroup.Key}");
            
            foreach (var test in classGroup)
            {
                var testNode = classNode.AddNode($"🧪 {test.TestName}");
                testNode.AddNode($"📝 {test.Assertions.Count} assertion(s)");
                
                if (test.MockSetup.Any())
                {
                    testNode.AddNode($"🎭 {test.MockSetup.Count} mock(s)");
                }
            }
        }

        _consoleService.WriteTree(tree);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays analysis results
    /// </summary>
    private void DisplayAnalysisResults(IEnumerable<AnalysisResult> analysisResults)
    {
        _consoleService.WriteRule("Analysis Results");

        var table = _consoleService.CreateTable();
        table.AddColumn("File");
        table.AddColumn("Type");
        table.AddColumn("Classes");
        table.AddColumn("Methods");
        table.AddColumn("Properties");
        table.AddColumn("Dependencies");

        foreach (var result in analysisResults)
        {
            var fileName = Path.GetFileName(result.FilePath);
            var fileType = result.FileType.ToString();
            var classCount = result.Classes.Count.ToString();
            var methodCount = result.Classes.SelectMany(c => c.Methods).Count().ToString();
            var propertyCount = result.Classes.SelectMany(c => c.Properties).Count().ToString();
            var dependencyCount = result.Dependencies.Count.ToString();

            table.AddRow(fileName, fileType, classCount, methodCount, propertyCount, dependencyCount);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays errors and warnings
    /// </summary>
    private void DisplayErrorsAndWarnings(IEnumerable<string> errors, IEnumerable<string> warnings)
    {
        if (errors.Any())
        {
            _consoleService.WriteRule("Errors");
            foreach (var error in errors)
            {
                _consoleService.WriteError($"❌ {error}");
            }
            AnsiConsole.WriteLine();
        }

        if (warnings.Any())
        {
            _consoleService.WriteRule("Warnings");
            foreach (var warning in warnings)
            {
                _consoleService.WriteWarning($"⚠️  {warning}");
            }
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Displays performance metrics
    /// </summary>
    private void DisplayPerformanceMetrics(GenerationResult result)
    {
        _consoleService.WriteRule("Performance Metrics");

        var metrics = new Dictionary<string, string>
        {
            ["Total Duration"] = $"{result.Duration.TotalSeconds:F2} seconds",
            ["Analysis Time"] = $"{result.Statistics.AnalysisTime.TotalSeconds:F2} seconds",
            ["Generation Time"] = $"{result.Statistics.GenerationTime.TotalSeconds:F2} seconds",
            ["Files per Second"] = $"{(result.Statistics.FilesAnalyzed / Math.Max(result.Duration.TotalSeconds, 1)):F1}",
            ["Tests per Second"] = $"{(result.Statistics.TestCasesGenerated / Math.Max(result.Duration.TotalSeconds, 1)):F1}"
        };

        var table = _consoleService.CreateTable();
        table.AddColumn("Metric");
        table.AddColumn(new TableColumn("Value").RightAligned());

        foreach (var metric in metrics)
        {
            table.AddRow(metric.Key, metric.Value);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays file output summary
    /// </summary>
    private void DisplayFileOutputSummary(string outputPath, int fileCount)
    {
        _consoleService.WriteRule("Output Summary");

        var panel = _consoleService.CreatePanel(
            "📁 Output Location",
            $"[cyan]{outputPath}[/]\n[green]{fileCount} test file(s) generated[/]"
        );

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Exports results to a JSON report file
    /// </summary>
    public async Task ExportReportAsync(TestGenerationResult result, string outputPath, string format = "json")
    {
        var reportPath = format.ToLower() switch
        {
            "json" => Path.Combine(outputPath, "test-generation-report.json"),
            "html" => Path.Combine(outputPath, "test-generation-report.html"),
            "xml" => Path.Combine(outputPath, "test-generation-report.xml"),
            _ => Path.Combine(outputPath, "test-generation-report.json")
        };

        switch (format.ToLower())
        {
            case "json":
                await ExportJsonReportAsync(result, reportPath);
                break;
            case "html":
                await ExportHtmlReportAsync(result, reportPath);
                break;
            case "xml":
                await ExportXmlReportAsync(result, reportPath);
                break;
        }

        _consoleService.WriteInfo($"📄 Report exported to: {reportPath}");
    }

    private async Task ExportJsonReportAsync(TestGenerationResult result, string filePath)
    {
        var report = new
        {
            Timestamp = DateTime.UtcNow,
            Success = result.Success,
            Duration = result.Duration,
            Statistics = result.Statistics,
            GeneratedTests = result.GeneratedTests.Select(t => new
            {
                ClassName = t.TargetClass,
                MethodName = t.TargetMethod,
                TestFramework = t.Framework,
                TestCount = 1,
                AssertionCount = t.Assertions.Count
            }),
            AnalysisResults = result.AnalysisResults.Select(a => new
            {
                a.FilePath,
                a.FileType,
                ClassCount = a.Classes.Count,
                MethodCount = a.Classes.SelectMany(c => c.Methods).Count(),
                PropertyCount = a.Classes.SelectMany(c => c.Properties).Count(),
                DependencyCount = a.Dependencies.Count
            }),
            Errors = result.Errors,
            Warnings = result.Warnings
        };

        var json = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task ExportHtmlReportAsync(TestGenerationResult result, string filePath)
    {
        var html = GenerateHtmlReport(result);
        await File.WriteAllTextAsync(filePath, html);
    }

    private async Task ExportXmlReportAsync(TestGenerationResult result, string filePath)
    {
        var xml = GenerateXmlReport(result);
        await File.WriteAllTextAsync(filePath, xml);
    }

    private string GenerateHtmlReport(TestGenerationResult result)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Test Generation Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #f0f0f0; padding: 20px; border-radius: 5px; }}
        .statistics {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 10px; margin: 20px 0; }}
        .stat-card {{ background-color: #e8f4fd; padding: 15px; border-radius: 5px; text-align: center; }}
        .success {{ color: green; }}
        .error {{ color: red; }}
        .warning {{ color: orange; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Test Generation Report</h1>
        <p>Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
        <p>Status: <span class='{(result.Success ? "success" : "error")}'>{(result.Success ? "SUCCESS" : "FAILED")}</span></p>
    </div>

    <div class='statistics'>
        <div class='stat-card'>
            <h3>{result.Statistics.FilesAnalyzed}</h3>
            <p>Files Analyzed</p>
        </div>
        <div class='stat-card'>
            <h3>{result.Statistics.TestCasesGenerated}</h3>
            <p>Test Cases Generated</p>
        </div>
        <div class='stat-card'>
            <h3>{result.Statistics.TestMethodsGenerated}</h3>
            <p>Test Methods</p>
        </div>
        <div class='stat-card'>
            <h3>{result.Statistics.CoveragePercentage:F1}%</h3>
            <p>Coverage</p>
        </div>
    </div>

    <h2>Generated Tests</h2>
    <table>
        <tr>
            <th>Class</th>
            <th>Method</th>
            <th>Framework</th>
            <th>Test Methods</th>
        </tr>
        {string.Join("", result.GeneratedTests.Select(t => $@"
        <tr>
            <td>{t.TargetClass}</td>
            <td>{t.TargetMethod}</td>
            <td>{t.Framework}</td>
            <td>1</td>
        </tr>"))}
    </table>
</body>
</html>";
    }

    private string GenerateXmlReport(TestGenerationResult result)
    {
        return $@"<?xml version='1.0' encoding='UTF-8'?>
<TestGenerationReport>
    <Timestamp>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</Timestamp>
    <Success>{result.Success}</Success>
    <Duration>{result.Duration}</Duration>
    <Statistics>
        <FilesAnalyzed>{result.Statistics.FilesAnalyzed}</FilesAnalyzed>
        <TestCasesGenerated>{result.Statistics.TestCasesGenerated}</TestCasesGenerated>
        <TestMethodsGenerated>{result.Statistics.TestMethodsGenerated}</TestMethodsGenerated>
        <CoveragePercentage>{result.Statistics.CoveragePercentage}</CoveragePercentage>
    </Statistics>
    <GeneratedTests>
        {string.Join("", result.GeneratedTests.Select(t => $@"
        <TestCase>
            <ClassName>{t.TargetClass}</ClassName>
            <MethodName>{t.TargetMethod}</MethodName>
            <TestFramework>{t.Framework}</TestFramework>
            <TestCount>1</TestCount>
        </TestCase>"))}
    </GeneratedTests>
</TestGenerationReport>";
    }
}
