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

        // Initialize the configuration, which may involve user input, and save to a configuration file.
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

    private static void Start(string[] args)
    {
        // Display JMC Compiler version and current directory information.
        PrettyPrint($" JMC Compiler {Configuration.Version}\n", Colors.Header);
        PrettyPrint($"Current Directory | {Path.GetFullPath(".")}\n", Colors.Yellow);

        // Load configuration settings.
        Configuration = new();
        Configuration.Load(args);

        // Display instructions based on the presence of a configuration file.
        if (Configuration.HasConfig)
            PrettyPrint("To compile, type `compile`. For help, type `help`", Colors.Info);
        else
            PrettyPrint("To setup workspace, type `config`. For help, type `help`", Colors.Info);

        // Continuously handle user commands in a loop.
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
        // Print a message indicating that an unexpected error caused the program to crash
        PrettyPrint("Unexpected error causes program to crash", Colors.Fail);

        // Print the type and message of the exception
        PrettyPrint(error.GetType().Name, Colors.Error);
        PrettyPrint(error.Message, Colors.Fail);

        // If the error is not considered normal, print the full exception details and a reporting message
        if (!isOk)
        {
            PrettyPrint(error.ToString(), Colors.Error);
            PrettyPrint("Please report this error at https://github.com/WingedSeal/jmc/issues/new/choose or https://discord.gg/PNWKpwdzD3.");
        }

        // Log a critical message indicating that the program crashed
        Logger.Log("Program crashed", LogLevel.Critical);

        // Request user confirmation to continue, if needed
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
        // Store the current console text color and
        // set the console text color to the specified color
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = (ConsoleColor)color;

        // Display the prompt and retrieve the user-entered line.
        Console.Write(prompt);
        string? line = Console.ReadLine();

        // Restore the original console text color.
        Console.ForegroundColor = oldColor;

        // Log the user input.
        if (line is not null)
            Logger.Log($"Input from user: {line}");

        // Return the user-entered input (or an empty string if null).
        return line ?? string.Empty;
    }

    private static void HandleCommand(string command)
    {
        // Check if the given command is null or empty.
        if (string.IsNullOrEmpty(command)) 
            return;

        // Split the command into parts, separating command name and arguments.
        string[] commandParts = command.Split(' ');
        string commandName = commandParts[0];
        string[] arguments = commandParts.Length > 1 ? commandParts[1..] : [];

        // Attempt to retrieve the command information from the dictionary.
        if (!CommandManager.Dictionary.TryGetValue(commandName, out var commandInfo))
        {
            // Display an error message for unrecognized commands.
            PrettyPrint("Command not recognized, try `help` for more info.", Colors.Fail);
            return;
        }

        try
        {
            // Execute the command using the specified arguments.
            commandInfo.command(arguments);
        }
        catch (ArgumentException ex)
        {
            // Handle any ArgumentException thrown during command execution.
            string errorMessage = ex.Message;
            errorMessage = errorMessage.Replace("()", " command").Replace("positional argument", "argument");

            // Display the error message and usage information.
            Console.WriteLine(errorMessage);
            Console.WriteLine($"Usage: {commandInfo.usage}");
        }
    }
}