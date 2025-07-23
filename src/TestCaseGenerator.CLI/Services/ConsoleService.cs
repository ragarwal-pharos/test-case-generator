using Spectre.Console;

namespace TestCaseGenerator.CLI;

/// <summary>
/// Service for console output with formatted styling
/// </summary>
public class ConsoleService
{
    /// <summary>
    /// Writes a formatted header to the console
    /// </summary>
    public void WriteHeader(string title)
    {
        AnsiConsole.Write(
            new FigletText(title)
                .LeftJustified()
                .Color(Color.Blue));
        
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Writes a success message to the console
    /// </summary>
    public void WriteSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]{EscapeMarkup(message)}[/]");
    }

    /// <summary>
    /// Writes an error message to the console
    /// </summary>
    public void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"[red]{EscapeMarkup(message)}[/]");
    }

    /// <summary>
    /// Writes a warning message to the console
    /// </summary>
    public void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]{EscapeMarkup(message)}[/]");
    }

    /// <summary>
    /// Writes an info message to the console
    /// </summary>
    public void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine($"[cyan]{EscapeMarkup(message)}[/]");
    }

    /// <summary>
    /// Writes a plain message to the console
    /// </summary>
    public void WriteLine(string message)
    {
        AnsiConsole.WriteLine(message);
    }

    /// <summary>
    /// Prompts the user for confirmation
    /// </summary>
    public bool Confirm(string message, bool defaultValue = false)
    {
        return AnsiConsole.Confirm(message, defaultValue);
    }

    /// <summary>
    /// Prompts the user for input
    /// </summary>
    public string Prompt(string message, string? defaultValue = null)
    {
        var prompt = new TextPrompt<string>(message);
        
        if (!string.IsNullOrEmpty(defaultValue))
        {
            prompt.DefaultValue(defaultValue);
        }

        return AnsiConsole.Prompt(prompt);
    }

    /// <summary>
    /// Creates a table for displaying structured data
    /// </summary>
    public Table CreateTable()
    {
        return new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);
    }

    /// <summary>
    /// Creates a panel for grouping content
    /// </summary>
    public Panel CreatePanel(string title, string content)
    {
        return new Panel(content)
            .Header(title)
            .BorderColor(Color.Blue)
            .Padding(1, 0);
    }

    /// <summary>
    /// Creates a rule (horizontal line) with optional title
    /// </summary>
    public void WriteRule(string? title = null)
    {
        var rule = new Rule();
        
        if (!string.IsNullOrEmpty(title))
        {
            rule.Title = title;
        }
        
        rule.RuleStyle("grey dim");
        AnsiConsole.Write(rule);
    }

    /// <summary>
    /// Displays a tree structure
    /// </summary>
    public void WriteTree(Tree tree)
    {
        AnsiConsole.Write(tree);
    }

    /// <summary>
    /// Displays a status with spinner
    /// </summary>
    public async Task<T> StatusAsync<T>(string message, Func<Task<T>> action)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green bold"))
            .StartAsync(message, async _ => await action());
    }

    /// <summary>
    /// Escapes markup characters for safe display
    /// </summary>
    private string EscapeMarkup(string text)
    {
        return text.Replace("[", "[[").Replace("]", "]]");
    }
}
