using System.IO.Compression;

namespace JMC.Logging;
public class Logger
{
    private string LogFilePath;
    private string LogDirectoryPath;
    private readonly bool Immediate;
    private readonly List<LogEntry> Logs = [];

    public Logger(bool immediate = false)
    {
        Immediate = immediate;

        SetLogPathToCurrentDirectory();

        // Check if the log file already exists
        if (File.Exists(LogFilePath))
        {
            // Create a timestamp for renaming the old log file
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            // Rename the old log file with a timestamp
            string renamedLogFilePath = $"{timestamp}{Path.GetExtension(LogFilePath)}";
            File.Move(LogFilePath, renamedLogFilePath);

            // Compress the old log file into .gz format
            CompressFile(renamedLogFilePath);
        }

        Log($"------ Log Initialized at {DateTime.Now} ------", LogLevel.Initialization, "");
    }

    public void Log(string message, LogLevel logLevel = LogLevel.Info, string prefix = "{DateTime.Now} [{logLevel}] - ")
    {
        string logEntry = $"{prefix}{message}";

        Logs.Add(new(logEntry, logLevel));

        if (Immediate)
            Dump();
    }

    public void Dump(LogLevel minimum = LogLevel.None)
    {
        // Create the "logs" directory if it doesn't exist
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

    public void SetLogPathToCurrentDirectory()
    {
        // Set the log file path within the "logs" directory
        LogDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        LogFilePath = Path.Combine(LogDirectoryPath, "latest.log");

        Log(Immediate ? $"------ Log Initialized at {DateTime.Now} ------" 
            : $"\n\n------ Log path changed at {DateTime.Now}  ------", LogLevel.Initialization, "");
    }


    public void DeleteOldGzipFiles()
    {
        try
        {
            // Check if the directory exists
            if (!Directory.Exists(LogDirectoryPath))
            {
                Log($"Directory does not exist: {LogDirectoryPath}", LogLevel.Error);
                return;
            }

            // Get all Gzip files in the directory
            string[] gzipFiles = Directory.GetFiles(LogDirectoryPath, "*.gz");

            // Delete each Gzip file
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

    private void CompressFile(string filePathToCompress)
    {
        using (FileStream sourceFileStream = File.OpenRead(filePathToCompress))
        {
            string compressedFilePath = Path.Combine(LogDirectoryPath, $"{filePathToCompress}.gz");
            // Create a new .gz file with the same name
            using FileStream compressedFileStream = File.Create(compressedFilePath);
            // Use GZipStream to compress the file
            using GZipStream compressionStream = new(compressedFileStream, CompressionMode.Compress);
            // Copy the contents of the old log file to the compressed file
            sourceFileStream.CopyTo(compressionStream);
        }

        // Delete the old log file after compression
        File.Delete(filePathToCompress);
    }
}