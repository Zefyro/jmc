using JMC.Exceptions;
using JMC.Logging;
using System.Diagnostics;

namespace JMC.Terminal;
public sealed class TerminalCommands
{
    /// <summary>
    /// Displays help information about available commands or provides detailed help for a specific command.
    /// </summary>
    /// <param name="args">Command arguments.</param>
    [AddCommand("help", "help [<command>]", "Output this message or get help for a command")]
    public void HelpCommand(string[] args)
    {
        if (args.Length == 0)
        {
            string availableCommands = "\n";
            foreach (KeyValuePair<string, (Action<string[]> command, string usage, string description)> command in CommandManager.Dictionary)
            {
                availableCommands += $"- {command.Value.usage}: {command.Value.description}\n";
            }

            Interface.PrettyPrint($"Available commands:{availableCommands}", Colors.Yellow);
            return;
        }

        if (!CommandManager.Dictionary.TryGetValue(args[0], out var value))
        {
            Interface.PrettyPrint($"Unrecognized command '{args[0]}'", Colors.Fail);
            return;
        }

        Interface.PrettyPrint($"{value.usage}: {value.description}", Colors.Yellow);
    }

    /// <summary>
    /// Exits the JMC compiler.
    /// </summary>
    /// <param name="args">Command arguments (not used in this command).</param>
    [AddCommand("exit", "exit", "Exit JMC compiler")]
    public void ExitCommand(string[] args)
    {
        Interface.Logger.Log("JMC exited with exit code: 0", LogLevel.Fatal);
        Environment.Exit(0);
    }

    /// <summary>
    /// Command for compiling the main JMC file.
    /// </summary>
    /// <param name="args">Command-line arguments, accepts "debug" for debug mode.</param>
    [AddCommand("compile", "compile [debug]", "Compile main JMC file")]
    public void CompileCommand(string[] args)
    {
        bool debugCompile = false;

        if (args.Length != 0)
        {
            if (args[0] != "debug")
                throw new ArgumentException($"Unrecognized argument '{args[0]}'");

            Interface.PrettyPrint("DEBUG MODE", Colors.Info);
            debugCompile = args[0] == "debug";
        }

        Interface.PrettyPrint("Compiling...", Colors.Info);

        if (!Interface.Configuration.HasConfig)
        {
            Interface.Configuration.Initialize([]);
            return;
        }

        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            double buildTime = double.Round(Interface.Compile() / 1000, 5);
            stopwatch.Stop();
            double elapsedSeconds = double.Round(stopwatch.Elapsed.TotalMilliseconds - buildTime / 1000, 5);

            Interface.PrettyPrint($"Compiled successfully in {elapsedSeconds} seconds, datapack built in {buildTime} seconds.", Colors.Info);
        }
        catch (Exception ex)
        {
            if (ex.GetType().ToString().StartsWith("JMC."))
            {
                Interface.Logger.Log(ex.ToString(), LogLevel.Debug);
                Interface.ReportError(ex);
            }
            else
            {
                Interface.Logger.Log("Non-JMC Error occur", LogLevel.Fatal);
                Interface.HandleException(ex, isOk: false); ;
            }
        }

        if (debugCompile)
            Interface.Logger.Dump();
    }

    private static readonly string[] LogArgs = ["debug", "info", "clear"];

    /// <summary>
    /// Handles logging commands, allowing users to create log files or clear existing logs.
    /// </summary>
    /// <param name="args">Command arguments.</param>
    [AddCommand("log", "log debug|info|clear", "\r\n  - debug|info: Create log file in output directory\r\n  - clear: Delete every log file inside log folder except latest")]
    public void LogCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Interface.PrettyPrint($"Unrecognized command: No arguments provided.", Colors.Fail);
            return;
        }

        if (args.Length > 1 || !LogArgs.Contains(args[0]))
        {
            Interface.PrettyPrint($"Invalid arguments. Usage: log debug|info|clear", Colors.Fail);
            return;
        }

        switch (args[0])
        {
            case "debug":
                Interface.Logger.Dump(LogLevel.Debug);
                return;

            case "info":
                Interface.Logger.Dump(LogLevel.Info);
                return;

            case "clear":
                Interface.Logger.DeleteOldGzipFiles();
                return;

            default:
                throw new ArgumentException($"Invalid argument '{args[0]}'");
        }
    }

    /// <summary>
    /// Displays JMC's version information.
    /// </summary>
    /// <param name="args">Command arguments (not used in this command).</param>
    [AddCommand("version", "version", "Get JMC's version info")]
    public void VersionCommand(string[] args)
    {
        Interface.PrettyPrint(Configuration.Version, Colors.Info);
    }

    private static readonly string[] ConfigArgs = ["edit", "reset"];

    /// <summary>
    /// Handles commands related to configuring the workspace, including editing existing configurations or resetting them.
    /// </summary>
    /// <param name="args">Command arguments.</param>
    [AddCommand("config", "config [edit|reset]", "Setup workspace's configuration\r\n  - reset: Delete the configuration file and restart the compiler\r\n  - edit: Override configuration file and bypass error checking")]
    public void ConfigCommand(string[] args)
    {
        if (args.Length == 0)
        {
            if (!Interface.Configuration.HasConfig)
            {
                Interface.Initialize([]);
                return;
            }

            Interface.PrettyPrint("Configuration file is already generated. To reset, use `config reset`", Colors.Fail);
            return;
        }

        if (args.Length > 1 || !ConfigArgs.Contains(args[0]))
        {
            Interface.PrettyPrint($"Invalid number of arguments. Usage: config [edit|reset]", Colors.Fail);
            return;
        }

        switch (args[0])
        {
            case "reset":
                File.Delete(Path.GetFullPath(Configuration.FileName));
                Interface.Logger.Log("Resetting configurations");
                Interface.PrettyPrint("Resetting configurations", Colors.Purple);
                Console.WriteLine("\n\n\n\n\n");
                throw new RestartException();

            case "edit":
                while (true)
                {
                    Configuration config = Interface.Configuration;
                    string[] configKeys = (string[])config.JsonProperty();

                    Interface.PrettyPrint($"Edit configurations (Bypass error checking)\nType 'cancel' to cancel", Colors.Purple);
                    foreach (string configKey in configKeys)
                        Interface.PrettyPrint($"- {configKey}", Colors.Purple);

                    string key = Interface.GetUserInput("Configuration: ");

                    if (string.IsNullOrWhiteSpace(key))
                    {
                        Interface.PrettyPrint("Invalid Key", Colors.Fail);
                        return;
                    }

                    if (!configKeys.Contains(key))
                    {
                        if (key.Equals("cancel", StringComparison.CurrentCultureIgnoreCase))
                            return;

                        Interface.PrettyPrint("Invalid Key", Colors.Fail);
                        continue;
                    }
                    else
                    {
                        Interface.PrettyPrint($"Current {key}: {config.JsonProperty(key)[0]}", Colors.Yellow);
                        string newValue = Interface.GetUserInput("New Value: ");
                        _ = config.JsonProperty(key, newValue);
                        config.Save();
                    }
                    return;
                }

            default:
                throw new ArgumentException($"Invalid argument '{args[0]}'");
        }
    }

    private static readonly ManualResetEvent ResetEvent = new(false);

    /// <summary>
    /// Starts automatic compilation with a specified interval. Press Enter to stop the compilation.
    /// </summary>
    /// <param name="args">Command arguments.</param>
    [AddCommand("autocompile", "autocompile <interval (second)>", "Start automatically compiling with certain interval (Press Enter to stop)")]
    public void AutoCompileCommand(string[] args)
    {
        if (args.Length != 1)
        {
            Interface.PrettyPrint("Invalid number of arguments. Usage: autocompile <interval (second)>", Colors.Fail);
            return;
        }

        if (!int.TryParse(args[0], out int intervalSeconds))
        {
            Interface.PrettyPrint("Invalid integer for interval", Colors.Fail);
            return;
        }

        if (intervalSeconds == 0)
        {
            Interface.PrettyPrint("Interval cannot be 0 seconds", Colors.Fail);
            return;
        }

        if (!Interface.Configuration.HasConfig)
        {
            Interface.PrettyPrint("Configuration not found. Please use 'config' command to set up workspace.", Colors.Fail);
            return;
        }

        // Create a thread to run the background compilation.
        Thread thread = new(() =>
        {
            while (!ResetEvent.WaitOne(TimeSpan.FromSeconds(intervalSeconds)))
            {
                try
                {
                    Interface.Logger.Log("Auto compiling", LogLevel.Debug);
                    CompileCommand([]);
                }
                catch (Exception ex)
                {
                    Interface.ReportError(ex);
                }
            }
        })
        {
            IsBackground = true
        };

        ResetEvent.Reset();
        thread.Start();
        Interface.GetUserConfirmation("Press Enter to stop...\n");
        Interface.PrettyPrint("\nStopping...", Colors.Info);
        ResetEvent.Set();
        thread.Join();
    }

    /// <summary>
    /// Handles the 'cd' command, allowing users to change the current directory.
    /// </summary>
    /// <param name="args">Command arguments, specifying the new directory path.</param>
    [AddCommand("cd", "cd <path>", "Change current directory")]
    public void ChangeDirectoryCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Interface.PrettyPrint($"Invalid number of arguments. Usage: cd <path>", Colors.Fail);
            return;
        }

        string path = args.Length > 1 ? string.Join(' ', args) : args[0];

        try
        {
            string dir = Path.GetFullPath(path);
            Directory.SetCurrentDirectory(dir);
            Interface.Logger.SetLogPathToCurrentDirectory();
        }
        catch (Exception ex)
        {
            Interface.ReportError(ex);
        }

        throw new RestartException();
    }

    /// <summary>
    /// Clears the console screen.
    /// </summary>
    /// <param name="args">Command arguments (not used in this command).</param>
    [AddCommand("cls", "cls", "Clear the console")]
    public void ClearConsoleCommand(string[] args)
    {
        Console.Clear();
    }
}
