using System.IO.Compression;

namespace JMC.Logging;
public class Logger
{
    private string LogFilePath;
    private string LogDirectoryPath;
    private readonly bool Immediate;
    private readonly List<LogEntry> Logs = [];

    /// <summary>
    /// Initializes a new instance of the Logger class with optional immediate log dumping.
    /// </summary>
    /// <param name="immediate">Specifies whether logs should be immediately dumped upon each log entry.</param>
public Logger(bool immediate = false)
    {
        Immediate = immediate;

        LogDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        LogFilePath = Path.Combine(LogDirectoryPath, "latest.log");

        if (File.Exists(LogFilePath))
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string renamedLogFilePath = $"{timestamp}{Path.GetExtension(LogFilePath)}";
            File.Move(LogFilePath, renamedLogFilePath);

            CompressFile(renamedLogFilePath);
        }

        Log($"------ Log Initialized at {DateTime.Now} ------", LogLevel.Initialization, "");
    }

    /// <summary>
    /// Adds a log entry with the specified message, log level, and optional prefix to the log collection.
    /// If Immediate is true, the log entry is immediately written to the log file.
    /// </summary>
    /// <param name="message">The log message to be added.</param>
    /// <param name="logLevel">The log level of the entry (default is Info).</param>
    /// <param name="prefix">An optional prefix for the log entry (default includes timestamp and log level).</param>
    public void Log(string message, LogLevel logLevel = LogLevel.Info, string prefix = "{DateTime.Now} [{logLevel}] - ")
    {
        string logEntry = $"{prefix}{message}";
        Logs.Add(new(logEntry, logLevel));

        if (Immediate)
            Dump();
    }

    /// <summary>
    /// Writes log entries with a minimum log level to the log file.
    /// </summary>
    /// <param name="minimum">Minimum log level to include in the log file.</param>
    public void Dump(LogLevel minimum = LogLevel.None)
    {
        Directory.CreateDirectory(LogDirectoryPath);

        using StreamWriter logFile = File.AppendText(LogFilePath);
        foreach(LogEntry logEntry in Logs)
        {
            if (logEntry.LogLevel >= minimum)
            {
                logFile.WriteLine(logEntry.Content);
                Logs.Remove(logEntry);
            }
        }
    }

    /// <summary>
    /// Sets the log path to the current directory and initializes the log file.
    /// </summary>
    public void SetLogPathToCurrentDirectory()
    {
        LogDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        LogFilePath = Path.Combine(LogDirectoryPath, "latest.log");

        Log(Immediate ? $"------ Log Initialized at {DateTime.Now} ------" 
            : $"\n\n------ Log path changed at {DateTime.Now}  ------", LogLevel.Initialization, "");
    }

    /// <summary>
    /// Deletes old Gzip files in the log directory.
    /// </summary>
    public void DeleteOldGzipFiles()
    {
        try
        {
            if (!Directory.Exists(LogDirectoryPath))
            {
                Log($"Directory does not exist: {LogDirectoryPath}", LogLevel.Error);
                return;
            }

            string[] gzipFiles = Directory.GetFiles(LogDirectoryPath, "*.gz");

            foreach (string gzipFile in gzipFiles)
            {
                File.Delete(gzipFile);
                Log($"Deleted Gzip file: {gzipFile}");
            }
        }
        catch (Exception ex)
        {
            Log($"Error deleting Gzip files: {ex.Message}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Compresses a specified file using GZip compression and deletes the original file.
    /// </summary>
    /// <param name="filePathToCompress">The path of the file to compress.</param>
    private void CompressFile(string filePathToCompress)
    {
        using (FileStream sourceFileStream = File.OpenRead(filePathToCompress))
        {
            string compressedFilePath = Path.Combine(LogDirectoryPath, $"{filePathToCompress}.gz");
            using FileStream compressedFileStream = File.Create(compressedFilePath);
            using GZipStream compressionStream = new(compressedFileStream, CompressionMode.Compress);
            sourceFileStream.CopyTo(compressionStream);
        }
        File.Delete(filePathToCompress);
    }
}