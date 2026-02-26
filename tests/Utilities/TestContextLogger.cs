using TUnit.Core;

namespace Pico.Bench.Tests.Utilities;

/// <summary>
/// Extension methods for TestContext to facilitate logging during tests.
/// </summary>
public static class TestContextLogger
{
    /// <summary>
    /// Logs a message with timestamp to the test output.
    /// </summary>
    public static void Log(this TestContext context, string message)
    {
        context.OutputWriter.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }
    
    /// <summary>
    /// Logs the start of a test section.
    /// </summary>
    public static void LogSectionStart(this TestContext context, string sectionName)
    {
        context.OutputWriter.WriteLine();
        context.OutputWriter.WriteLine($"=== {sectionName} ===");
    }
    
    /// <summary>
    /// Logs the end of a test section.
    /// </summary>
    public static void LogSectionEnd(this TestContext context, string sectionName)
    {
        context.OutputWriter.WriteLine($"=== End {sectionName} ===");
        context.OutputWriter.WriteLine();
    }
    
    /// <summary>
    /// Logs a formatted object for debugging purposes.
    /// </summary>
    public static void LogObject(this TestContext context, string label, object obj)
    {
        context.OutputWriter.WriteLine($"{label}: {obj}");
    }
    
    /// <summary>
    /// Logs a collection of items for debugging purposes.
    /// </summary>
    public static void LogCollection<T>(this TestContext context, string label, IEnumerable<T> collection)
    {
        context.OutputWriter.WriteLine($"{label}:");
        foreach (var item in collection)
        {
            context.OutputWriter.WriteLine($"  - {item}");
        }
    }
    
    /// <summary>
    /// Logs file system operation details.
    /// </summary>
    public static void LogFileOperation(this TestContext context, string operation, string path)
    {
        context.OutputWriter.WriteLine($"[FileSystem] {operation}: {path}");
    }
    
    /// <summary>
    /// Logs performance timing information.
    /// </summary>
    public static void LogPerformance(this TestContext context, string operation, TimeSpan elapsed)
    {
        context.OutputWriter.WriteLine($"[Performance] {operation}: {elapsed.TotalMilliseconds:F2}ms");
    }
    
    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public static void LogWarning(this TestContext context, string message)
    {
        context.OutputWriter.WriteLine($"[WARNING] {message}");
    }
    
    /// <summary>
    /// Logs an error message.
    /// </summary>
    public static void LogError(this TestContext context, string message)
    {
        context.OutputWriter.WriteLine($"[ERROR] {message}");
    }
    
    /// <summary>
    /// Logs a success message.
    /// </summary>
    public static void LogSuccess(this TestContext context, string message)
    {
        context.OutputWriter.WriteLine($"[SUCCESS] {message}");
    }
}