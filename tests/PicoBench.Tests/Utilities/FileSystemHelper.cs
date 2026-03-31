namespace PicoBench.Tests.Utilities;

/// <summary>
/// Helper class for file system operations during testing.
/// Uses System.IO.Abstractions to allow for test cleanup without mocking.
/// </summary>
public static class FileSystemHelper
{
    private static readonly IFileSystem FileSystem = new FileSystem();

    /// <summary>
    /// Creates a unique temporary directory for test files.
    /// </summary>
    public static string CreateTestDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"PicoBenchTests_{Guid.NewGuid():N}");

        FileSystem.Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    /// <summary>
    /// Deletes a test directory and all its contents.
    /// </summary>
    public static void DeleteTestDirectory(string path)
    {
        if (FileSystem.Directory.Exists(path))
        {
            try
            {
                FileSystem.Directory.Delete(path, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors during tests
            }
        }
    }

    /// <summary>
    /// Creates a test file with the specified content.
    /// </summary>
    public static string CreateTestFile(string directory, string fileName, string content)
    {
        var filePath = Path.Combine(directory, fileName);
        FileSystem.File.WriteAllText(filePath, content);
        return filePath;
    }

    /// <summary>
    /// Reads the content of a test file.
    /// </summary>
    public static string ReadTestFile(string filePath)
    {
        return FileSystem.File.ReadAllText(filePath);
    }

    /// <summary>
    /// Checks if a file exists and contains the expected content.
    /// </summary>
    public static bool FileContains(string filePath, string expectedContent)
    {
        if (!FileSystem.File.Exists(filePath))
            return false;

        var content = FileSystem.File.ReadAllText(filePath);
        return content.Contains(expectedContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    public static long GetFileSize(string filePath)
    {
        if (!FileSystem.File.Exists(filePath))
            return 0;

        return FileSystem.FileInfo.New(filePath).Length;
    }

    /// <summary>
    /// Counts the number of lines in a file.
    /// </summary>
    public static int CountFileLines(string filePath)
    {
        if (!FileSystem.File.Exists(filePath))
            return 0;

        var content = FileSystem.File.ReadAllText(filePath);
        return content.Split('\n').Length;
    }

    /// <summary>
    /// Ensures a directory exists and is empty.
    /// </summary>
    public static string EnsureEmptyDirectory(string path)
    {
        if (FileSystem.Directory.Exists(path))
        {
            FileSystem.Directory.Delete(path, recursive: true);
        }

        FileSystem.Directory.CreateDirectory(path);
        return path;
    }
}
