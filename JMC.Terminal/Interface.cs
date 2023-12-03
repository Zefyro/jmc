using JMC.Exceptions;
using JMC.Logging;
using System.Runtime.CompilerServices;

namespace JMC.Terminal;
public static class Interface
{
    internal static Logger Logger = new();
    internal static Configuration Configuration = new();

    /// <summary>
    /// Initializes the JMC compiler by creating a new Configuration instance and invoking its initialization process.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static void Initialize(string[] args)
    {
        Configuration.Initialize(args);
    }

    /// <summary>
    /// Starts the JMC compiler, displaying version information, current directory, and providing guidance based on the presence of a configuration file.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static void Run(string[] args)
    {
        if (args.Length == 0)
        {
            Start([]);
            return;
        }

        if (args[0] == "-h" || args[0] == "--help")
        {
            Console.WriteLine("Help");
            return;
        }

        if (args[0] == "-v" || args[0] == "--version")
        {
            PrettyPrint(Configuration.Version, Colors.Info);
            return;
        }

        string commandName = args[0];
        string[] arguments = args.Length > 1 ? args[1..] : [];

        if (!CommandManager.CLICommands.TryGetValue(commandName, out var commandInfo))
        {
            Start([]);
            return;
        }

        try
        {
            // Execute the command using the specified arguments.
            commandInfo.command(arguments);
        }
        // Handle any Exception thrown during command execution.
        catch (Exception ex)
        {
            string errorMessage = ex.Message;

            PrettyPrint($"Usage: {commandInfo.usage}", Colors.Info);
            PrettyPrint(errorMessage, Colors.Fail);
        }
    }

    internal static double Compile()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Starts the JMC Compiler, displaying version information, current directory, and handling user commands in a continuous loop.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the program.</param>
    internal static void Start(string[] args)
    {
        while (true)
        {
            try
            {
                PrettyPrint($" JMC Compiler {Configuration.Version}\n", Colors.Header);
                PrettyPrint($"Current Directory | {Path.GetFullPath(".")}\n", Colors.Yellow);

                Configuration.Load(args);

                if (Configuration.HasConfig)
                    PrettyPrint("To compile, type `compile`. For help, type `help`", Colors.Info);
                else
                    PrettyPrint("To setup workspace, type `config`. For help, type `help`", Colors.Info);

                while (true)
                    HandleCommand(GetUserInput());
            }
            catch (Exception ex)
            {
                if (ex is RestartException)
                    continue;

                HandleException(ex, true);

                break;
            }
        }
    }

    /// <summary>
    /// Outputs a formatted message to the console with an optional specified text color.
    /// </summary>
    /// <param name="value">The message to be displayed.</param>
    /// <param name="color">The optional text color (default is Colors.None).</param>
    internal static void PrettyPrint(string value, Colors color = Colors.None)
    {
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = (ConsoleColor)color;
        Console.WriteLine(value);
        Console.ForegroundColor = oldColor;
    }

    /// <summary>
    /// Reports an error by displaying the type of the exception and its string representation in the console.
    /// </summary>
    /// <param name="ex">The exception to report.</param>
    internal static void ReportError(Exception ex)
    {
        PrettyPrint(ex.GetType().ToString(), Colors.Error);
        PrettyPrint(ex.Message, Colors.Fail);
    }

    /// <summary>
    /// Handles unexpected exceptions, informs the user, logs the error, and requests user confirmation to continue.
    /// </summary>
    /// <param name="error">The exception that occurred.</param>
    /// <param name="isOk">A flag indicating whether the error is considered normal (true) or exceptional (false).</param>
    internal static void HandleException(Exception error, bool isOk)
    {
        PrettyPrint("Unexpected error causes program to crash", Colors.Fail);

        PrettyPrint(error.GetType().Name, Colors.Error);
        PrettyPrint(error.Message, Colors.Fail);

        if (!isOk)
        {
            PrettyPrint(error.ToString(), Colors.Error);
            PrettyPrint("Please report this error at https://github.com/WingedSeal/jmc/issues/new/choose or https://discord.gg/PNWKpwdzD3.");
        }

        Logger.Log("Program crashed", LogLevel.Critical);
        GetUserConfirmation("Press Enter to continue...");
    }

    /// <summary>
    /// Displays a prompt, waits for the user to press the Enter key, and optionally sets the text color.
    /// </summary>
    /// <param name="prompt">The message to display as a prompt.</param>
    /// <param name="color">The text color to use for the prompt (default is Colors.Input).</param>
    internal static void GetUserConfirmation(string prompt, Colors color = Colors.Input)
    {
        PrettyPrint(prompt, color);
        do { } while (Console.ReadKey().Key != ConsoleKey.Enter);
    }

    /// <summary>
    /// Retrieves input from the user with an optional prompt and specified text color.
    /// </summary>
    /// <param name="prompt">The optional prompt to display (default is "> ").</param>
    /// <param name="color">The optional text color for the prompt (default is Colors.Input).</param>
    /// <returns>The user-entered input as a string.</returns>
    internal static string GetUserInput(string prompt = "> ", Colors color = Colors.Input)
    {
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = (ConsoleColor)color;
        Console.Write(prompt);
        string? line = Console.ReadLine();
        Console.ForegroundColor = oldColor;

        if (line is not null)
            Logger.Log($"Input from user: {line}");

        return line ?? string.Empty;
    }

    /// <summary>
    /// Processes and executes a command with its arguments.
    /// </summary>
    /// <param name="command">The full command string to be processed.</param>
    internal static void HandleCommand(string command)
    {
        if (string.IsNullOrEmpty(command))
            return;

        // Split the command into parts, separating command name and arguments.
        string[] commandParts = command.Split(' ');
        string commandName = commandParts[0];
        string[] arguments = commandParts.Length > 1 ? commandParts[1..] : [];

        if (!CommandManager.TerminalCommands.TryGetValue(commandName, out var commandInfo))
        {
            PrettyPrint("Command not recognized, try `help` for more info.", Colors.Fail);
            return;
        }

        try
        {
            // Execute the command using the specified arguments.
            commandInfo.command(arguments);
        }
        // Handle any Exception thrown during command execution.
        catch (Exception ex)
        {
            string errorMessage = ex.Message;
            errorMessage = errorMessage.Replace("()", " command").Replace("positional argument", "argument");

            PrettyPrint(errorMessage, Colors.Fail);
            PrettyPrint($"Usage: {commandInfo.usage}", Colors.Info);
        }
    }

    /// <summary>
    /// Creates a file at the specified path, including necessary directories.
    /// </summary>
    /// <param name="filePath">The path of the file to be created.</param>
    internal static void CreateFile(this string filePath)
    {
        try
        {
            Directory.CreateDirectory(
                string.IsNullOrEmpty(Path.GetDirectoryName(filePath))
                    ? Directory.GetCurrentDirectory()
                    : Path.GetDirectoryName(filePath)!);

            FileStream fs = File.Create(filePath);
            fs.Close();
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }
}