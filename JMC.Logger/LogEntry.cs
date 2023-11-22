namespace JMC.Logging;

internal class LogEntry(string content, LogLevel logLevel)
{
    internal string Content = content;
    internal LogLevel LogLevel = logLevel;
}
