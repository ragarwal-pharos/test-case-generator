using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using TestCaseGenerator.Core.Interfaces;
using TestCaseGenerator.Core.Models;
using System.Text.RegularExpressions;

namespace TestCaseGenerator.Analyzers.CSharp;

/// <summary>
/// Analyzes C# source files to extract class, method, and dependency information
/// </summary>
public class CSharpAnalyzer : ICodeAnalyzer
{
    private readonly ILogger<CSharpAnalyzer> _logger;
    private static readonly string[] SupportedExtensionsArray = { ".cs" };

    public CSharpAnalyzer(ILogger<CSharpAnalyzer> logger)
    {
        _logger = logger;
    }

    public string Name => "C# Analyzer";

    public IEnumerable<string> SupportedExtensions => SupportedExtensionsArray;

    public bool CanAnalyze(string filePath)
    {
        return SupportedExtensions.Any(ext => filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<AnalysisResult> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Analyzing C# file: {FilePath}", filePath);

        try
        {
            var sourceCode = await File.ReadAllTextAsync(filePath, cancellationToken);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, cancellationToken: cancellationToken);
            var root = await syntaxTree.GetRootAsync(cancellationToken);

            var result = new AnalysisResult
            {
                FilePath = filePath,
                FileType = "csharp"
            };

            // Extract namespace
            var namespaceDeclaration = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
            result.Namespace = namespaceDeclaration?.Name?.ToString() ?? string.Empty;

            // Extract using statements
            result.UsingStatements = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString() ?? string.Empty)
                .Where(u => !string.IsNullOrEmpty(u))
                .ToList();

            // Analyze classes
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDeclaration in classes)
            {
                var classInfo = await AnalyzeClassAsync(classDeclaration, result.Namespace, cancellationToken);
                result.Classes.Add(classInfo);
                
                // Add methods and properties from this class to the global lists
                result.Methods.AddRange(classInfo.Methods);
                result.Properties.AddRange(classInfo.Properties);
            }

            // Analyze dependencies
            result.Dependencies = AnalyzeDependencies(result.Classes);

            _logger.LogDebug("Analysis completed for {FilePath}. Found {ClassCount} classes, {MethodCount} methods", 
                filePath, result.Classes.Count, result.Methods.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing C# file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<List<AnalysisResult>> AnalyzeBatchAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
    {
        var tasks = filePaths.Select(fp => AnalyzeAsync(fp, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    /// <summary>
    /// Analyzes a C# class declaration
    /// </summary>
    private async Task<ClassInfo> AnalyzeClassAsync(ClassDeclarationSyntax classDeclaration, string namespaceName, CancellationToken cancellationToken)
    {
        var classInfo = new ClassInfo
        {
            Name = classDeclaration.Identifier.ValueText,
            FullName = string.IsNullOrEmpty(namespaceName) ? classDeclaration.Identifier.ValueText : $"{namespaceName}.{classDeclaration.Identifier.ValueText}",
            Namespace = namespaceName,
            AccessModifier = GetAccessModifier(classDeclaration.Modifiers),
            IsAbstract = classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)),
            IsSealed = classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.SealedKeyword)),
            IsStatic = classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))
        };

        // Detect common patterns
        classInfo.IsController = IsController(classInfo.Name, classDeclaration);
        classInfo.IsService = IsService(classInfo.Name, classDeclaration);
        classInfo.IsRepository = IsRepository(classInfo.Name, classDeclaration);

        // Extract base types and interfaces
        if (classDeclaration.BaseList != null)
        {
            foreach (var baseType in classDeclaration.BaseList.Types)
            {
                var typeName = baseType.Type.ToString();
                if (typeName.StartsWith("I") && char.IsUpper(typeName[1]))
                {
                    classInfo.Interfaces.Add(typeName);
                }
                else
                {
                    classInfo.BaseTypes.Add(typeName);
                }
            }
        }

        // Extract attributes
        classInfo.Attributes = ExtractAttributes(classDeclaration.AttributeLists);

        // Analyze methods
        var methods = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
        foreach (var method in methods)
        {
            var methodInfo = AnalyzeMethod(method);
            classInfo.Methods.Add(methodInfo);
        }

        // Analyze properties
        var properties = classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>();
        foreach (var property in properties)
        {
            var propertyInfo = AnalyzeProperty(property);
            classInfo.Properties.Add(propertyInfo);
        }

        // Analyze constructors
        var constructors = classDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
        foreach (var constructor in constructors)
        {
            var constructorInfo = AnalyzeConstructor(constructor);
            classInfo.Constructors.Add(constructorInfo);
        }

        await Task.Delay(1, cancellationToken); // Yield control
        return classInfo;
    }

    /// <summary>
    /// Analyzes a method declaration
    /// </summary>
    private Core.Models.MethodInfo AnalyzeMethod(MethodDeclarationSyntax method)
    {
        var methodInfo = new Core.Models.MethodInfo
        {
            Name = method.Identifier.ValueText,
            ReturnType = method.ReturnType.ToString(),
            AccessModifier = GetAccessModifier(method.Modifiers),
            IsAsync = method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)) || method.ReturnType.ToString().Contains("Task"),
            IsStatic = method.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
            IsVirtual = method.Modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)),
            IsOverride = method.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)),
            IsAbstract = method.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword))
        };

        // Extract parameters
        if (method.ParameterList != null)
        {
            foreach (var param in method.ParameterList.Parameters)
            {
                var paramInfo = new ParameterInfo
                {
                    Name = param.Identifier.ValueText,
                    Type = param.Type?.ToString() ?? "object",
                    IsOptional = param.Default != null,
                    DefaultValue = param.Default?.Value?.ToString(),
                    IsParams = param.Modifiers.Any(m => m.IsKind(SyntaxKind.ParamsKeyword)),
                    IsOut = param.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword)),
                    IsRef = param.Modifiers.Any(m => m.IsKind(SyntaxKind.RefKeyword)),
                    Attributes = ExtractAttributes(param.AttributeLists)
                };
                methodInfo.Parameters.Add(paramInfo);
            }
        }

        // Extract attributes
        methodInfo.Attributes = ExtractAttributes(method.AttributeLists);

        // Extract documentation
        var documentationComment = method.GetLeadingTrivia()
            .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
        
        if (!documentationComment.IsKind(SyntaxKind.None))
        {
            methodInfo.Documentation = documentationComment.ToString();
        }

        // Calculate cyclomatic complexity (simplified)
        methodInfo.CyclomaticComplexity = CalculateCyclomaticComplexity(method);

        return methodInfo;
    }

    /// <summary>
    /// Analyzes a property declaration
    /// </summary>
    private Core.Models.PropertyInfo AnalyzeProperty(PropertyDeclarationSyntax property)
    {
        var propertyInfo = new Core.Models.PropertyInfo
        {
            Name = property.Identifier.ValueText,
            Type = property.Type.ToString(),
            AccessModifier = GetAccessModifier(property.Modifiers)
        };

        // Check accessors
        if (property.AccessorList != null)
        {
            foreach (var accessor in property.AccessorList.Accessors)
            {
                if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                {
                    propertyInfo.HasGetter = true;
                }
                else if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                {
                    propertyInfo.HasSetter = true;
                }
            }

            // Check if it's an auto-property
            propertyInfo.IsAutoProperty = property.AccessorList.Accessors
                .All(a => a.Body == null && a.ExpressionBody == null);
        }

        // Extract attributes
        propertyInfo.Attributes = ExtractAttributes(property.AttributeLists);

        // Extract default value for expression-bodied properties
        if (property.ExpressionBody != null)
        {
            propertyInfo.DefaultValue = property.ExpressionBody.Expression.ToString();
        }

        return propertyInfo;
    }

    /// <summary>
    /// Analyzes a constructor declaration
    /// </summary>
    private Core.Models.ConstructorInfo AnalyzeConstructor(ConstructorDeclarationSyntax constructor)
    {
        var constructorInfo = new Core.Models.ConstructorInfo
        {
            AccessModifier = GetAccessModifier(constructor.Modifiers)
        };

        // Extract parameters
        if (constructor.ParameterList != null)
        {
            foreach (var param in constructor.ParameterList.Parameters)
            {
                var paramInfo = new ParameterInfo
                {
                    Name = param.Identifier.ValueText,
                    Type = param.Type?.ToString() ?? "object",
                    IsOptional = param.Default != null,
                    DefaultValue = param.Default?.Value?.ToString(),
                    Attributes = ExtractAttributes(param.AttributeLists)
                };
                constructorInfo.Parameters.Add(paramInfo);
            }
        }

        // Check for base/this calls
        if (constructor.Initializer != null)
        {
            constructorInfo.CallsBase = constructor.Initializer.IsKind(SyntaxKind.BaseConstructorInitializer);
            constructorInfo.CallsThis = constructor.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer);
        }

        // Extract documentation
        var documentationComment = constructor.GetLeadingTrivia()
            .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
        
        if (!documentationComment.IsKind(SyntaxKind.None))
        {
            constructorInfo.Documentation = documentationComment.ToString();
        }

        return constructorInfo;
    }

    /// <summary>
    /// Analyzes dependencies between classes
    /// </summary>
    private List<DependencyInfo> AnalyzeDependencies(List<ClassInfo> classes)
    {
        var dependencies = new List<DependencyInfo>();

        foreach (var classInfo in classes)
        {
            // Analyze constructor dependencies (dependency injection pattern)
            foreach (var constructor in classInfo.Constructors)
            {
                foreach (var parameter in constructor.Parameters)
                {
                    if (IsInterface(parameter.Type) || IsServiceType(parameter.Type))
                    {
                        var dependency = new DependencyInfo
                        {
                            Name = parameter.Name,
                            Type = parameter.Type,
                            InterfaceType = parameter.Type,
                            IsInjected = true,
                            RequiresMock = true,
                            Lifetime = DetermineDependencyLifetime(parameter.Type)
                        };
                        dependencies.Add(dependency);
                    }
                }
            }
        }

        return dependencies;
    }

    /// <summary>
    /// Extracts attributes from attribute lists
    /// </summary>
    private List<AttributeInfo> ExtractAttributes(SyntaxList<AttributeListSyntax> attributeLists)
    {
        var attributes = new List<AttributeInfo>();

        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeInfo = new AttributeInfo
                {
                    Name = attribute.Name.ToString(),
                    FullName = attribute.Name.ToString()
                };

                if (attribute.ArgumentList != null)
                {
                    foreach (var argument in attribute.ArgumentList.Arguments)
                    {
                        if (argument.NameEquals != null)
                        {
                            attributeInfo.NamedArguments[argument.NameEquals.Name.ToString()] = argument.Expression.ToString();
                        }
                        else
                        {
                            attributeInfo.Arguments.Add(argument.Expression.ToString());
                        }
                    }
                }

                attributes.Add(attributeInfo);
            }
        }

        return attributes;
    }

    /// <summary>
    /// Gets the access modifier from syntax tokens
    /// </summary>
    private AccessModifier GetAccessModifier(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            return AccessModifier.Public;
        if (modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)) && modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword)))
            return AccessModifier.ProtectedInternal;
        if (modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)) && modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
            return AccessModifier.PrivateProtected;
        if (modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
            return AccessModifier.Protected;
        if (modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword)))
            return AccessModifier.Internal;
        if (modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
            return AccessModifier.Private;

        return AccessModifier.Private; // Default
    }

    /// <summary>
    /// Calculates cyclomatic complexity for a method
    /// </summary>
    private int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
    {
        var complexity = 1; // Base complexity

        var complexityNodes = method.DescendantNodes().Where(node =>
            node.IsKind(SyntaxKind.IfStatement) ||
            node.IsKind(SyntaxKind.WhileStatement) ||
            node.IsKind(SyntaxKind.ForStatement) ||
            node.IsKind(SyntaxKind.ForEachStatement) ||
            node.IsKind(SyntaxKind.SwitchStatement) ||
            node.IsKind(SyntaxKind.CatchClause) ||
            node.IsKind(SyntaxKind.ConditionalExpression) ||
            node.IsKind(SyntaxKind.LogicalAndExpression) ||
            node.IsKind(SyntaxKind.LogicalOrExpression));

        complexity += complexityNodes.Count();

        return complexity;
    }

    /// <summary>
    /// Determines if a class is a controller
    /// </summary>
    private bool IsController(string className, ClassDeclarationSyntax classDeclaration)
    {
        return className.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) ||
               classDeclaration.BaseList?.Types.Any(t => t.Type.ToString().Contains("Controller")) == true ||
               classDeclaration.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString().Contains("ApiController") || a.Name.ToString().Contains("Controller")));
    }

    /// <summary>
    /// Determines if a class is a service
    /// </summary>
    private bool IsService(string className, ClassDeclarationSyntax classDeclaration)
    {
        return className.EndsWith("Service", StringComparison.OrdinalIgnoreCase) ||
               className.Contains("Service", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if a class is a repository
    /// </summary>
    private bool IsRepository(string className, ClassDeclarationSyntax classDeclaration)
    {
        return className.EndsWith("Repository", StringComparison.OrdinalIgnoreCase) ||
               className.Contains("Repository", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if a type is an interface
    /// </summary>
    private bool IsInterface(string typeName)
    {
        return typeName.StartsWith("I") && char.IsUpper(typeName[1]);
    }

    /// <summary>
    /// Determines if a type is likely a service type
    /// </summary>
    private bool IsServiceType(string typeName)
    {
        var servicePatterns = new[] { "Service", "Repository", "Manager", "Handler", "Provider", "Factory" };
        return servicePatterns.Any(pattern => typeName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines the dependency lifetime based on type name
    /// </summary>
    private DependencyLifetime DetermineDependencyLifetime(string typeName)
    {
        // This is a simplified heuristic - in real scenarios, you'd analyze DI registration
        if (typeName.Contains("Repository", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Context", StringComparison.OrdinalIgnoreCase))
        {
            return DependencyLifetime.Scoped;
        }

        if (typeName.Contains("Configuration", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Settings", StringComparison.OrdinalIgnoreCase))
        {
            return DependencyLifetime.Singleton;
        }

        return DependencyLifetime.Transient;
    }
}
