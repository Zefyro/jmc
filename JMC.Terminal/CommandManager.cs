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

            Interface.Logger.Log($"Terminal command added: {method.Name} as {name}", LogLevel.Debug);
            Interface.Logger.Log($"Command: {name}, Usage: {usage}", LogLevel.Debug);
            
            commandDictionary.Add(name, (actionDelegate, usage, desc));
        }

        return commandDictionary;
    }
}