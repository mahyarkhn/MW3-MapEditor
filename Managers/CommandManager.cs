using InfinityScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapEdit.Core.Reflection;
using MapEdit.Misc;
using System.Reflection;

namespace MapEdit.Managers
{
    public class CommandManager
    {
        public Commands Commands;
        public CommandManager()
        {
            Commands = new Commands();

            AddCommand("/help", (sender, args) =>
            {
                if (args.Length < 2)
                {
                    var commands = Commands.CommandList.Where(x => x.Active).ToList();
                    Main.Instance.Common.RawSayToCondense(sender, commands.Select(x => x.Name).ToArray(), 2000);
                }
                else
                {
                    var commands = Commands.CommandList.Where(x => x.Active && x.Name.ToLower().Contains(args[1].ToLower())).ToList();
                    if(commands.Count == 0)
                    {
                        Main.Instance.Common.RawSayTo(sender, "^1Command not found");
                    }
                    if (commands.Count > 1)
                    {
                        Main.Instance.Common.RawSayTo(sender, "^5More than one command exists:");
                        Main.Instance.Common.RawSayToCondense(sender, commands.Select(x => x.Name).ToArray(), 2000);
                        return;
                    }

                    Main.Instance.Common.RawSayTo(sender, "^5Command: " + commands.First().Name);
                    Main.Instance.Common.RawSayTo(sender, "^5Usage: " + commands.First().Usage?? "^1No Usage Exists");
                }
            }, "/help [command]");

            AddCommand("removeedit", (sender, args) =>
            {
                if(args.Length < 2)
                {
                    Main.Instance.Common.RawSayTo(sender, "^1Usage: !removeedit [<id> | last]");
                    return;
                }

                if(args[1] == "last")
                {
                    Main.Instance.ScriptModel.Remove(Main.Instance.ScriptModel.ModelsList.Last());
                    Main.Instance.Common.RawSayTo(sender, "^2Last edit has been removed");
                }
                else
                {
                    if(int.TryParse(args[1], out int id))
                    {
                        Main.Instance.ScriptModel.Remove(id);
                        Main.Instance.Common.RawSayTo(sender, "^2Edit with id " + id + " has been removed");
                    }
                    else
                    {
                        Main.Instance.Common.RawSayTo(sender, "^1Usage: !removeedit [<id> | last]");
                    }
                }
            }, "re [<id> | last]", new List<string> { "re" });

            AddCommand("addadmin", (sender, args) =>
            {
                if (args.Length < 2)
                {
                    Main.Instance.Common.RawSayTo(sender, "^1Usage: !add <target>");
                    return;
                }

                Entity target;
                var foundTargets = Main.Instance.Script.Players.Where(x => x.Name.ToLower().Contains(args[1])).ToList();
                if(foundTargets.Count != 1)
                {
                    Main.Instance.Common.RawSayTo(sender, "^1No or more players found");
                    return;
                }

                target = foundTargets.First();

                if (Main.Instance.ScriptAdmins.Exists(target.HWID))
                {
                    Main.Instance.Common.RawSayTo(sender, "^1Player is already in admins list");
                    return;
                }
                
                Dictionary<string, string> param = new Dictionary<string, string>();
                if (args.Length > 2)
                {
                    var parameters = args.Skip(2);
                    foreach (var str in parameters)
                        param[str.Split(':')[0]] = str.Split(':')[1];
                }
                Main.Instance.ScriptAdmins.Add(target.Name, target.HWID, param);
                Main.Instance.Common.RawSayTo(sender, "^1Player has been added to admins");
                target.SetField("IsAdmin", "true");
                Main.Instance.Common.RawSayTo(target, "You have been added to MapEdit admins list by ^2" + sender.Name);

            }, "add <target>", new List<string> { "add" });

            AddCommand("removeadmin", (sender, args) =>
            {
                if (args.Length < 2)
                {
                    Main.Instance.Common.RawSayTo(sender, "^1Usage: !remove <target>");
                    return;
                }
                if (!Main.Instance.ScriptAdmins.Get(sender.HWID).Parameters.ContainsKey("Administrator") || Main.Instance.ScriptAdmins.Get(sender.HWID).Parameters["Administrator"] != "true")
                {
                    Main.Instance.Common.RawSayTo(sender, "^1You need Administrator perm to use this command");
                    return;
                }

                var foundAdmins = Main.Instance.ScriptAdmins.AdminsList.Where(x => x.Name.ToLower().Contains(args[1])).ToList();
                if (foundAdmins.Count != 1)
                {
                    Main.Instance.Common.RawSayTo(sender, "^1No or more admins exists");
                    return;
                }

                if (foundAdmins.First().Parameters.ContainsKey("Administrator") && foundAdmins.First().Parameters["Administrator"] == "true")
                {
                    Main.Instance.Common.RawSayTo(sender, "^1You can't remove admins with perm Administrator");
                    return;
                }
                
                Main.Instance.ScriptAdmins.Remove(foundAdmins.First());
                Main.Instance.Common.RawSayTo(sender, "^1Player has been removed from admins");

                var foundTargets = Main.Instance.Script.Players.Where(x => x.Name.ToLower().Contains(args[1])).ToList();
                if (foundTargets.Count == 1)
                {
                    foundTargets.First().SetField("IsAdmin", "false");
                    return;
                }
            }, "remove <player>", new List<string> { "remove" });

            AddCommand("/admins", (sender, args) =>
            {
                var admins = Main.Instance.Script.Players.Where(x => x.HasField("IsAdmin") && x.GetField<string>("IsAdmin") == "true").ToList();
                Main.Instance.Common.RawSayTo(sender, "^2MapEdit Online Admins:");
                Main.Instance.Common.RawSayToCondense(sender, admins.Select(x => x.Name).ToArray());
            }, "/admins");

            AddCommand("cmd", (sender, args) =>
            {
                Main.Instance.Common.RawSayTo(sender, "^7Executing: ^2" + string.Join(" ", args.Skip(1)));
                Utilities.ExecuteCommand(string.Join(" ", args.Skip(1)));
            }, "cmd <params>");

            AddCommand("version", (sender, args) =>
            {
                Main.Instance.Common.RawSayTo(sender, "^2MapEdit v" + GetType().Assembly.GetName().Version + " by Mahyar");
            }, "version");

            AddCommand("model", (sender, args) =>
            {
                if(args.Length < 2)
                {
                    Main.Instance.Common.RawSayTo(sender, "^1Usage: !model <model> [<param>:<value>]");
                    return;
                }
                ScriptModel.ModelInfo model = new ScriptModel.ModelInfo()
                {
                    Id = Main.Instance.ScriptModel.ModelsList.Count - 1,
                    Model = args[1],
                    Angles = sender.GetField<Vector3>("angles").ToString(),
                    Origin = sender.Origin.ToString(),
                    Parameters = new Dictionary<string, string>()
                    {
                        {"Type", "Model" },
                        {"Spawn-Type", "script_model" }
                    }
                };
                if(args.Length > 2)
                {
                    var parameters = args.Skip(2);
                    foreach(var str in parameters)
                        model.Parameters[str.Split(':')[0]] = str.Split(':')[1];
                }
                Main.Instance.ScriptModel.Add(model);
                Main.Instance.Common.RawSayTo(sender, "Model ^2Saved^7, Id: ^2" + model.Id);
            }, "model [Spawn-Type:<type>]");

            AddCommand("fly", (sender, args) =>
            {
                Main.Instance.Common.ToggleFly(sender);
            }, "fly");

            AddCommand("ramp", (sender, args) =>
            {
                AddEdit(sender, args, "Ramp");
            }, "ramp");
            AddCommand("hramp", (sender, args) =>
            {
                AddEdit(sender, args, "Ramp", true);
            }, "hramp");

            AddCommand("wall", (sender, args) =>
            {
                AddEdit(sender, args, "Wall");
            }, "Wall");
            AddCommand("hwall", (sender, args) =>
            {
                AddEdit(sender, args, "Wall", true);
            }, "hwall");

            AddCommand("floor", (sender, args) =>
            {
                AddEdit(sender, args, "Floor");
            }, "floor");
            AddCommand("hfloor", (sender, args) =>
            {
                AddEdit(sender, args, "Floor", true);
            }, "hfloor");

            AddCommand("tp", (sender, args) =>
            {
                AddEdit(sender, args, "Teleport");
            }, "tp");
            AddCommand("htp", (sender, args) =>
            {
                AddEdit(sender, args, "Teleport", true);
            }, "htp");

            AddCommand("elevator", (sender, args) =>
            {
                AddEdit(sender, args, "Elevator");
            }, "elevator");
            AddCommand("helevator", (sender, args) =>
            {
                AddEdit(sender, args, "Elevator", true);
            }, "helevator");

            //AddCommand("turret", (sender, args) =>
            //{
            //    ScriptModel.ModelInfo model = new ScriptModel.ModelInfo()
            //    {
            //        Id = Main.Instance.ScriptModel.ModelsList.Count - 1,
            //        Model = "Turret",
            //        Angles = new Vector3(sender.Origin.X, sender.Origin.Y, sender.Origin.Z + 50f).ToString(),
            //        Origin = sender.GetField<Vector3>("angles").ToString(),
            //        Parameters = new Dictionary<string, string>()
            //        {
            //            { "Type", "Turret" },
            //        }
            //    };
            //    if (args.Length > 1)
            //    {
            //        var parameters = args.Skip(1);
            //        foreach (var str in parameters)
            //            model.Parameters[str.Split(':')[0]] = str.Split(':')[1];
            //    }
            //    Main.Instance.ScriptModel.Add(model);
            //    Main.Instance.Common.RawSayTo(sender, "Turret ^2Saved^7, Id: ^2" + model.Id);
            //}, "turret / turret [MinigunType:<[sentry_minigun_mp/pavelow_minigun_mp]>] [MinigunModel:<[weapon_minigun/sentry_minigun]>]");

            AddCommand("door", (sender, args) =>
            {
                if ((!sender.HasField("isDrawing")) || sender.GetField<string>("isDrawing") == "false")
                {
                    sender.SetField("isDrawing", "true");
                    sender.SetField("Start", sender.Origin);
                    sender.Call("iprintlnbold", "^2Start Set");
                    Main.Instance.Common.ToggleFly(sender);
                    return;
                }
                
                ScriptModel.ModelInfo model = new ScriptModel.ModelInfo()
                {
                    Id = Main.Instance.ScriptModel.ModelsList.Count - 1,
                    Model = "Door",
                    Angles = new Vector3(90, sender.GetField<Vector3>("angles").Y, 90).ToString(),
                    Origin = "null",
                    Parameters = new Dictionary<string, string>()
                    {
                        { "Type", "Door" },
                        { "Start", sender.GetField<Vector3>("Start").ToString() },
                        { "End", sender.Origin.ToString() },
                        { "Size", "3" },
                        { "Heigth", "2" },
                        { "Health", "16" }
                    }
                };
                if (args.Length > 1)
                {
                    var parameters = args.Skip(1);
                    foreach (var str in parameters)
                        model.Parameters[str.Split(':')[0]] = str.Split(':')[1];
                }
                Main.Instance.ScriptModel.Add(model);
                Main.Instance.Common.RawSayTo(sender, "Door ^2Saved^7, Id: ^2" + model.Id);
                sender.SetField("isDrawing", "false");
                Main.Instance.Script.AfterDelay(1000, () => Main.Instance.Common.ToggleFly(sender));
            }, "door / door [Size:<size>] [Heigth:<heigth>] [Health:<health>");
        }
        private void AddEdit(Entity sender, string[] args, string type, bool hidden = false, int paramIndex = 1)
        {
            if ((!sender.HasField("isDrawing")) || sender.GetField<string>("isDrawing") == "false")
            {
                sender.SetField("isDrawing", "true");
                sender.SetField("Start", sender.Origin);
                sender.Call("iprintlnbold", "^2Start Set");
                Main.Instance.Common.ToggleFly(sender);
                return;
            }

            ScriptModel.ModelInfo model = new ScriptModel.ModelInfo()
            {
                Id = Main.Instance.ScriptModel.ModelsList.Count - 1,
                Model = type,
                Angles = "null",
                Origin = "null",
                Parameters = new Dictionary<string, string>()
                {
                    { "Type", type },
                    { "Start", sender.GetField<Vector3>("Start").ToString() },
                    { "End", sender.Origin.ToString() },
                    { "Hide", hidden.ToString() }
                }
            };
            if (args.Length > paramIndex)
            {
                var parameters = args.Skip(paramIndex);
                foreach (var str in parameters)
                    model.Parameters[str.Split(':')[0]] = str.Split(':')[1];
            }
            Main.Instance.ScriptModel.Add(model);
            Main.Instance.Common.RawSayTo(sender,  (hidden ? "^9Hidden ^7" : "^7") + type + " ^2Saved^7, Id: ^2" + model.Id);
            sender.SetField("isDrawing", "false");
            Main.Instance.Script.AfterDelay(1000, () => Main.Instance.Common.ToggleFly(sender));
        }
        private void AddCommand(string name, Action<Entity, string[]> run, string usage = null, List<string> aliases = null) => Commands.AddCommand(name, run, usage, aliases);
        public bool HandleCommand(Entity player, string message)
        {
            string[] args = message.Split(' ');
            args[0] = args[0].ToLowerInvariant();

            foreach (var command in Commands.CommandList.Where(x => x.Active))
            {
                if ((command.Name.ToLower() == args[0].ToLower()) || (command.Alias?.Contains(args[0]) ?? false))// || ((command.Aliases != null && command.Aliases.Count() > 0) && command.Aliases.Contains(args[0])))
                {
                    if(args.Length > 1 && args[1] == "/?")
                    {
                        Main.Instance.Common.RawSayTo(player, "^5Command: " + command.Name);
                        Main.Instance.Common.RawSayTo(player, "^5Usage: " + command.Usage ?? "^1No Usage Exists");
                        return true;
                    }
                    try
                    {
                        command.Run(player, args);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("There was an error while handling a command: {0}", ex.Message);
                        return false;
                    }
                }
            }
            //Log.Debug("Command not found {0}", message);
            return false;
        }

        private int m_pos;
        private string NextWord(string m_str)
        {
            return NextWord(m_str, " ");
        }

        private string NextWord(string m_str, string seperator)
        {
            var length = m_str.Length;
            if (m_pos >= length)
                return "";

            int x;
            while ((x = CustomIndexOf(m_str, seperator, m_pos, '\'')) == 0)
            {
                m_pos += seperator.Length;
            }

            if (x < 0)
            {
                if (m_pos == length)
                    return "";
                else
                    x = length;
            }
            var word = m_str.Substring(m_pos, x - m_pos);

            m_pos = x + seperator.Length;
            if (m_pos > length)
                m_pos = length;

            return word;
        }

        private int CustomIndexOf(string m_str, string searchedChar, int startIndex, char containerChar)
        {
            if (searchedChar.Length > 1)
                throw new ArgumentException("searchedChar can only be a char into a string");

            char search = searchedChar[0];

            int index = startIndex;

            bool inContainer = false;
            do
            {
                if (m_str[index] == containerChar)
                    inContainer = !inContainer;

                index++;

                if (index >= m_str.Length)
                    return -1;

            } while (inContainer || m_str[index] != search);

            return index;
        }
    }

    public class Commands
    {
        internal string Name;
        //internal List<string> Alias;
        internal string Usage;
        internal Action<Entity, string[]> Run;
        internal static List<Commands> CommandList = new List<Commands>();
        internal bool Active;
        internal List<string> Alias;
        public Commands()
        {

        }

        public Commands(string CommandName)
        {
        }
        public Commands(string Command_Name, Action<Entity, string[]> run, string usage = null, bool IsActive = true, List<string> alias = null)
        {
            try
            {
                if (Exists(Command_Name))
                {
                    Log.Error("The Command Already Exists With Name = " + Command_Name);
                }
                else
                {
                    Commands item = new Commands
                    {
                        Name = Command_Name,
                        Run = run,
                        Usage = usage,
                        Active = IsActive,
                        Alias = alias,
                    };
                    CommandList.Add(item);
                }
            }
            catch
            {
                Log.Error("Error when creating the command");
            }
        }
        internal bool Exists(string CommandName)
        {
            foreach (Commands item in CommandList)
            {
                if (item.Name.ToLower() == CommandName.ToLower())
                {
                    return true;
                }
            }
            return false;
        }
        internal void AddCommand(string Command_Name, Action<Entity, string[]> Run, string Usage = null, List<string> Alias = null)
        {
            try
            {
                new Commands(Command_Name, Run, Usage, true, Alias);
            }
            catch
            {
                Log.Error("Something wrong with AddCommand");
            }
        }
    }
}
