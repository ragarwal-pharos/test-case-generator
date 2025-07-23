namespace TestCaseGenerator.Core.Models;

/// <summary>
/// Represents the structure of a project being analyzed
/// </summary>
public class ProjectStructure
{
    public string RootPath { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
    public List<ProjectReference> References { get; set; } = new();
    public List<PackageReference> Packages { get; set; } = new();
    public List<SourceFolder> SourceFolders { get; set; } = new();
    public List<TestFolder> TestFolders { get; set; } = new();
    public Dictionary<string, string> ProjectProperties { get; set; } = new();
    public List<string> ConfigurationFiles { get; set; } = new();
    public NuGetInfo NuGetInfo { get; set; } = new();
    public BuildInfo BuildInfo { get; set; } = new();
}

/// <summary>
/// Information about a project reference
/// </summary>
public class ProjectReference
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
}

/// <summary>
/// Information about a NuGet package reference
/// </summary>
public class PackageReference
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsTestPackage { get; set; }
    public PackageType Type { get; set; }
}

/// <summary>
/// Information about a source folder
/// </summary>
public class SourceFolder
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public List<string> Files { get; set; } = new();
    public FolderType Type { get; set; }
    public List<SourceFolder> SubFolders { get; set; } = new();
}

/// <summary>
/// Information about a test folder
/// </summary>
public class TestFolder
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public List<string> TestFiles { get; set; } = new();
    public string Framework { get; set; } = string.Empty;
    public List<string> TestedProjects { get; set; } = new();
    public TestingPattern Pattern { get; set; }
}

/// <summary>
/// Information about NuGet packages in the project
/// </summary>
public class NuGetInfo
{
    public List<TestFrameworkInfo> TestFrameworks { get; set; } = new();
    public List<MockingFrameworkInfo> MockingFrameworks { get; set; } = new();
    public List<AssertionLibraryInfo> AssertionLibraries { get; set; } = new();
    public List<string> TestingUtilities { get; set; } = new();
}

/// <summary>
/// Information about detected test frameworks
/// </summary>
public class TestFrameworkInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public TestFrameworkType Type { get; set; }
    public List<string> TestAttributes { get; set; } = new();
    public List<string> TestMethods { get; set; } = new();
}

/// <summary>
/// Information about detected mocking frameworks
/// </summary>
public class MockingFrameworkInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public MockingFrameworkType Type { get; set; }
    public List<string> MockMethods { get; set; } = new();
}

/// <summary>
/// Information about assertion libraries
/// </summary>
public class AssertionLibraryInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public AssertionLibraryType Type { get; set; }
    public List<string> AssertionMethods { get; set; } = new();
}

/// <summary>
/// Build information
/// </summary>
public class BuildInfo
{
    public string Configuration { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string OutputType { get; set; } = string.Empty;
    public bool HasWebConfig { get; set; }
    public bool HasAppConfig { get; set; }
    public List<string> BuildTargets { get; set; } = new();
}

/// <summary>
/// Package types
/// </summary>
public enum PackageType
{
    Unknown,
    TestFramework,
    MockingFramework,
    AssertionLibrary,
    TestUtility,
    ProductionDependency
}

/// <summary>
/// Folder types
/// </summary>
public enum FolderType
{
    Controllers,
    Services,
    Models,
    Repositories,
    ViewModels,
    Helpers,
    Extensions,
    Configuration,
    Data,
    Business,
    Web,
    Api,
    Other
}

/// <summary>
/// Testing patterns
/// </summary>
public enum TestingPattern
{
    Unknown,
    UnitTests,
    IntegrationTests,
    FunctionalTests,
    AcceptanceTests,
    PerformanceTests
}

/// <summary>
/// Test framework types
/// </summary>
public enum TestFrameworkType
{
    Unknown,
    XUnit,
    NUnit,
    MSTest,
    Jest,
    Mocha,
    Jasmine
}

/// <summary>
/// Mocking framework types
/// </summary>
public enum MockingFrameworkType
{
    Unknown,
    Moq,
    NSubstitute,
    FakeItEasy,
    Rhino,
    Jest,
    Sinon
}

/// <summary>
/// Assertion library types
/// </summary>
public enum AssertionLibraryType
{
    Unknown,
    FluentAssertions,
    Shouldly,
    NUnit,
    XUnit,
    MSTest,
    Jest,
    Chai
}
