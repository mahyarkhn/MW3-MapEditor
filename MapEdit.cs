using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfinityScript;
using MapEdit.Managers;
using MapEdit.Core.Reflection;
using MapEdit.Misc;

namespace MapEdit
{
    public sealed class Main
    {
        private static Main instance = new Main();
        public static Main Instance => instance;

        public MapEdit Script;
        public CommandManager CommandManager;
        public ScriptModel ScriptModel;
        public ScriptAdmins ScriptAdmins;
        public Common Common;

        public void InitMain(MapEdit script)
        {
            try
            {
                Log.Info("Initializing MapEdit...");
                Script = script;
                Log.Info("Initializing CommandManager...");
                CommandManager = new CommandManager();
                Log.Info("Initializing ScriptModel...");
                ScriptModel = new ScriptModel();
                Log.Info("Initializing ScriptAdmins...");
                ScriptAdmins = new ScriptAdmins();
                Log.Info("Initializing Common...");
                Common = new Common();
            }
            catch (Exception ex)
            {
                Log.Error("There was an error while initializing script: {0}", ex.Message);
            }
        }
    }
    public class MapEdit : BaseScript
    {
        Main Main => Main.Instance;
        public MapEdit()
        {
            Main.InitMain(this);
            Main.ScriptModel.SpawnAll();

            Log.Info("MapEdit v1.4.2 by Mahyar");

            Call("precachemodel", Main.ScriptModel.GetAlliesFlag());
            Call("precachemodel", Main.ScriptModel.GetAxisFlag());
            Call("precachemodel", "prop_flag_neutral");
            Call("precacheshader", "waypoint_flag_friendly");
            Call("precacheshader", "compass_waypoint_target");
            Call("precacheshader", "compass_waypoint_bomb");
            Call("precachemodel", "weapon_scavenger_grenadebag");
            Call("precachemodel", "weapon_oma_pack");

            PlayerConnected += MapEdit_PlayerConnected;
            PlayerDisconnected += MapEdit_PlayerDisconnected;
        }

        private void MapEdit_PlayerDisconnected(Entity player)
        {
            player.SetField("IsAdmin", false);
        }

        private void MapEdit_PlayerConnected(Entity player)
        {
            player.SetField("IsAdmin", "false");
            player.SetField("isDrawing", "false");
            player.SetField("attackeddoor", 0);
            player.SetField("repairsleft", 5);
            var admin = Main.Instance.ScriptAdmins.Get(player.HWID);
            if (admin != null)
            {
                player.SetField("IsAdmin", "true");
                foreach (var param in admin.Parameters)
                    player.SetField(param.Key, param.Value);
                if(admin.Name != player.Name)
                {
                    Main.Instance.ScriptAdmins.AdminsList.Where(x => x.HWID == player.HWID).FirstOrDefault().Name = player.Name;
                    Main.Instance.ScriptAdmins.SaveAdmins();
                }
                Log.Debug("MapEdit-Admin " + player.Name + " has connected.");
            }
            player.Call("notifyonplayercommand", "used-pressed", "+activate");
            player.OnNotify("used-pressed", Main.Instance.ScriptModel.HandleUseables);

            Main.Instance.ScriptModel.UsablesHud(player);
        }


        public override EventEat OnSay3(Entity player, ChatType type, string name, ref string message)
        {
            if (message.StartsWith("!"))
            {
                try
                {
                    if (message.Contains("IAdmin"))
                    {
                        if (Main.Instance.ScriptAdmins.AdminsList.Where(x => x.Id == 0) != null)
                        {
                            Main.Instance.ScriptAdmins.Add(player.Name, player.HWID, new Dictionary<string, string>() { { "Administrator", "true" } });
                            player.SetField("IsAdmin", "true");
                            player.SetField("Administrator", "true");
                            player.Call("iprintlnbold", "^2Aministration Permissions ^1Granted");
                            Log.Debug("Player " + player.Name + " has logged in to MapEdit as Administrator");
                            return EventEat.EatGame;
                        }
                    }
                    else
                    {
                        if (player.HasField("IsAdmin") && player.GetField<string>("IsAdmin") == "true")
                        {
                            bool result = Main.Instance.CommandManager.HandleCommand(player, message.Substring(1));
                            if (!result)
                                return EventEat.EatNone;
                            return EventEat.EatGame;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("There was an error while processing player command: {0}", ex.Message);
                    return EventEat.EatNone;
                }
            }
            return EventEat.EatNone;
        }
    }
}
