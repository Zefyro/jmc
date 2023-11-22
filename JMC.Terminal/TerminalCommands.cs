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
        // Check if no arguments are provided.
        if (args.Length == 0)
        {
            // Build a string of available commands and their descriptions.
            string availableCommands = "\n";
            foreach (KeyValuePair<string, (Action<string[]> command, string usage, string description)> command in CommandManager.Dictionary)
            {
                availableCommands += $"- {command.Value.usage}: {command.Value.description}\n";
            }

            // Display the available commands to the user.
            Interface.PrettyPrint($"Available commands:{availableCommands}", Colors.Yellow);
            return;
        }

        // Check if the provided command exists in the dictionary.
        if (!CommandManager.Dictionary.TryGetValue(args[0], out var value))
        {
            // Display an error message for an unrecognized command.
            Interface.PrettyPrint($"Unrecognized command '{args[0]}'", Colors.Fail);
            return;
        }

        // Display detailed help for the specified command.
        Interface.PrettyPrint($"{value.usage}: {value.description}", Colors.Yellow);
    }

    /// <summary>
    /// Exits the JMC compiler.
    /// </summary>
    /// <param name="args">Command arguments (not used in this command).</param>
    [AddCommand("exit", "exit", "Exit JMC compiler")]
    public void ExitCommand(string[] args)
    {
        // Terminate the application with exit code 0.
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
        // Flag to determine if debug mode is enabled
        bool debugCompile = false;

        // Check if there are arguments provided
        if (args.Length != 0)
        {
            // Check if the argument is "debug"
            if (args[0] != "debug")
                throw new ArgumentException($"Unrecognized argument '{args[0]}'");

            // Indicate that debug mode is enabled
            Interface.PrettyPrint("DEBUG MODE", Colors.Info);
            debugCompile = args[0] == "debug";
        }

        // Print a message indicating that compilation is in progress
        Interface.PrettyPrint("Compiling...", Colors.Info);

        // Check if configuration is available
        if (!Interface.Configuration.HasConfig)
        {
            // Initialize configuration if not available
            Interface.Configuration.Initialize([]);
            return;
        }

        try
        {
            // Measure compilation time using a Stopwatch
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Call the Compile method and measure build time
            double buildTime = double.Round(Interface.Compile() / 1000, 5);

            // Stop the stopwatch and calculate total elapsed compile time
            stopwatch.Stop();
            double elapsedSeconds = double.Round(stopwatch.Elapsed.TotalMilliseconds - buildTime / 1000, 5);

            // Print a message indicating successful compilation and build time
            Interface.PrettyPrint($"Compiled successfully in {elapsedSeconds} seconds, datapack built in {buildTime} seconds.", Colors.Info);
        }
        catch (Exception ex)
        {
            // Check if the exception type starts with "JMC." to distinguish JMC-specific errors
            if (ex.GetType().ToString().StartsWith("JMC."))
            {
                // Log the exception, report the error, and continue
                Interface.Logger.Log(ex.ToString(), LogLevel.Debug);
                Interface.ReportError(ex);
            }
            else
            {
                // Log non-JMC errors, handle the exception, and inform the user
                Interface.Logger.Log("Non-JMC Error occur", LogLevel.Fatal);
                Interface.HandleException(ex, isOk: false); ;
            }
        }

        // If debug mode is enabled, dump additional debugging information
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
        // Check if no arguments are provided.
        if (args.Length == 0)
        {
            Interface.PrettyPrint($"Unrecognized command: No arguments provided.", Colors.Fail);
            return;
        }

        // Check for invalid argument or too many arguments.
        if (args.Length > 1 || !LogArgs.Contains(args[0]))
        {
            Interface.PrettyPrint($"Invalid arguments. Usage: log debug|info|clear", Colors.Fail);
            return;
        }

        // Switch based on the provided log command.
        switch (args[0])
        {
            // Case: Create a log file with debug information.
            case "debug":
                Interface.Logger.Dump(LogLevel.Debug);
                return;

            // Case: Create a log file with info-level information.
            case "info":
                Interface.Logger.Dump(LogLevel.Info);
                return;

            // Case: Delete all old log files in the log folder.
            case "clear":
                Interface.Logger.DeleteOldGzipFiles();
                return;

            // Default case: Throw an exception for an invalid argument.
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
        // Display JMC's version information to the user.
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
        // Check if no arguments are provided.
        if (args.Length == 0)
        {
            // If no arguments are provided and no configuration exists, initialize a new configuration.
            if (!Interface.Configuration.HasConfig)
            {
                Interface.Initialize([]);
                return;
            }

            // If a configuration file already exists, notify the user and provide instructions for resetting.
            Interface.PrettyPrint("Configuration file is already generated. To reset, use `config reset`", Colors.Fail);
            return;
        }

        // Check for invalid argument or too many arguments.
        if (args.Length > 1 || !ConfigArgs.Contains(args[0]))
        {
            Interface.PrettyPrint($"Invalid number of arguments. Usage: config [edit|reset]", Colors.Fail);
            return;
        }

        // Process the specified configuration command.
        switch (args[0])
        {
            // Case: Delete old configuration file and crash.
            case "reset":
                // Delete the configuration file from the current directory.
                File.Delete(Path.GetFullPath(Configuration.FileName));

                // Log a message indicating the reset of configurations.
                Interface.Logger.Log("Resetting configurations");

                // Print a formatted message about resetting configurations in the console.
                Interface.PrettyPrint("Resetting configurations", Colors.Purple);
                Console.WriteLine("\n\n\n\n\n");

                // Restart the compiler by throwing an exception.
                throw new Exceptions.RestartException();

            // Case: Edit the existing configurations and save.
            case "edit":
                while (true)
                {
                    // Retrieve the current configuration and obtain a list of configuration keys.
                    Configuration config = Interface.Configuration;
                    string[] configKeys = (string[])config.JsonProperty();

                    // Display available configurations
                    Interface.PrettyPrint($"Edit configurations (Bypass error checking)\nType 'cancel' to cancel", Colors.Purple);
                    foreach (string configKey in configKeys)
                        Interface.PrettyPrint($"- {configKey}", Colors.Purple);

                    // Get user input for configuration key
                    string key = Interface.GetUserInput("Configuration: ");

                    if (string.IsNullOrWhiteSpace(key))
                    {
                        // Handle cancellation
                        Interface.PrettyPrint("Invalid Key", Colors.Fail);
                        return;
                    }

                    if (!configKeys.Contains(key))
                    {
                        // Handle invalid key
                        if (key.Equals("cancel", StringComparison.CurrentCultureIgnoreCase))
                            return;

                        Interface.PrettyPrint("Invalid Key", Colors.Fail);
                        continue;
                    }
                    else
                    {
                        // Display current value of the configuration
                        Interface.PrettyPrint($"Current {key}: {config.JsonProperty(key)[0]}", Colors.Yellow);

                        // Get user input for new value
                        string newValue = Interface.GetUserInput("New Value: ");

                        // Update the configuration and save to file
                        _ = config.JsonProperty(key, newValue);
                        config.Save();
                    }
                    return;
                }

            // Default case: Throw an exception for an invalid argument.
            default:
                throw new ArgumentException($"Invalid argument '{args[0]}'");
        }
    }

    // ManualResetEvent to control background thread
    private static readonly ManualResetEvent ResetEvent = new(false);

    [AddCommand("autocompile", "autocompile <interval (second)>", "Start automatically compiling with certain interval (Press Enter to stop)")]
    public void AutoCompileCommand(string[] args)
    {
        // Check if the required number of arguments is provided.
        if (args.Length != 1)
        {
            Interface.PrettyPrint("Invalid number of arguments. Usage: autocompile <interval (second)>", Colors.Fail);
            return;
        }

        // Attempt to parse the interval argument to an integer.
        if (!int.TryParse(args[0], out int intervalSeconds))
        {
            Interface.PrettyPrint("Invalid integer for interval", Colors.Fail);
            return;
        }

        // Check if the interval is 0 seconds.
        if (intervalSeconds == 0)
        {
            Interface.PrettyPrint("Interval cannot be 0 seconds", Colors.Fail);
            return;
        }

        // Check if the configuration is available.
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
                    // Handle and report any exceptions that occur during background compilation.
                    Interface.ReportError(ex);
                }
            }
        })
        {
            IsBackground = true
        };

        // Set the ManualResetEvent to non-signaled state.
        ResetEvent.Reset();

        // Start the background thread.
        thread.Start();

        // Wait for the user to press Enter to stop.
        Interface.GetUserConfirmation("Press Enter to stop...\n");

        // Print a message indicating stopping.
        Interface.PrettyPrint("\nStopping...", Colors.Info);

        // Set the ManualResetEvent to signaled state.
        ResetEvent.Set();

        // Wait for the background thread to complete.
        thread.Join();
    }

    /// <summary>
    /// Handles the 'cd' command, allowing users to change the current directory.
    /// </summary>
    /// <param name="args">Command arguments, specifying the new directory path.</param>
    [AddCommand("cd", "cd <path>", "Change current directory")]
    public void ChangeDirectoryCommand(string[] args)
    {
        // Check if no arguments are provided.
        if (args.Length == 0)
        {
            Interface.PrettyPrint($"Invalid number of arguments. Usage: cd <path>", Colors.Fail);
            return;
        }

        // Combine all arguments to form the path.
        string path = args.Length > 1 ? string.Join(' ', args) : args[0];

        try
        {
            // Get the full path of the specified directory.
            string dir = Path.GetFullPath(path);

            // Change the current directory.
            Directory.SetCurrentDirectory(dir);

            // Set the log path to the current directory.
            Interface.Logger.SetLogPathToCurrentDirectory();
        }
        catch (Exception ex)
        {
            // Handle and report any exceptions that occur during the directory change.
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
