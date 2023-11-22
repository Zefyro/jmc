using System.Reflection;
using JMC.Logging;

namespace JMC.Terminal;
internal static class CommandManager
{
    /// <summary>
    /// A dictionary containing terminal commands, where the key is the command name,
    /// and the value is a tuple consisting of the command action delegate, usage information, and a description.
    /// The dictionary is initialized by calling the <see cref="InitializeCommands"/> method.
    /// </summary>
    internal static readonly Dictionary<string, (Action<string[]> command, string usage, string description)> Dictionary = InitializeCommands();
    private static readonly TerminalCommands Commands = new();
    private static Dictionary<string, (Action<string[]>, string, string)> InitializeCommands()
    {
        // Get all public instance methods of the TerminalCommands class
        // that have the AddCommandAttribute applied.
        IEnumerable<MethodInfo> methodsWithAttribute = typeof(TerminalCommands)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => Attribute.IsDefined(method, typeof(AddCommandAttribute)));

        // Dictionary to store terminal commands.
        Dictionary<string, (Action<string[]>, string, string)> commandDictionary = [];

        // Iterate over methods with the AddCommandAttribute.
        foreach (MethodInfo method in methodsWithAttribute)
        {
            // Get the AddCommandAttribute applied to the method.
            AddCommandAttribute attribute = (AddCommandAttribute)Attribute.GetCustomAttribute(method, typeof(AddCommandAttribute))!;

            // Extract information from the attribute.
            string usage = attribute.Usage;
            string name = attribute.Command ?? method.Name;
            string desc = attribute.Description;

            // Create an action delegate for the method.
            Action<string[]> actionDelegate = (Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), Commands, method);

            // Check if the key already exists before adding
            if (commandDictionary.ContainsKey(name))
            {
                // Log a warning for duplicate keys and skip the command.
                Interface.Logger.Log($"Duplicate key found: {name}", LogLevel.Warn);
                continue;
            }

            // Log that a terminal command has been added.
            Interface.Logger.Log($"Terminal command added: {method.Name} as {name}", LogLevel.Debug);
            Interface.Logger.Log($"Command: {name}, Usage: {usage}", LogLevel.Debug);
            
            // Add the command to the dictionary.
            commandDictionary.Add(name, (actionDelegate, usage, desc));
        }

        return commandDictionary;
    }
}