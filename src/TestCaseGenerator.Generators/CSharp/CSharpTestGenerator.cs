using Humanizer;
using Microsoft.Extensions.Logging;
using TestCaseGenerator.Core.Interfaces;
using TestCaseGenerator.Core.Models;
using System.Text;

namespace TestCaseGenerator.Generators.CSharp;

/// <summary>
/// Generates C# test cases using xUnit, NUnit, or MSTest frameworks
/// </summary>
public class CSharpTestGenerator : ITestGenerator
{
    private readonly ILogger<CSharpTestGenerator> _logger;
    private readonly ITemplateEngine _templateEngine;

    public CSharpTestGenerator(ILogger<CSharpTestGenerator> logger, ITemplateEngine templateEngine)
    {
        _logger = logger;
        _templateEngine = templateEngine;
    }

    public string Name => "C# Test Generator";
    public string TestFramework { get; private set; } = "xunit";

    public bool CanGenerate(AnalysisResult analysisResult)
    {
        return analysisResult.FileType.Equals("csharp", StringComparison.OrdinalIgnoreCase) &&
               analysisResult.Classes.Any();
    }

    public async Task<List<TestCase>> GenerateTestsAsync(AnalysisResult analysisResult, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating test cases for C# file: {FilePath}", analysisResult.FilePath);

        var testCases = new List<TestCase>();

        // Determine test framework from existing tests or project structure
        TestFramework = DetermineTestFramework(analysisResult);

        foreach (var classInfo in analysisResult.Classes)
        {
            // Skip if this is already a test class
            if (IsTestClass(classInfo))
            {
                continue;
            }

            // Generate tests for each method
            foreach (var method in classInfo.Methods)
            {
                if (ShouldGenerateTestForMethod(method, analysisResult))
                {
                    var methodTestCases = await GenerateMethodTestCasesAsync(classInfo, method, analysisResult, cancellationToken);
                    testCases.AddRange(methodTestCases);
                }
            }

            // Generate constructor tests
            foreach (var constructor in classInfo.Constructors)
            {
                if (ShouldGenerateTestForConstructor(constructor))
                {
                    var constructorTestCases = await GenerateConstructorTestCasesAsync(classInfo, constructor, analysisResult, cancellationToken);
                    testCases.AddRange(constructorTestCases);
                }
            }

            // Generate property tests
            foreach (var property in classInfo.Properties)
            {
                if (ShouldGenerateTestForProperty(property))
                {
                    var propertyTestCases = await GeneratePropertyTestCasesAsync(classInfo, property, analysisResult, cancellationToken);
                    testCases.AddRange(propertyTestCases);
                }
            }
        }

        _logger.LogDebug("Generated {TestCaseCount} test cases for {FilePath}", testCases.Count, analysisResult.FilePath);
        return testCases;
    }

    /// <summary>
    /// Creates a test file path that preserves the source project structure
    /// </summary>
    private string CreateTestFilePathWithStructure(string sourceFilePath, string outputPath, string testFileName)
    {
        try
        {
            // Find the project root by looking for common project indicators
            var projectRoot = FindProjectRoot(sourceFilePath);
            
            if (string.IsNullOrEmpty(projectRoot))
            {
                // Fallback to simple output if we can't determine structure
                return Path.Combine(outputPath, testFileName);
            }

            // Get the relative path from project root
            var relativePath = Path.GetRelativePath(projectRoot, Path.GetDirectoryName(sourceFilePath)!);
            
            // Create the test file path preserving the folder structure
            var testDirectoryPath = Path.Combine(outputPath, relativePath);
            return Path.Combine(testDirectoryPath, testFileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preserve folder structure for {SourceFile}, using flat structure", sourceFilePath);
            return Path.Combine(outputPath, testFileName);
        }
    }

    /// <summary>
    /// Finds the project root directory by looking for project files
    /// </summary>
    private string FindProjectRoot(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        
        while (!string.IsNullOrEmpty(directory))
        {
            // Look for project indicators
            var projectFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(directory, "*.sln", SearchOption.TopDirectoryOnly))
                .Concat(Directory.GetFiles(directory, "*.vbproj", SearchOption.TopDirectoryOnly))
                .Concat(Directory.GetFiles(directory, "*.fsproj", SearchOption.TopDirectoryOnly));

            if (projectFiles.Any())
            {
                return directory;
            }

            // Move up one directory level
            var parentDirectory = Path.GetDirectoryName(directory);
            if (parentDirectory == directory) // Reached root
                break;
                
            directory = parentDirectory;
        }

        return string.Empty;
    }

    /// <summary>
    /// Cleans up old test folder to prevent duplicates when re-running
    /// </summary>
    private async Task CleanupOldTestFolderAsync(string outputPath)
    {
        try
        {
            if (Directory.Exists(outputPath))
            {
                _logger.LogInformation("Cleaning up old test folder: {OutputPath}", outputPath);
                
                // Delete all files and subdirectories
                var directory = new DirectoryInfo(outputPath);
                
                foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
                {
                    file.Delete();
                }
                
                foreach (var dir in directory.GetDirectories())
                {
                    dir.Delete(recursive: true);
                }
                
                _logger.LogInformation("Successfully cleaned up old test folder");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up old test folder: {OutputPath}", outputPath);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Generates test files based on analysis results with improved folder structure and cleanup
    /// </summary>
    public async Task<List<GeneratedTestFile>> GenerateTestFilesAsync(
        IEnumerable<AnalysisResult> analysisResults, 
        string outputPath, 
        CancellationToken cancellationToken = default)
    {
        var generatedFiles = new List<GeneratedTestFile>();
        var analysisResultsList = analysisResults.ToList();

        // Clean up old test folder if it exists (prevent duplicates) - only once at the start
        if (analysisResultsList.Any())
        {
            await CleanupOldTestFolderAsync(outputPath);
        }

        foreach (var analysisResult in analysisResultsList)
        {
            if (!CanGenerate(analysisResult))
            {
                continue;
            }

            try
            {
                var testCases = await GenerateTestsAsync(analysisResult, cancellationToken);
                if (!testCases.Any())
                {
                    continue;
                }

                var testFile = await GenerateTestFileAsync(analysisResult, testCases, outputPath, cancellationToken);
                generatedFiles.Add(testFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test file for {FilePath}", analysisResult.FilePath);
            }
        }

        return generatedFiles;
    }

    /// <summary>
    /// Generates a complete test file for the given analysis result
    /// </summary>
    private async Task<GeneratedTestFile> GenerateTestFileAsync(
        AnalysisResult analysisResult, 
        List<TestCase> testCases, 
        string outputPath, 
        CancellationToken cancellationToken)
    {
        var sourceFileName = Path.GetFileNameWithoutExtension(analysisResult.FilePath);
        var testFileName = $"{sourceFileName}Tests.cs";
        
        // Preserve source project folder structure in test output
        var testFilePath = CreateTestFilePathWithStructure(analysisResult.FilePath, outputPath, testFileName);

        // Group test cases by class
        var testCasesByClass = testCases.GroupBy(tc => tc.TargetClass).ToList();

        var testFileContent = new StringBuilder();
        
        foreach (var classGroup in testCasesByClass)
        {
            var classInfo = analysisResult.Classes.First(c => c.Name == classGroup.Key);
            var classTestContent = await GenerateTestClassAsync(classInfo, classGroup.ToList(), analysisResult, cancellationToken);
            testFileContent.AppendLine(classTestContent);
        }

        var generatedFile = new GeneratedTestFile
        {
            FilePath = testFilePath,
            SourceFilePath = analysisResult.FilePath,
            Content = testFileContent.ToString(),
            Framework = TestFramework,
            TestCases = testCases,
            IsNewFile = !File.Exists(testFilePath),
            RequiresCompilation = true
        };

        // Ensure the directory structure exists for the test file
        var testDirectory = Path.GetDirectoryName(testFilePath);
        if (!string.IsNullOrEmpty(testDirectory))
        {
            Directory.CreateDirectory(testDirectory);
        }

        // Write the test file
        await File.WriteAllTextAsync(testFilePath, generatedFile.Content, cancellationToken);

        _logger.LogInformation("Generated test file: {TestFilePath} with {TestCount} test cases", 
            testFilePath, testCases.Count);

        return generatedFile;
    }

    /// <summary>
    /// Generates a test class using templates
    /// </summary>
    private async Task<string> GenerateTestClassAsync(
        ClassInfo classInfo, 
        List<TestCase> testCases, 
        AnalysisResult analysisResult, 
        CancellationToken cancellationToken)
    {
        var templateData = CreateTemplateData(classInfo, testCases, analysisResult);
        
        var templateName = DetermineTemplateName(classInfo);
        var content = await _templateEngine.RenderTemplateAsync(templateName, templateData);
        
        return content;
    }

    /// <summary>
    /// Creates template data for rendering test classes
    /// </summary>
    private object CreateTemplateData(ClassInfo classInfo, List<TestCase> testCases, AnalysisResult analysisResult)
    {
        var dependencies = analysisResult.Dependencies
            .Where(d => d.RequiresMock)
            .Select(d => new
            {
                type = d.InterfaceType ?? d.Type,
                name = d.Name.Camelize(),
                hasNext = false // Will be set correctly when processing the list
            })
            .ToList();

        // Set hasNext flags
        for (int i = 0; i < dependencies.Count - 1; i++)
        {
            var dep = dependencies[i];
            dependencies[i] = new
            {
                type = dep.type,
                name = dep.name,
                hasNext = true
            };
        }

        var methods = testCases
            .Where(tc => tc.Type != TestType.Constructor && tc.Type != TestType.Property)
            .GroupBy(tc => tc.TargetMethod)
            .Select(g => new
            {
                name = g.Key,
                testCases = g.Select(tc => new
                {
                    name = tc.TestName,
                    scenario = tc.Scenario,
                    expectedResult = tc.ExpectedResult,
                    methodName = tc.TargetMethod,
                    isAsync = tc.IsAsync,
                    arrangeCode = tc.ArrangeCode,
                    mockSetup = tc.MockSetup,
                    assertions = tc.Assertions,
                    mockVerifications = tc.MockVerifications,
                    parameters = ExtractParametersFromTestCase(tc)
                })
            });

        var properties = testCases
            .Where(tc => tc.Type == TestType.Property)
            .Select(tc => new
            {
                name = tc.TargetMethod, // For properties, TargetMethod contains property name
                testValue = GenerateTestValue(GetPropertyType(classInfo, tc.TargetMethod))
            });

        var constructors = testCases
            .Where(tc => tc.Type == TestType.Constructor)
            .Select(tc => new
            {
                scenario = tc.Scenario,
                expectedResult = tc.ExpectedResult,
                arrangeCode = tc.ArrangeCode,
                expectsException = tc.ExpectedResult.Contains("Exception"),
                exceptionType = ExtractExceptionType(tc.ExpectedResult),
                parameters = ExtractParametersFromTestCase(tc),
                assertions = tc.Assertions
            });

        return new
        {
            className = classInfo.Name,
            @namespace = analysisResult.Namespace,
            usings = GetRequiredUsings(analysisResult, TestFramework),
            generatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            dependencies = dependencies,
            methods = methods,
            hasProperties = properties.Any(),
            properties = properties,
            hasConstructors = constructors.Any(),
            constructors = constructors
        };
    }

    /// <summary>
    /// Generates test cases for a method
    /// </summary>
    private async Task<List<TestCase>> GenerateMethodTestCasesAsync(
        ClassInfo classInfo, 
        Core.Models.MethodInfo method, 
        AnalysisResult analysisResult, 
        CancellationToken cancellationToken)
    {
        var testCases = new List<TestCase>();

        // Generate happy path test
        var happyPathTest = CreateMethodTestCase(classInfo, method, "WithValidInput", "ReturnsExpectedResult", analysisResult);
        testCases.Add(happyPathTest);

        // Generate null parameter tests for reference types
        foreach (var param in method.Parameters.Where(p => IsReferenceType(p.Type) && !p.IsOptional))
        {
            var nullTest = CreateMethodTestCase(
                classInfo, 
                method, 
                $"WithNull{param.Name.Pascalize()}", 
                "ThrowsArgumentNullException", 
                analysisResult,
                TestType.Exception);
            testCases.Add(nullTest);
        }

        // Generate edge case tests based on parameters
        foreach (var param in method.Parameters)
        {
            if (IsNumericType(param.Type))
            {
                var edgeTest = CreateMethodTestCase(
                    classInfo, 
                    method, 
                    $"WithZero{param.Name.Pascalize()}", 
                    "HandlesEdgeCase", 
                    analysisResult);
                testCases.Add(edgeTest);
            }
            else if (IsStringType(param.Type))
            {
                var emptyStringTest = CreateMethodTestCase(
                    classInfo, 
                    method, 
                    $"WithEmpty{param.Name.Pascalize()}", 
                    "HandlesEmptyString", 
                    analysisResult);
                testCases.Add(emptyStringTest);
            }
        }

        // Generate async-specific tests
        if (method.IsAsync)
        {
            var asyncTest = CreateMethodTestCase(
                classInfo, 
                method, 
                "WhenOperationCompletesSuccessfully", 
                "ReturnsCompletedTask", 
                analysisResult,
                TestType.AsyncMethod);
            testCases.Add(asyncTest);
        }

        await Task.Delay(1, cancellationToken); // Yield control
        return testCases;
    }

    /// <summary>
    /// Generates test cases for a constructor
    /// </summary>
    private async Task<List<TestCase>> GenerateConstructorTestCasesAsync(
        ClassInfo classInfo, 
        Core.Models.ConstructorInfo constructor, 
        AnalysisResult analysisResult, 
        CancellationToken cancellationToken)
    {
        var testCases = new List<TestCase>();

        // Generate valid constructor test
        var validTest = CreateConstructorTestCase(classInfo, constructor, "WithValidParameters", "CreatesInstance", analysisResult);
        testCases.Add(validTest);

        // Generate null parameter tests
        foreach (var param in constructor.Parameters.Where(p => IsReferenceType(p.Type) && !p.IsOptional))
        {
            var nullTest = CreateConstructorTestCase(
                classInfo, 
                constructor, 
                $"WithNull{param.Name.Pascalize()}", 
                "ThrowsArgumentNullException", 
                analysisResult);
            testCases.Add(nullTest);
        }

        await Task.Delay(1, cancellationToken); // Yield control
        return testCases;
    }

    /// <summary>
    /// Generates test cases for a property
    /// </summary>
    private async Task<List<TestCase>> GeneratePropertyTestCasesAsync(
        ClassInfo classInfo, 
        Core.Models.PropertyInfo property, 
        AnalysisResult analysisResult, 
        CancellationToken cancellationToken)
    {
        var testCases = new List<TestCase>();

        if (property.HasGetter && property.HasSetter)
        {
            var getSetTest = CreatePropertyTestCase(classInfo, property, "GetAndSet", "WorkCorrectly", analysisResult);
            testCases.Add(getSetTest);
        }

        await Task.Delay(1, cancellationToken); // Yield control
        return testCases;
    }

    /// <summary>
    /// Creates a test case for a method
    /// </summary>
    private TestCase CreateMethodTestCase(
        ClassInfo classInfo, 
        Core.Models.MethodInfo method, 
        string scenario, 
        string expectedResult, 
        AnalysisResult analysisResult,
        TestType testType = TestType.Unit)
    {
        var testCase = new TestCase
        {
            TestName = $"{method.Name}_{scenario}_{expectedResult}",
            TargetMethod = method.Name,
            TargetClass = classInfo.Name,
            Scenario = scenario,
            ExpectedResult = expectedResult,
            Type = testType,
            Framework = TestFramework,
            IsAsync = method.IsAsync,
            Priority = 1
        };

        // Generate arrange code
        GenerateArrangeCode(testCase, method, classInfo, analysisResult);

        // Generate mock setup
        GenerateMockSetup(testCase, method, analysisResult);

        // Generate assertions
        GenerateAssertions(testCase, method, testType);

        // Generate mock verifications
        GenerateMockVerifications(testCase, method, analysisResult);

        return testCase;
    }

    /// <summary>
    /// Creates a test case for a constructor
    /// </summary>
    private TestCase CreateConstructorTestCase(
        ClassInfo classInfo, 
        Core.Models.ConstructorInfo constructor, 
        string scenario, 
        string expectedResult, 
        AnalysisResult analysisResult)
    {
        var testCase = new TestCase
        {
            TestName = $"Constructor_{scenario}_{expectedResult}",
            TargetMethod = "Constructor",
            TargetClass = classInfo.Name,
            Scenario = scenario,
            ExpectedResult = expectedResult,
            Type = TestType.Constructor,
            Framework = TestFramework,
            IsAsync = false,
            Priority = 1
        };

        // Generate constructor-specific code
        GenerateConstructorArrangeCode(testCase, constructor, classInfo, analysisResult);
        GenerateConstructorAssertions(testCase, constructor, expectedResult);

        return testCase;
    }

    /// <summary>
    /// Creates a test case for a property
    /// </summary>
    private TestCase CreatePropertyTestCase(
        ClassInfo classInfo, 
        Core.Models.PropertyInfo property, 
        string scenario, 
        string expectedResult, 
        AnalysisResult analysisResult)
    {
        var testCase = new TestCase
        {
            TestName = $"{property.Name}_{scenario}_{expectedResult}",
            TargetMethod = property.Name,
            TargetClass = classInfo.Name,
            Scenario = scenario,
            ExpectedResult = expectedResult,
            Type = TestType.Property,
            Framework = TestFramework,
            IsAsync = false,
            Priority = 2
        };

        // Generate property-specific code
        GeneratePropertyArrangeCode(testCase, property, classInfo);
        GeneratePropertyAssertions(testCase, property);

        return testCase;
    }

    // Helper methods for code generation
    private void GenerateArrangeCode(TestCase testCase, Core.Models.MethodInfo method, ClassInfo classInfo, AnalysisResult analysisResult)
    {
        foreach (var param in method.Parameters)
        {
            var testValue = GenerateTestValue(param.Type);
            testCase.ArrangeCode.Add($"var {param.Name} = {testValue};");
        }
    }

    private void GenerateMockSetup(TestCase testCase, Core.Models.MethodInfo method, AnalysisResult analysisResult)
    {
        foreach (var dependency in analysisResult.Dependencies.Where(d => d.RequiresMock))
        {
            var mockName = $"_{dependency.Name.Camelize()}Mock";
            
            // Find the dependency interface and its methods
            var dependencyInterface = analysisResult.Classes.FirstOrDefault(c => 
                c.Name == dependency.Type || c.FullName == dependency.Type);
            
            if (dependencyInterface?.Methods.Any() == true)
            {
                // Use the first method as an example, or find a relevant method
                var targetMethod = dependencyInterface.Methods.FirstOrDefault(m => 
                    !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"));
                
                if (targetMethod != null)
                {
                    var parameters = string.Join(", ", targetMethod.Parameters.Select(p => GetMockParameterValue(p.Type)));
                    var setupCall = $"{mockName}.Setup(x => x.{targetMethod.Name}({parameters}))";
                    
                    if (targetMethod.ReturnType != "void" && targetMethod.ReturnType != null)
                    {
                        setupCall += ".Returns(expectedValue)";
                    }
                    
                    testCase.MockSetup.Add(setupCall + ";");
                }
                else
                {
                    // Fallback for properties or when no suitable method found
                    testCase.MockSetup.Add($"{mockName}.Setup(x => x.{dependencyInterface.Methods.First().Name}).Returns(expectedValue);");
                }
            }
            else
            {
                // Fallback when interface is not found
                testCase.MockSetup.Add($"{mockName}.Setup(x => x.GetAsync(It.IsAny<int>())).Returns(expectedValue);");
            }
        }
    }

    private string GetMockParameterValue(string parameterType)
    {
        return parameterType switch
        {
            "int" or "int?" => "It.IsAny<int>()",
            "string" => "It.IsAny<string>()",
            "bool" or "bool?" => "It.IsAny<bool>()",
            "decimal" or "decimal?" => "It.IsAny<decimal>()",
            "double" or "double?" => "It.IsAny<double>()",
            "float" or "float?" => "It.IsAny<float>()",
            "long" or "long?" => "It.IsAny<long>()",
            "DateTime" or "DateTime?" => "It.IsAny<DateTime>()",
            "Guid" or "Guid?" => "It.IsAny<Guid>()",
            _ when parameterType.EndsWith("[]") => $"It.IsAny<{parameterType}>()",
            _ when parameterType.StartsWith("List<") => $"It.IsAny<{parameterType}>()",
            _ when parameterType.StartsWith("IEnumerable<") => $"It.IsAny<{parameterType}>()",
            _ => $"It.IsAny<{parameterType}>()"
        };
    }

    private void GenerateAssertions(TestCase testCase, Core.Models.MethodInfo method, TestType testType)
    {
        if (testType == TestType.Exception)
        {
            testCase.Assertions.Add("result.Should().Throw<ArgumentNullException>();");
        }
        else if (method.ReturnType != "void")
        {
            if (TestFramework == "xunit")
            {
                testCase.Assertions.Add("result.Should().NotBeNull();");
            }
            else
            {
                testCase.Assertions.Add("Assert.IsNotNull(result);");
            }
        }
    }

    private void GenerateMockVerifications(TestCase testCase, Core.Models.MethodInfo method, AnalysisResult analysisResult)
    {
        foreach (var dependency in analysisResult.Dependencies.Where(d => d.RequiresMock))
        {
            var mockName = $"_{dependency.Name.Camelize()}Mock";
            
            // Find the dependency interface and its methods
            var dependencyInterface = analysisResult.Classes.FirstOrDefault(c => 
                c.Name == dependency.Type || c.FullName == dependency.Type);
            
            if (dependencyInterface?.Methods.Any() == true)
            {
                // Use the first method as an example, or find a relevant method
                var targetMethod = dependencyInterface.Methods.FirstOrDefault(m => 
                    !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"));
                
                if (targetMethod != null)
                {
                    var parameters = string.Join(", ", targetMethod.Parameters.Select(p => GetMockParameterValue(p.Type)));
                    testCase.MockVerifications.Add($"{mockName}.Verify(x => x.{targetMethod.Name}({parameters}), Times.Once);");
                }
                else
                {
                    // Fallback for properties or when no suitable method found
                    testCase.MockVerifications.Add($"{mockName}.Verify(x => x.{dependencyInterface.Methods.First().Name}, Times.Once);");
                }
            }
            else
            {
                // Fallback when interface is not found
                testCase.MockVerifications.Add($"{mockName}.Verify(x => x.GetAsync(It.IsAny<int>()), Times.Once);");
            }
        }
    }

    private void GenerateConstructorArrangeCode(TestCase testCase, Core.Models.ConstructorInfo constructor, ClassInfo classInfo, AnalysisResult analysisResult)
    {
        foreach (var param in constructor.Parameters)
        {
            if (testCase.Scenario.Contains($"Null{param.Name.Pascalize()}"))
            {
                testCase.ArrangeCode.Add($"var {param.Name} = null;");
            }
            else
            {
                var testValue = GenerateTestValue(param.Type);
                testCase.ArrangeCode.Add($"var {param.Name} = {testValue};");
            }
        }
    }

    private void GenerateConstructorAssertions(TestCase testCase, Core.Models.ConstructorInfo constructor, string expectedResult)
    {
        if (expectedResult.Contains("Exception"))
        {
            testCase.Assertions.Add("act.Should().Throw<ArgumentNullException>();");
        }
        else
        {
            testCase.Assertions.Add("result.Should().NotBeNull();");
        }
    }

    private void GeneratePropertyArrangeCode(TestCase testCase, Core.Models.PropertyInfo property, ClassInfo classInfo)
    {
        var testValue = GenerateTestValue(property.Type);
        testCase.ArrangeCode.Add($"var expected{property.Name} = {testValue};");
    }

    private void GeneratePropertyAssertions(TestCase testCase, Core.Models.PropertyInfo property)
    {
        testCase.Assertions.Add($"actual{property.Name}.Should().Be(expected{property.Name});");
    }

    // Helper methods for type checking and value generation
    private string GenerateTestValue(string type) => type.ToLowerInvariant() switch
    {
        "string" => "\"test\"",
        "int" or "int32" => "42",
        "long" or "int64" => "42L",
        "bool" or "boolean" => "true",
        "decimal" => "42.0m",
        "double" => "42.0",
        "float" => "42.0f",
        "guid" => "Guid.NewGuid()",
        "datetime" => "new DateTime(2023, 1, 1)",
        _ when type.EndsWith("[]") => $"new {type.Replace("[]", "[0]")}",
        _ when type.StartsWith("List<") => $"new {type}()",
        _ when type.StartsWith("I") && char.IsUpper(type[1]) => "Mock.Of<" + type + ">()",
        _ => $"new {type}()"
    };

    private bool IsReferenceType(string type) => 
        type == "string" || 
        type.EndsWith("[]") || 
        type.StartsWith("List<") || 
        type.StartsWith("I") ||
        !IsValueType(type);

    private bool IsValueType(string type) => type.ToLowerInvariant() switch
    {
        "int" or "int32" or "long" or "int64" or "bool" or "boolean" or 
        "decimal" or "double" or "float" or "byte" or "sbyte" or 
        "short" or "ushort" or "uint" or "ulong" or "char" => true,
        _ => false
    };

    private bool IsNumericType(string type) => type.ToLowerInvariant() switch
    {
        "int" or "int32" or "long" or "int64" or "decimal" or 
        "double" or "float" or "byte" or "sbyte" or "short" or 
        "ushort" or "uint" or "ulong" => true,
        _ => false
    };

    private bool IsStringType(string type) => 
        type.Equals("string", StringComparison.OrdinalIgnoreCase);

    private bool IsTestClass(ClassInfo classInfo) => 
        classInfo.Name.EndsWith("Test", StringComparison.OrdinalIgnoreCase) ||
        classInfo.Name.EndsWith("Tests", StringComparison.OrdinalIgnoreCase) ||
        classInfo.Attributes.Any(a => a.Name.Contains("TestClass") || a.Name.Contains("TestFixture"));

    private bool ShouldGenerateTestForMethod(Core.Models.MethodInfo method, AnalysisResult analysisResult)
    {
        // Skip private methods unless configured otherwise
        if (method.AccessModifier == AccessModifier.Private)
        {
            return false; // TODO: Check configuration
        }

        // Skip test methods
        if (method.Attributes.Any(a => a.Name.Contains("Test") || a.Name.Contains("Fact")))
        {
            return false;
        }

        // Check if test already exists
        var existingTests = analysisResult.ExistingTests?.ExistingTestMethods;
        if (existingTests != null && existingTests.ContainsKey(method.Name))
        {
            return false; // TODO: Check if we should add additional test cases
        }

        return true;
    }

    private bool ShouldGenerateTestForConstructor(Core.Models.ConstructorInfo constructor) => 
        constructor.AccessModifier == AccessModifier.Public;

    private bool ShouldGenerateTestForProperty(Core.Models.PropertyInfo property) => 
        property.AccessModifier == AccessModifier.Public && 
        property.HasGetter && 
        property.HasSetter;

    private string DetermineTestFramework(AnalysisResult analysisResult)
    {
        var testFrameworks = analysisResult.ProjectStructure?.NuGetInfo?.TestFrameworks;
        if (testFrameworks?.Any() == true)
        {
            var framework = testFrameworks.First();
            return framework.Type.ToString().ToLowerInvariant();
        }

        // Check existing test info
        if (!string.IsNullOrEmpty(analysisResult.ExistingTests?.TestFramework))
        {
            return analysisResult.ExistingTests.TestFramework.ToLowerInvariant();
        }

        return "xunit"; // Default
    }

    private string DetermineTemplateName(ClassInfo classInfo)
    {
        if (classInfo.IsController)
            return "csharp/controller-test";
        if (classInfo.IsService)
            return "csharp/service-test";
        
        return "csharp/unit-test";
    }

    private List<string> GetRequiredUsings(AnalysisResult analysisResult, string testFramework)
    {
        var usings = new List<string>
        {
            "System",
            analysisResult.Namespace
        };

        switch (testFramework.ToLowerInvariant())
        {
            case "xunit":
                usings.Add("Xunit");
                break;
            case "nunit":
                usings.Add("NUnit.Framework");
                break;
            case "mstest":
                usings.Add("Microsoft.VisualStudio.TestTools.UnitTesting");
                break;
        }

        // Add System.Threading.Tasks for async methods
        if (analysisResult.Classes.Any(c => c.Methods.Any(m => m.IsAsync)))
        {
            usings.Add("System.Threading.Tasks");
        }

        // Add mocking framework
        var mockingFrameworks = analysisResult.ProjectStructure?.NuGetInfo?.MockingFrameworks;
        if (mockingFrameworks?.Any() == true)
        {
            var mockingFramework = mockingFrameworks.First();
            usings.Add(mockingFramework.Name);
        }
        else
        {
            usings.Add("Moq"); // Default
        }

        // Add assertion libraries
        var assertionLibraries = analysisResult.ProjectStructure?.NuGetInfo?.AssertionLibraries;
        if (assertionLibraries?.Any() == true)
        {
            var assertionLibrary = assertionLibraries.First();
            usings.Add(assertionLibrary.Name);
        }
        else
        {
            usings.Add("FluentAssertions"); // Default
        }

        return usings.Distinct().ToList();
    }

    private object ExtractParametersFromTestCase(TestCase testCase)
    {
        var parameters = new List<object>();
        
        // Extract parameters from test data dictionary
        if (testCase.TestData != null && testCase.TestData.Count > 0)
        {
            foreach (var data in testCase.TestData)
            {
                var parameterInfo = CreateParameterFromTestData(data.Key, data.Value);
                if (parameterInfo != null)
                {
                    parameters.Add(parameterInfo);
                }
            }
        }
        
        // Extract parameters from scenario or arrange code
        if (!string.IsNullOrEmpty(testCase.Scenario))
        {
            var extractedParams = ParseParametersFromScenario(testCase.Scenario);
            parameters.AddRange(extractedParams);
        }
        
        // If no parameters found, create default ones based on common patterns
        if (parameters.Count == 0)
        {
            // Add some default parameters for common scenarios
            if (testCase.Type == TestType.Unit || testCase.Type == TestType.Service)
            {
                parameters.Add(new { name = "input", type = "string", value = "\"test\"" });
            }
            else if (testCase.Type == TestType.Controller)
            {
                parameters.Add(new { name = "id", type = "int", value = "1" });
            }
        }
        
        return parameters;
    }

    private object CreateParameterFromTestData(string key, object value)
    {
        if (value == null)
        {
            return new { name = key, type = "object", value = "null" };
        }
        
        var valueType = value.GetType();
        
        if (valueType == typeof(string))
        {
            return new { name = key, type = "string", value = $"\"{value}\"" };
        }
        
        if (valueType == typeof(int) || valueType == typeof(long))
        {
            return new { name = key, type = "int", value = value.ToString() };
        }
        
        if (valueType == typeof(bool))
        {
            return new { name = key, type = "bool", value = value.ToString()?.ToLower() ?? "false" };
        }
        
        if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
        {
            return new { name = key, type = "decimal", value = value.ToString() };
        }
        
        // Default to object type
        return new { name = key, type = "object", value = value.ToString() };
    }

    private List<object> ParseParametersFromScenario(string scenario)
    {
        var parameters = new List<object>();
        
        // Simple pattern matching for common scenarios
        if (scenario.Contains("null"))
        {
            parameters.Add(new { name = "input", type = "object", value = "null" });
        }
        else if (scenario.Contains("empty"))
        {
            parameters.Add(new { name = "input", type = "string", value = "\"\"" });
        }
        else if (scenario.Contains("valid"))
        {
            parameters.Add(new { name = "input", type = "string", value = "\"validInput\"" });
        }
        
        return parameters;
    }

    private string GetPropertyType(ClassInfo classInfo, string propertyName)
    {
        var property = classInfo.Properties.FirstOrDefault(p => p.Name == propertyName);
        return property?.Type ?? "object";
    }

    private string ExtractExceptionType(string expectedResult)
    {
        if (expectedResult.Contains("ArgumentNull"))
            return "ArgumentNullException";
        if (expectedResult.Contains("ArgumentOutOfRange"))
            return "ArgumentOutOfRangeException";
        if (expectedResult.Contains("InvalidOperation"))
            return "InvalidOperationException";
        
        return "Exception";
    }
}
