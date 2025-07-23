using Microsoft.Extensions.Logging;
using TestCaseGenerator.Core.Interfaces;
using TestCaseGenerator.Core.Models;
using System.Text.Json;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace TestCaseGenerator.Analyzers.Common;

/// <summary>
/// Processes files and analyzes project structure
/// </summary>
public class FileProcessor : IFileProcessor
{
    private readonly ILogger<FileProcessor> _logger;

    public FileProcessor(ILogger<FileProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<List<string>> DiscoverFilesAsync(
        string projectPath, 
        IEnumerable<string> fileTypes, 
        IEnumerable<string> excludePatterns, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discovering files in project: {ProjectPath}", projectPath);

        var files = new List<string>();
        var allowedExtensions = GetFileExtensions(fileTypes);
        var excludeRegexes = excludePatterns.Select(pattern => new Regex(
            pattern.Replace("*", ".*").Replace("?", "."), 
            RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToList();

        await DiscoverFilesRecursiveAsync(projectPath, allowedExtensions, excludeRegexes, files, cancellationToken);

        _logger.LogInformation("Discovered {FileCount} files matching criteria", files.Count);
        return files;
    }

    public async Task<ProjectStructure> AnalyzeProjectStructureAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing project structure: {ProjectPath}", projectPath);

        var projectStructure = new ProjectStructure
        {
            RootPath = projectPath,
            ProjectName = Path.GetFileName(projectPath)
        };

        try
        {
            // Find project files
            var projectFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(projectPath, "*.vbproj", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(projectPath, "*.fsproj", SearchOption.AllDirectories))
                .ToList();

            if (projectFiles.Any())
            {
                var mainProjectFile = projectFiles.First();
                await AnalyzeProjectFileAsync(mainProjectFile, projectStructure, cancellationToken);
            }

            // Find solution files
            var solutionFiles = Directory.GetFiles(projectPath, "*.sln", SearchOption.TopDirectoryOnly);
            if (solutionFiles.Any())
            {
                await AnalyzeSolutionFileAsync(solutionFiles.First(), projectStructure, cancellationToken);
            }

            // Analyze folder structure
            await AnalyzeFolderStructureAsync(projectPath, projectStructure, cancellationToken);

            // Detect NuGet packages and frameworks
            await AnalyzeNuGetPackagesAsync(projectPath, projectStructure, cancellationToken);

            // Find configuration files
            projectStructure.ConfigurationFiles = FindConfigurationFiles(projectPath);

            _logger.LogInformation("Project structure analysis completed. Found {ProjectType} project with {PackageCount} packages", 
                projectStructure.ProjectType, projectStructure.Packages.Count);

            return projectStructure;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing project structure for {ProjectPath}", projectPath);
            throw;
        }
    }

    public async Task<ExistingTestInfo> AnalyzeExistingTestsAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing existing tests in project: {ProjectPath}", projectPath);

        var existingTestInfo = new ExistingTestInfo();

        try
        {
            // Find test files
            var testFilePatterns = new[] { "*test*.cs", "*tests*.cs", "*.test.cs", "*.tests.cs" };
            var testFiles = new List<string>();

            foreach (var pattern in testFilePatterns)
            {
                var matchingFiles = Directory.GetFiles(projectPath, pattern, SearchOption.AllDirectories);
                testFiles.AddRange(matchingFiles);
            }

            // Remove duplicates
            existingTestInfo.TestFiles = testFiles.Distinct().ToList();

            // Analyze test frameworks and libraries
            await AnalyzeTestFrameworksAsync(projectPath, existingTestInfo, cancellationToken);

            // Analyze test classes and methods
            foreach (var testFile in existingTestInfo.TestFiles)
            {
                try
                {
                    await AnalyzeTestFileAsync(testFile, existingTestInfo, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error analyzing test file: {TestFile}", testFile);
                }
            }

            _logger.LogInformation("Existing tests analysis completed. Found {TestFileCount} test files using {Framework} framework", 
                existingTestInfo.TestFiles.Count, existingTestInfo.TestFramework);

            return existingTestInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing existing tests for {ProjectPath}", projectPath);
            throw;
        }
    }

    /// <summary>
    /// Recursively discovers files matching criteria
    /// </summary>
    private async Task DiscoverFilesRecursiveAsync(
        string directory, 
        HashSet<string> allowedExtensions, 
        List<Regex> excludeRegexes, 
        List<string> files, 
        CancellationToken cancellationToken)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(directory);
            
            // Skip excluded directories
            if (excludeRegexes.Any(regex => regex.IsMatch(directoryInfo.FullName)))
            {
                return;
            }

            // Process files in current directory
            foreach (var file in directoryInfo.GetFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (allowedExtensions.Contains(file.Extension.ToLowerInvariant()))
                {
                    if (!excludeRegexes.Any(regex => regex.IsMatch(file.FullName)))
                    {
                        files.Add(file.FullName);
                    }
                }
            }

            // Process subdirectories
            foreach (var subDirectory in directoryInfo.GetDirectories())
            {
                await DiscoverFilesRecursiveAsync(subDirectory.FullName, allowedExtensions, excludeRegexes, files, cancellationToken);
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Access denied to directory: {Directory}", directory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing directory: {Directory}", directory);
        }
    }

    /// <summary>
    /// Gets file extensions for specified file types
    /// </summary>
    private HashSet<string> GetFileExtensions(IEnumerable<string> fileTypes)
    {
        var extensionMap = new Dictionary<string, string[]>
        {
            { "csharp", new[] { ".cs" } },
            { "typescript", new[] { ".ts", ".tsx" } },
            { "javascript", new[] { ".js", ".jsx" } },
            { "html", new[] { ".html", ".cshtml", ".razor" } },
            { "css", new[] { ".css", ".less", ".scss", ".sass" } }
        };

        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fileType in fileTypes)
        {
            if (extensionMap.TryGetValue(fileType.ToLowerInvariant(), out var typeExtensions))
            {
                foreach (var ext in typeExtensions)
                {
                    extensions.Add(ext);
                }
            }
            else if (fileType.StartsWith("."))
            {
                // Direct extension specification
                extensions.Add(fileType.ToLowerInvariant());
            }
        }

        // Default to C# if no types specified
        if (!extensions.Any())
        {
            extensions.Add(".cs");
        }

        return extensions;
    }

    /// <summary>
    /// Analyzes a project file to extract project information
    /// </summary>
    private async Task AnalyzeProjectFileAsync(string projectFile, ProjectStructure projectStructure, CancellationToken cancellationToken)
    {
        try
        {
            var content = await File.ReadAllTextAsync(projectFile, cancellationToken);
            var projectXml = XDocument.Parse(content);

            // Extract basic project information
            var propertyGroups = projectXml.Descendants("PropertyGroup");
            foreach (var propertyGroup in propertyGroups)
            {
                foreach (var property in propertyGroup.Elements())
                {
                    projectStructure.ProjectProperties[property.Name.LocalName] = property.Value;
                }
            }

            // Extract target framework
            var targetFramework = projectXml.Descendants("TargetFramework").FirstOrDefault()?.Value ??
                                projectXml.Descendants("TargetFrameworks").FirstOrDefault()?.Value;
            if (!string.IsNullOrEmpty(targetFramework))
            {
                projectStructure.TargetFramework = targetFramework;
            }

            // Extract output type
            var outputType = projectXml.Descendants("OutputType").FirstOrDefault()?.Value;
            if (!string.IsNullOrEmpty(outputType))
            {
                projectStructure.ProjectType = outputType;
            }

            // Extract package references
            var packageReferences = projectXml.Descendants("PackageReference");
            foreach (var packageRef in packageReferences)
            {
                var name = packageRef.Attribute("Include")?.Value;
                var version = packageRef.Attribute("Version")?.Value;
                
                if (!string.IsNullOrEmpty(name))
                {
                    var packageReference = new PackageReference
                    {
                        Name = name,
                        Version = version ?? string.Empty,
                        Type = DeterminePackageType(name),
                        IsTestPackage = IsTestPackage(name)
                    };
                    projectStructure.Packages.Add(packageReference);
                }
            }

            // Extract project references
            var projectReferences = projectXml.Descendants("ProjectReference");
            foreach (var projectRef in projectReferences)
            {
                var include = projectRef.Attribute("Include")?.Value;
                if (!string.IsNullOrEmpty(include))
                {
                    var reference = new ProjectReference
                    {
                        Name = Path.GetFileNameWithoutExtension(include),
                        Path = include,
                        ProjectType = "Unknown"
                    };
                    projectStructure.References.Add(reference);
                }
            }

            projectStructure.ProjectName = Path.GetFileNameWithoutExtension(projectFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing project file: {ProjectFile}", projectFile);
        }
    }

    /// <summary>
    /// Analyzes solution file to extract solution-level information
    /// </summary>
    private async Task AnalyzeSolutionFileAsync(string solutionFile, ProjectStructure projectStructure, CancellationToken cancellationToken)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(solutionFile, cancellationToken);
            
            foreach (var line in lines)
            {
                if (line.StartsWith("Project("))
                {
                    // Parse project line: Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ProjectName", "ProjectPath", "{GUID}"
                    var parts = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var projectInfo = parts[1].Trim().Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (projectInfo.Length >= 2)
                        {
                            var name = projectInfo[0].Trim(' ', '"');
                            var path = projectInfo[1].Trim(' ', '"');
                            
                            var reference = new ProjectReference
                            {
                                Name = name,
                                Path = path,
                                ProjectType = DetermineProjectTypeFromPath(path)
                            };
                            projectStructure.References.Add(reference);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing solution file: {SolutionFile}", solutionFile);
        }
    }

    /// <summary>
    /// Analyzes folder structure to categorize source folders
    /// </summary>
    private async Task AnalyzeFolderStructureAsync(string projectPath, ProjectStructure projectStructure, CancellationToken cancellationToken)
    {
        try
        {
            var rootDirectory = new DirectoryInfo(projectPath);
            await AnalyzeDirectoryAsync(rootDirectory, projectStructure.SourceFolders, cancellationToken);

            // Look for test folders
            var testFolders = projectStructure.SourceFolders
                .Where(f => f.Name.Contains("Test", StringComparison.OrdinalIgnoreCase) ||
                           f.Name.Contains("Spec", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var testFolder in testFolders)
            {
                var testFolderInfo = new TestFolder
                {
                    Name = testFolder.Name,
                    Path = testFolder.Path,
                    TestFiles = testFolder.Files.Where(f => f.Contains("test", StringComparison.OrdinalIgnoreCase) ||
                                                           f.Contains("spec", StringComparison.OrdinalIgnoreCase)).ToList(),
                    Pattern = DetermineTestingPattern(testFolder.Name)
                };
                projectStructure.TestFolders.Add(testFolderInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing folder structure for {ProjectPath}", projectPath);
        }
    }

    /// <summary>
    /// Recursively analyzes directory structure
    /// </summary>
    private async Task AnalyzeDirectoryAsync(DirectoryInfo directory, List<SourceFolder> parentFolders, CancellationToken cancellationToken)
    {
        try
        {
            var sourceFolder = new SourceFolder
            {
                Name = directory.Name,
                Path = directory.FullName,
                Type = DetermineFolderType(directory.Name)
            };

            // Get files in this directory
            sourceFolder.Files = directory.GetFiles("*.cs")
                .Select(f => f.FullName)
                .ToList();

            // Process subdirectories
            foreach (var subDir in directory.GetDirectories())
            {
                // Skip common build/cache directories
                if (subDir.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    subDir.Name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                    subDir.Name.Equals("node_modules", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                await AnalyzeDirectoryAsync(subDir, sourceFolder.SubFolders, cancellationToken);
            }

            parentFolders.Add(sourceFolder);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Access denied to directory: {Directory}", directory.FullName);
        }
    }

    /// <summary>
    /// Analyzes NuGet packages to detect frameworks and libraries
    /// </summary>
    private async Task AnalyzeNuGetPackagesAsync(string projectPath, ProjectStructure projectStructure, CancellationToken cancellationToken)
    {
        try
        {
            // Analyze test frameworks
            foreach (var package in projectStructure.Packages)
            {
                var testFramework = DetermineTestFramework(package.Name);
                if (testFramework != TestFrameworkType.Unknown)
                {
                    var frameworkInfo = new TestFrameworkInfo
                    {
                        Name = package.Name,
                        Version = package.Version,
                        Type = testFramework
                    };
                    
                    // Add known test attributes and methods based on framework
                    PopulateTestFrameworkInfo(frameworkInfo);
                    projectStructure.NuGetInfo.TestFrameworks.Add(frameworkInfo);
                }

                var mockingFramework = DetermineMockingFramework(package.Name);
                if (mockingFramework != MockingFrameworkType.Unknown)
                {
                    var mockingInfo = new MockingFrameworkInfo
                    {
                        Name = package.Name,
                        Version = package.Version,
                        Type = mockingFramework
                    };
                    
                    PopulateMockingFrameworkInfo(mockingInfo);
                    projectStructure.NuGetInfo.MockingFrameworks.Add(mockingInfo);
                }

                var assertionLibrary = DetermineAssertionLibrary(package.Name);
                if (assertionLibrary != AssertionLibraryType.Unknown)
                {
                    var assertionInfo = new AssertionLibraryInfo
                    {
                        Name = package.Name,
                        Version = package.Version,
                        Type = assertionLibrary
                    };
                    
                    PopulateAssertionLibraryInfo(assertionInfo);
                    projectStructure.NuGetInfo.AssertionLibraries.Add(assertionInfo);
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing NuGet packages for {ProjectPath}", projectPath);
        }
    }

    /// <summary>
    /// Analyzes test frameworks in the project
    /// </summary>
    private async Task AnalyzeTestFrameworksAsync(string projectPath, ExistingTestInfo existingTestInfo, CancellationToken cancellationToken)
    {
        // Check for common test framework packages
        var projectFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
        
        foreach (var projectFile in projectFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(projectFile, cancellationToken);
                
                if (content.Contains("xunit", StringComparison.OrdinalIgnoreCase))
                {
                    existingTestInfo.TestFramework = "xunit";
                    existingTestInfo.TestLibraries.Add("xUnit");
                }
                else if (content.Contains("nunit", StringComparison.OrdinalIgnoreCase))
                {
                    existingTestInfo.TestFramework = "nunit";
                    existingTestInfo.TestLibraries.Add("NUnit");
                }
                else if (content.Contains("mstest", StringComparison.OrdinalIgnoreCase))
                {
                    existingTestInfo.TestFramework = "mstest";
                    existingTestInfo.TestLibraries.Add("MSTest");
                }

                if (content.Contains("moq", StringComparison.OrdinalIgnoreCase))
                {
                    existingTestInfo.TestLibraries.Add("Moq");
                }
                if (content.Contains("fluentassertions", StringComparison.OrdinalIgnoreCase))
                {
                    existingTestInfo.TestLibraries.Add("FluentAssertions");
                }
                if (content.Contains("nsubstitute", StringComparison.OrdinalIgnoreCase))
                {
                    existingTestInfo.TestLibraries.Add("NSubstitute");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error analyzing project file for test frameworks: {ProjectFile}", projectFile);
            }
        }
    }

    /// <summary>
    /// Analyzes an individual test file
    /// </summary>
    private async Task AnalyzeTestFileAsync(string testFile, ExistingTestInfo existingTestInfo, CancellationToken cancellationToken)
    {
        try
        {
            var content = await File.ReadAllTextAsync(testFile, cancellationToken);
            
            // Extract test class information using simple regex patterns
            var classMatches = Regex.Matches(content, @"public\s+class\s+(\w+Tests?\w*)", RegexOptions.IgnoreCase);
            
            foreach (Match match in classMatches)
            {
                var testClassName = match.Groups[1].Value;
                var testClassInfo = new TestClassInfo
                {
                    Name = testClassName,
                    Framework = existingTestInfo.TestFramework
                };

                // Try to determine what class is being tested
                var testedClassName = testClassName.Replace("Tests", "").Replace("Test", "");
                testClassInfo.TestedClass = testedClassName;

                // Extract test methods
                var methodPatterns = new[]
                {
                    @"\[Test\]\s*public\s+\w+\s+(\w+)",  // NUnit
                    @"\[Fact\]\s*public\s+\w+\s+(\w+)",  // xUnit
                    @"\[TestMethod\]\s*public\s+\w+\s+(\w+)"  // MSTest
                };

                foreach (var pattern in methodPatterns)
                {
                    var methodMatches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
                    foreach (Match methodMatch in methodMatches)
                    {
                        testClassInfo.TestMethods.Add(methodMatch.Groups[1].Value);
                    }
                }

                // Extract mocked dependencies
                var mockMatches = Regex.Matches(content, @"Mock<(\w+)>", RegexOptions.IgnoreCase);
                foreach (Match mockMatch in mockMatches)
                {
                    testClassInfo.MockedDependencies.Add(mockMatch.Groups[1].Value);
                }

                existingTestInfo.TestClasses.Add(testClassInfo);

                // Add to existing test methods mapping
                if (!existingTestInfo.ExistingTestMethods.ContainsKey(testedClassName))
                {
                    existingTestInfo.ExistingTestMethods[testedClassName] = new List<string>();
                }
                existingTestInfo.ExistingTestMethods[testedClassName].AddRange(testClassInfo.TestMethods);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing test file: {TestFile}", testFile);
        }
    }

    /// <summary>
    /// Finds configuration files in the project
    /// </summary>
    private List<string> FindConfigurationFiles(string projectPath)
    {
        var configFiles = new List<string>();
        var configPatterns = new[] { "*.config", "appsettings*.json", "*.settings", "web.config", "app.config" };

        foreach (var pattern in configPatterns)
        {
            try
            {
                var files = Directory.GetFiles(projectPath, pattern, SearchOption.AllDirectories);
                configFiles.AddRange(files);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error searching for config files with pattern: {Pattern}", pattern);
            }
        }

        return configFiles.Distinct().ToList();
    }

    // Helper methods for determining types and patterns

    private PackageType DeterminePackageType(string packageName) => packageName.ToLowerInvariant() switch
    {
        var name when name.Contains("test") => PackageType.TestFramework,
        var name when name.Contains("moq") || name.Contains("nsubstitute") || name.Contains("fakeiteasy") => PackageType.MockingFramework,
        var name when name.Contains("fluentassertions") || name.Contains("shouldly") => PackageType.AssertionLibrary,
        var name when name.Contains("xunit") || name.Contains("nunit") || name.Contains("mstest") => PackageType.TestFramework,
        _ => PackageType.ProductionDependency
    };

    private bool IsTestPackage(string packageName) => 
        packageName.Contains("test", StringComparison.OrdinalIgnoreCase) ||
        packageName.Contains("moq", StringComparison.OrdinalIgnoreCase) ||
        packageName.Contains("nsubstitute", StringComparison.OrdinalIgnoreCase) ||
        packageName.Contains("fluentassertions", StringComparison.OrdinalIgnoreCase);

    private string DetermineProjectTypeFromPath(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".csproj" => "C# Project",
        ".vbproj" => "VB.NET Project",
        ".fsproj" => "F# Project",
        _ => "Unknown"
    };

    private FolderType DetermineFolderType(string folderName) => folderName.ToLowerInvariant() switch
    {
        "controllers" => FolderType.Controllers,
        "services" => FolderType.Services,
        "models" => FolderType.Models,
        "repositories" => FolderType.Repositories,
        "viewmodels" => FolderType.ViewModels,
        "helpers" => FolderType.Helpers,
        "extensions" => FolderType.Extensions,
        "configuration" or "config" => FolderType.Configuration,
        "data" => FolderType.Data,
        "business" => FolderType.Business,
        "web" => FolderType.Web,
        "api" => FolderType.Api,
        _ => FolderType.Other
    };

    private TestingPattern DetermineTestingPattern(string folderName) => folderName.ToLowerInvariant() switch
    {
        var name when name.Contains("unit") => TestingPattern.UnitTests,
        var name when name.Contains("integration") => TestingPattern.IntegrationTests,
        var name when name.Contains("functional") => TestingPattern.FunctionalTests,
        var name when name.Contains("acceptance") => TestingPattern.AcceptanceTests,
        var name when name.Contains("performance") => TestingPattern.PerformanceTests,
        _ => TestingPattern.UnitTests
    };

    private TestFrameworkType DetermineTestFramework(string packageName) => packageName.ToLowerInvariant() switch
    {
        var name when name.Contains("xunit") => TestFrameworkType.XUnit,
        var name when name.Contains("nunit") => TestFrameworkType.NUnit,
        var name when name.Contains("mstest") => TestFrameworkType.MSTest,
        _ => TestFrameworkType.Unknown
    };

    private MockingFrameworkType DetermineMockingFramework(string packageName) => packageName.ToLowerInvariant() switch
    {
        var name when name.Contains("moq") => MockingFrameworkType.Moq,
        var name when name.Contains("nsubstitute") => MockingFrameworkType.NSubstitute,
        var name when name.Contains("fakeiteasy") => MockingFrameworkType.FakeItEasy,
        _ => MockingFrameworkType.Unknown
    };

    private AssertionLibraryType DetermineAssertionLibrary(string packageName) => packageName.ToLowerInvariant() switch
    {
        var name when name.Contains("fluentassertions") => AssertionLibraryType.FluentAssertions,
        var name when name.Contains("shouldly") => AssertionLibraryType.Shouldly,
        var name when name.Contains("nunit") => AssertionLibraryType.NUnit,
        var name when name.Contains("xunit") => AssertionLibraryType.XUnit,
        var name when name.Contains("mstest") => AssertionLibraryType.MSTest,
        _ => AssertionLibraryType.Unknown
    };

    private void PopulateTestFrameworkInfo(TestFrameworkInfo frameworkInfo)
    {
        switch (frameworkInfo.Type)
        {
            case TestFrameworkType.XUnit:
                frameworkInfo.TestAttributes.AddRange(new[] { "Fact", "Theory", "InlineData" });
                frameworkInfo.TestMethods.AddRange(new[] { "Assert.Equal", "Assert.True", "Assert.False", "Assert.Null" });
                break;
            case TestFrameworkType.NUnit:
                frameworkInfo.TestAttributes.AddRange(new[] { "Test", "TestCase", "TestCaseSource" });
                frameworkInfo.TestMethods.AddRange(new[] { "Assert.AreEqual", "Assert.IsTrue", "Assert.IsFalse", "Assert.IsNull" });
                break;
            case TestFrameworkType.MSTest:
                frameworkInfo.TestAttributes.AddRange(new[] { "TestMethod", "DataRow", "DataTestMethod" });
                frameworkInfo.TestMethods.AddRange(new[] { "Assert.AreEqual", "Assert.IsTrue", "Assert.IsFalse", "Assert.IsNull" });
                break;
        }
    }

    private void PopulateMockingFrameworkInfo(MockingFrameworkInfo mockingInfo)
    {
        switch (mockingInfo.Type)
        {
            case MockingFrameworkType.Moq:
                mockingInfo.MockMethods.AddRange(new[] { "Setup", "Returns", "Verify", "Callback" });
                break;
            case MockingFrameworkType.NSubstitute:
                mockingInfo.MockMethods.AddRange(new[] { "Returns", "Received", "DidNotReceive", "When" });
                break;
            case MockingFrameworkType.FakeItEasy:
                mockingInfo.MockMethods.AddRange(new[] { "CallTo", "Returns", "MustHaveHappened" });
                break;
        }
    }

    private void PopulateAssertionLibraryInfo(AssertionLibraryInfo assertionInfo)
    {
        switch (assertionInfo.Type)
        {
            case AssertionLibraryType.FluentAssertions:
                assertionInfo.AssertionMethods.AddRange(new[] { "Should().Be", "Should().BeNull", "Should().BeOfType", "Should().Contain" });
                break;
            case AssertionLibraryType.Shouldly:
                assertionInfo.AssertionMethods.AddRange(new[] { "ShouldBe", "ShouldBeNull", "ShouldBeOfType", "ShouldContain" });
                break;
        }
    }
}
