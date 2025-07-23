using Microsoft.Extensions.Logging;
using Stubble.Core.Builders;
using TestCaseGenerator.Core.Interfaces;
using System.Reflection;

namespace TestCaseGenerator.Templates.Engine;

/// <summary>
/// Template engine implementation using Stubble (Mustache templates)
/// </summary>
public class MustacheTemplateEngine : ITemplateEngine
{
    private readonly ILogger<MustacheTemplateEngine> _logger;
    private readonly Dictionary<string, string> _templates = new();
    private readonly Stubble.Core.StubbleVisitorRenderer _renderer;

    public MustacheTemplateEngine(ILogger<MustacheTemplateEngine> logger)
    {
        _logger = logger;
        _renderer = new StubbleBuilder().Build();
        
        // Load embedded templates
        LoadEmbeddedTemplatesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public IEnumerable<string> AvailableTemplates => _templates.Keys;

    public async Task<string> RenderTemplateAsync(string templateName, object data)
    {
        try
        {
            if (!_templates.TryGetValue(templateName, out var template))
            {
                throw new ArgumentException($"Template '{templateName}' not found", nameof(templateName));
            }

            _logger.LogDebug("Rendering template: {TemplateName}", templateName);
            
            var result = await _renderer.RenderAsync(template, data);
            
            _logger.LogDebug("Template rendered successfully: {TemplateName}", templateName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template: {TemplateName}", templateName);
            throw;
        }
    }

    public void RegisterTemplate(string templateName, string templateContent)
    {
        _templates[templateName] = templateContent;
        _logger.LogDebug("Registered template: {TemplateName}", templateName);
    }

    public async Task LoadTemplatesAsync(string templateDirectory, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading templates from directory: {TemplateDirectory}", templateDirectory);

            if (!Directory.Exists(templateDirectory))
            {
                _logger.LogWarning("Template directory does not exist: {TemplateDirectory}", templateDirectory);
                return;
            }

            var templateFiles = Directory.GetFiles(templateDirectory, "*.mustache", SearchOption.AllDirectories);
            
            foreach (var templateFile in templateFiles)
            {
                var templateName = GetTemplateNameFromPath(templateFile, templateDirectory);
                var templateContent = await File.ReadAllTextAsync(templateFile, cancellationToken);
                
                RegisterTemplate(templateName, templateContent);
            }

            _logger.LogInformation("Loaded {TemplateCount} templates from {TemplateDirectory}", 
                templateFiles.Length, templateDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading templates from directory: {TemplateDirectory}", templateDirectory);
            throw;
        }
    }

    /// <summary>
    /// Loads embedded templates from the assembly
    /// </summary>
    private async Task LoadEmbeddedTemplatesAsync()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allResourceNames = assembly.GetManifestResourceNames();
            _logger.LogInformation("All embedded resources: {Resources}", string.Join(", ", allResourceNames));
            
            var resourceNames = allResourceNames
                .Where(name => name.Contains("Templates") && name.EndsWith(".mustache"))
                .ToList();

            _logger.LogInformation("Found {ResourceCount} embedded template resources: {Resources}", 
                resourceNames.Count, string.Join(", ", resourceNames));

            foreach (var resourceName in resourceNames)
            {
                _logger.LogInformation("Processing resource: {ResourceName}", resourceName);
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) 
                {
                    _logger.LogWarning("Could not load resource stream for: {ResourceName}", resourceName);
                    continue;
                }

                using var reader = new StreamReader(stream);
                var templateContent = await reader.ReadToEndAsync();
                
                var templateName = GetTemplateNameFromResourceName(resourceName);
                _logger.LogInformation("Mapped resource '{ResourceName}' to template name '{TemplateName}'", resourceName, templateName);
                
                RegisterTemplate(templateName, templateContent);
                _logger.LogInformation("Registered template '{TemplateName}' with content length {Length}", templateName, templateContent.Length);
            }

            _logger.LogInformation("Loaded {TemplateCount} embedded templates", resourceNames.Count);
            _logger.LogInformation("Available templates after loading: {AvailableTemplates}", string.Join(", ", _templates.Keys));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading embedded templates");
            throw;
        }
    }

    /// <summary>
    /// Converts file path to template name
    /// </summary>
    private string GetTemplateNameFromPath(string filePath, string baseDirectory)
    {
        var relativePath = Path.GetRelativePath(baseDirectory, filePath);
        var templateName = relativePath.Replace('\\', '/').Replace(".mustache", "");
        return templateName;
    }

    /// <summary>
    /// Converts resource name to template name
    /// </summary>
    private string GetTemplateNameFromResourceName(string resourceName)
    {
        // Expected format: TestCaseGenerator.Templates.Templates.csharp.unit-test.mustache
        // We want: csharp/unit-test
        var parts = resourceName.Split('.');
        var templateParts = new List<string>();
        
        // Find the second occurrence of "Templates" and start from there
        int templatesCount = 0;
        bool startCollecting = false;
        
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            
            if (part == "Templates")
            {
                templatesCount++;
                if (templatesCount == 2) // Second "Templates" folder
                {
                    startCollecting = true;
                    continue; // Skip this "Templates" part
                }
            }
            else if (startCollecting && part != "mustache")
            {
                templateParts.Add(part.ToLowerInvariant());
            }
        }

        return string.Join("/", templateParts);
    }
}
