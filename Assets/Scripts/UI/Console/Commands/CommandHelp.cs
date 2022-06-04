using System.Collections.Generic;
using static viva.console.DevConsole;

namespace viva.console
{
    public class CommandHelp : ConsoleCommand
    {
        public sealed override string Name { get; protected set; }
        public sealed override string Command { get; protected set; }
        public sealed override string Description { get; protected set; }
        public sealed override string Help { get; protected set; }
        public sealed override string Example { get; protected set; }

        public CommandHelp()
        {
            Name = "Help";
            Command = "help";
            Description = "Returns description for specified command (or all available commands if parameter is not specified)";
            Help = "Syntax: help <command name> \n" +
                   $"<color={OptionalColor}><command name></color> is optional";
            Example = "help set";

            AddCommandToConsole();
        }

        public override void RunCommand(string[] data)
        {
            if (data.Length == 1)
            {
                AddStaticMessageToConsole("--------------------------------------------------");
                AddStaticMessageToConsole("Available commands");

                int indexCounter = 1;

                foreach (KeyValuePair<string, ConsoleCommand> command in Commands)
                {
                    AddStaticMessageToConsole(indexCounter + ") " + command.Key);

                    indexCounter++;
                }
            }
            else if (data.Length == 2 && Commands.ContainsKey(data[1]))
            {
                var parameter = data[1];

                var command = Commands[parameter];

                AddStaticMessageToConsole("--------------------------------------------------");
                AddStaticMessageToConsole("<b>Title of command</b>");
                AddStaticMessageToConsole(command.Name + "\n");
                AddStaticMessageToConsole("<b>Description</b>");
                AddStaticMessageToConsole(command.Description + "\n");
                AddStaticMessageToConsole("<b>Usage</b>");
                AddStaticMessageToConsole(command.Help + "\n");
                AddStaticMessageToConsole("<b>Example</b>");
                AddStaticMessageToConsole(command.Example + "\n");
            }
            else if (Commands.ContainsKey(data[1]) == false)
            {
                AddStaticMessageToConsole(CommandNotRecognized);
            }
            else
            {
                AddStaticMessageToConsole(ParametersAmount);
            }

        }

        public static CommandHelp CreateCommand()
        {
            return new CommandHelp();
        }
    }
}