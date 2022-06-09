using static viva.console.DevConsole;

namespace viva.console
{
    public class CommandSelectCharacter : ConsoleCommand
    {
        public sealed override string Name { get; protected set; }
        public sealed override string Command { get; protected set; }
        public sealed override string Description { get; protected set; }
        public sealed override string Help { get; protected set; }
        public sealed override string Example { get; protected set; }

        public CommandSelectCharacter()
        {
            Name = "Select Character";
            Command = "selected";
            Description = "Selects/Deselects Loli's";
            Help = "Syntax: selected <all/none> \n" +
                   $"<color={RequiredColor}><all/none></color> is required!";
            Example = "selected all, selected none";

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
                if (commandParameter.Contains("all"))
                {
                    Loli loli = GameDirector.instance.FindNearbyLoli(GameDirector.player.head.position, 500.0f);
                    if (!GameDirector.player.objectFingerPointer.selectedLolis.Contains(loli))
                    {
                        loli.characterSelectionTarget.OnSelected();
                        GameDirector.player.objectFingerPointer.selectedLolis.Add(loli);
                    }

                    AddStaticMessageToConsole("Selected All Loli's");
                }
                else if (commandParameter.Contains("none"))
                {
                    Loli loli = GameDirector.instance.FindNearbyLoli(GameDirector.player.head.position, 500.0f);
                    loli.characterSelectionTarget.OnUnselected();
                    GameDirector.player.objectFingerPointer.selectedLolis.Remove(loli);
                    AddStaticMessageToConsole("Deselected All Loli's");
                }
                else
                {
                    AddStaticMessageToConsole(ArgumentNotRecognized);
                }
            }
            else
            {
                AddStaticMessageToConsole(ParametersAmount);
            }
        }

        public static CommandSelectCharacter CreateCommand()
        {
            return new CommandSelectCharacter();
        }
    }
}

