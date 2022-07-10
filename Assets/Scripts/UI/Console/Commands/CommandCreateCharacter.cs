using static viva.console.DevConsole;
using UnityEngine;

namespace viva.console
{
    public class CommandCreateCharacter : ConsoleCommand
    {
        public sealed override string Name { get; protected set; }
        public sealed override string Command { get; protected set; }
        public sealed override string Description { get; protected set; }
        public sealed override string Help { get; protected set; }
        public sealed override string Example { get; protected set; }

        public CommandCreateCharacter()
        {
            Name = "Create Character";
            Command = "createcharacter";
            Description = "Creates Loli's";
            Help = "Syntax: createcharacter <card name> \n" +
                   $"<color={RequiredColor}><card name></color> is required!";
            Example = "createcharacter shinobu, createcharacter kyaru";

            AddCommandToConsole();
        }

        public override void RunCommand(string[] data)
        {
            if (data.Length == 2)
            {
                var commandParameter = data[1];
                if (string.IsNullOrWhiteSpace(commandParameter))
                {
                    AddStaticMessageToConsole(ParametersAmount);
                }
                Vector3 spawnPos = GameDirector.player.head.transform.position + GameDirector.player.head.transform.forward*2;
                var cardsavailable = ModelCustomizer.main.characterCardBrowser.FindAllExistingCardsInFolders();
                GameDirector.instance.town.BuildTownLolis(new string[] { commandParameter }, 1, spawnPos);
            }
            else
            {
                AddStaticMessageToConsole(ParametersAmount);
            }
        }

        public static CommandCreateCharacter CreateCommand()
        {
            return new CommandCreateCharacter();
        }
    }
}

