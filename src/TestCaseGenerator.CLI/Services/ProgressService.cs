using Spectre.Console;

namespace TestCaseGenerator.CLI;

/// <summary>
/// Service for displaying progress indicators
/// </summary>
public class ProgressService
{
    /// <summary>
    /// Executes an action with a progress bar
    /// </summary>
    public async Task<T> ExecuteWithProgressAsync<T>(string description, Func<IProgress<string>, Task<T>> action)
    {
        return await AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            )
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(description);
                var progress = new Progress<string>(status =>
                {
                    task.Description = status;
                    task.Increment(1);
                });

                var result = await action(progress);
                task.Value = 100;
                return result;
            });
    }

    /// <summary>
    /// Executes multiple tasks with individual progress tracking
    /// </summary>
    public async Task<T[]> ExecuteMultipleWithProgressAsync<T>(
        IEnumerable<(string Description, Func<IProgress<string>, Task<T>> Action)> tasks)
    {
        return await AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            )
            .StartAsync(async ctx =>
            {
                var progressTasks = new List<(ProgressTask Task, Func<IProgress<string>, Task<T>> Action)>();

                foreach (var (description, action) in tasks)
                {
                    var progressTask = ctx.AddTask(description);
                    progressTasks.Add((progressTask, action));
                }

                var results = new List<T>();

                foreach (var (progressTask, action) in progressTasks)
                {
                    var progress = new Progress<string>(status =>
                    {
                        progressTask.Description = status;
                        progressTask.Increment(1);
                    });

                    var result = await action(progress);
                    progressTask.Value = 100;
                    results.Add(result);
                }

                return results.ToArray();
            });
    }

    /// <summary>
    /// Shows a simple status message with spinner
    /// </summary>
    public async Task<T> ShowStatusAsync<T>(string message, Func<Task<T>> action)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green bold"))
            .StartAsync(message, async _ => await action());
    }

    /// <summary>
    /// Shows a countdown timer
    /// </summary>
    public async Task ShowCountdownAsync(TimeSpan duration, string message = "Processing...")
    {
        var endTime = DateTime.Now.Add(duration);

        await AnsiConsole.Live(new Panel($"[yellow]{message}[/]"))
            .StartAsync(async ctx =>
            {
                while (DateTime.Now < endTime)
                {
                    var remaining = endTime - DateTime.Now;
                    var display = $"[yellow]{message}[/]\n[green]Time remaining: {remaining:mm\\:ss}[/]";
                    
                    ctx.UpdateTarget(new Panel(display)
                        .Header("Progress")
                        .BorderColor(Color.Yellow));

                    await Task.Delay(1000);
                }
            });
    }

    /// <summary>
    /// Displays a file operation progress
    /// </summary>
    public async Task<T> ExecuteFileOperationAsync<T>(
        string operation,
        IEnumerable<string> files,
        Func<string, IProgress<string>, Task<T>> fileAction)
    {
        var fileList = files.ToList();
        var results = new List<T>();

        await AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            )
            .StartAsync(async ctx =>
            {
                var mainTask = ctx.AddTask($"[green]{operation}[/]", maxValue: fileList.Count);

                foreach (var file in fileList)
                {
                    var fileName = Path.GetFileName(file);
                    var progress = new Progress<string>(status =>
                    {
                        mainTask.Description = $"[green]{operation}[/] - {fileName}: {status}";
                    });

                    var result = await fileAction(file, progress);
                    results.Add(result);
                    mainTask.Increment(1);
                }
            });

        return results.FirstOrDefault()!;
    }
}
