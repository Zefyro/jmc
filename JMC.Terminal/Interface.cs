using JMC.Exceptions;
using JMC.Logging;

namespace JMC.Terminal;
public sealed class Interface
{
    internal static Logger Logger = new();
    internal static Configuration Configuration = new();

    /// <summary>
    /// Initializes the JMC compiler by creating a new Configuration instance and invoking its initialization process.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static void Initialize(string[] args)
    {
        Configuration = new();
        Configuration.Initialize(args);
    }

    /// <summary>
    /// Starts the JMC compiler, displaying version information, current directory, and providing guidance based on the presence of a configuration file.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static void Run(string[] args)
    {
        while (true)
        {
            try
            {
                Start(args);
            }
            catch (Exception ex)
            {
                if (ex is RestartException)
                    continue;
                break;
            }
        }
    }

    internal static double Compile()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Initiates the JMC Compiler, displays version information, loads configuration settings,
    /// and handles user commands in a continuous loop.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the JMC Compiler.</param>
    private static void Start(string[] args)
    {
        PrettyPrint($" JMC Compiler {Configuration.Version}\n", Colors.Header);
        PrettyPrint($"Current Directory | {Path.GetFullPath(".")}\n", Colors.Yellow);

        Configuration = new();
        Configuration.Load(args);

        if (Configuration.HasConfig)
            PrettyPrint("To compile, type `compile`. For help, type `help`", Colors.Info);
        else
            PrettyPrint("To setup workspace, type `config`. For help, type `help`", Colors.Info);

        while (true)
            HandleCommand(GetUserInput());
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
    private static void HandleCommand(string command)
    {
        if (string.IsNullOrEmpty(command)) 
            return;

        // Split the command into parts, separating command name and arguments.
        string[] commandParts = command.Split(' ');
        string commandName = commandParts[0];
        string[] arguments = commandParts.Length > 1 ? commandParts[1..] : [];

        if (!CommandManager.Dictionary.TryGetValue(commandName, out var commandInfo))
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
}