using System.Reflection;
using JMC.Logging;

namespace JMC.Terminal;
internal static class CommandManager
{
    /// <summary>
    /// Gets a dictionary of CLI commands with command names as keys and tuples containing:
    /// - Action to execute the CLI command.
    /// - Usage information for the CLI command.
    /// - Description of the CLI command.
    /// </summary>
    internal static readonly
        Dictionary<string, (Action<string[]> command, string usage, string description)>
        CLICommands = InitCLICommands();

    /// <summary>
    /// Gets a dictionary of terminal commands with command names as keys and tuples containing:
    /// - Action to execute the terminal command.
    /// - Usage information for the terminal command.
    /// - Description of the terminal command.
    /// </summary>
    internal static readonly
        Dictionary<string, (Action<string[]> command, string usage, string description)>
        TerminalCommands = InitTerminalCommands();

    /// <summary>
    /// Initializes and returns a dictionary of CLI commands from methods marked with the AddCommandAttribute.
    /// </summary>
    /// <returns>
    /// A dictionary where the key is the command name, and the value is a tuple containing:
    /// - Action to execute the CLI command.
    /// - Usage information for the CLI command.
    /// - Description of the CLI command.
    /// </returns>
    private static Dictionary<string, (Action<string[]> command, string usage, string description)> InitCLICommands()
    {
        // Get all public instance methods of the CLICommands class
        // that have the AddCommandAttribute applied.
        IEnumerable<MethodInfo> methodsWithAttribute = typeof(Terminal.CLICommands)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => Attribute.IsDefined(method, typeof(AddCommandAttribute)));

        CLICommands Commands = new();
        Dictionary<string, (Action<string[]>, string, string)> commandDictionary = [];

        foreach (MethodInfo method in methodsWithAttribute)
        {
            AddCommandAttribute attribute = (AddCommandAttribute)Attribute.GetCustomAttribute(method, typeof(AddCommandAttribute))!;

            string usage = attribute.Usage;
            string name = attribute.Command ?? method.Name;
            string desc = attribute.Description;

            Action<string[]> actionDelegate = (Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), Commands, method);

            if (commandDictionary.ContainsKey(name))
            {
                Interface.Logger.Log($"Duplicate key found: {name}", LogLevel.Warn);
                continue;
            }

            Interface.Logger.Log($"CLI Command added: {method.Name} as {name}", LogLevel.Debug);
            Interface.Logger.Log($"CLI Command: {name}, Usage: {usage}", LogLevel.Debug);

            commandDictionary.Add(name, (actionDelegate, usage, desc));
        }

        return commandDictionary;
    }

    /// <summary>
    /// Initializes and returns a dictionary of terminal commands from methods marked with the AddCommandAttribute.
    /// </summary>
    /// <returns>
    /// A dictionary where the key is the command name, and the value is a tuple containing:
    /// - Action to execute the command.
    /// - Usage information for the command.
    /// - Description of the command.
    /// </returns>
    private static Dictionary<string, (Action<string[]>, string, string)> InitTerminalCommands()
    {
        // Get all public instance methods of the TerminalCommands class
        // that have the AddCommandAttribute applied.
        IEnumerable<MethodInfo> methodsWithAttribute = typeof(Terminal.TerminalCommands)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => Attribute.IsDefined(method, typeof(AddCommandAttribute)));

        TerminalCommands Commands = new();
        Dictionary<string, (Action<string[]>, string, string)> commandDictionary = [];

        foreach (MethodInfo method in methodsWithAttribute)
        {
            AddCommandAttribute attribute = (AddCommandAttribute)Attribute.GetCustomAttribute(method, typeof(AddCommandAttribute))!;

            string usage = attribute.Usage;
            string name = attribute.Command ?? method.Name;
            string desc = attribute.Description;

            Action<string[]> actionDelegate = (Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), Commands, method);

            if (commandDictionary.ContainsKey(name))
            {
                Interface.Logger.Log($"Duplicate key found: {name}", LogLevel.Warn);
                continue;
            }

            Interface.Logger.Log($"Command added: {method.Name} as {name}", LogLevel.Debug);
            Interface.Logger.Log($"Command: {name}, Usage: {usage}", LogLevel.Debug);

            commandDictionary.Add(name, (actionDelegate, usage, desc));
        }

        return commandDictionary;
    }
}