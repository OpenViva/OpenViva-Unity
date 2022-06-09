using System;
using static viva.console.DevConsole;

namespace viva.console
{
    public class CommandRagdoll : ConsoleCommand
    {
        public sealed override string Name { get; protected set; }
        public sealed override string Command { get; protected set; }
        public sealed override string Description { get; protected set; }
        public sealed override string Help { get; protected set; }
        public sealed override string Example { get; protected set; }

        public CommandRagdoll()
        {
            Name = "Ragdoll";
            Command = "ragdoll";
            Description = "Ragdoll's Selected Lolis";
            Help = "Syntax: ragdoll <muscle weights> \n" +
                   $"<color={RequiredColor}><muscle weights></color> are required!";
            Example = "ragdoll 0.5";

            AddCommandToConsole();
        }

        public override void RunCommand(string[] data)
        {

            if (data.Length == 2)
            {
                var commandParameter = data[1];
                float weight = Convert.ToSingle(commandParameter);
                if (GameDirector.player.objectFingerPointer.selectedLolis.Count > 0)
                {
                    foreach (var loli in GameDirector.player.objectFingerPointer.selectedLolis)
                    {
                        loli.BeginRagdollMode(weight, Loli.Animation.FALLING_LOOP);
                    }
                }
                else
                {
                    AddStaticMessageToConsole("No Loli's Selected");
                }
            }
            else
            {
                AddStaticMessageToConsole(ParametersAmount);
            }
        }

        public static CommandRagdoll CreateCommand()
        {
            return new CommandRagdoll();
        }
    }
}

