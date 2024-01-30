using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfinityScript;
using Newtonsoft.Json;
using System.IO;
using MapEdit.Core;

namespace MapEdit.Misc
{
    public class ScriptModel
    {
        public List<ModelInfo> ModelsList { get; private set; }
        public List<Entity> SpawnedModels { get; private set; }
        private Entity _airdropCollision;
        private Random _rng = new Random();

        public class ModelInfo
        {
            [JsonProperty("Id", Required = Required.Always)]
            public int Id;

            [JsonProperty("Model", Required = Required.Always)]
            public string Model;

            [JsonProperty("Origin", Required = Required.Always)]
            public string Origin;

            [JsonProperty("Angles", Required = Required.Always)]
            public string Angles;

            [JsonProperty("Parameters", Required = Required.Default)]
            public Dictionary<string, string> Parameters;
        }

        [Variable]
        public readonly string MapEditFolder = "scripts/MapEdit";
        [Variable]
        public readonly string MapJson = $"scripts/MapEdit/{Function.Call<string>("getdvar", "mapname")}.json";

        public ScriptModel()
        {
            Entity entity = Main.Instance.Script.Call<Entity>("getent", "care_package", "targetname");
            _airdropCollision = Main.Instance.Script.Call<Entity>("getent", entity.GetField<string>("target"), "targetname");

            ModelsList = new List<ModelInfo>();
            SpawnedModels = new List<Entity>();
            Initialize();
        }

        public void Initialize()
        {
            bool exists = true;

            if (!Directory.Exists(MapEditFolder))
                Directory.CreateDirectory(MapEditFolder);
            if (!File.Exists(MapJson))
            {
                File.WriteAllLines(MapJson, new string[0]);
                exists = false;
            }

            if (ModelsList == null)
                ModelsList = new List<ModelInfo>();

            if (!exists)
            {
                ModelsList.Add(new ModelInfo()
                {
                    Id = -1,
                    Model = "DO NOT EDIT THIS",
                    Angles = new Vector3().ToString(),
                    Origin = new Vector3().ToString(),
                    Parameters = new Dictionary<string, string>() { { "Type", "Script Model" } }
                });
                SaveMap();
            }

            LoadMap();
        }

        public void LoadMap()
        {
            if (ModelsList == null)
                ModelsList = new List<ModelInfo>();
            if (File.Exists(MapJson))
                ModelsList = JsonConvert.DeserializeObject<List<ModelInfo>>(File.ReadAllText(MapJson));
        }
        public void SaveMap()
        {
            if (ModelsList != null && ModelsList.Count > 0)
                File.WriteAllText(MapJson, JsonConvert.SerializeObject(ModelsList, Formatting.Indented));
        }
        public void ReloadMap()
        {
            SaveMap();
            LoadMap();
        }

        public void Add(string name, Vector3 origin, Vector3 angles, Dictionary<string, string> dict)
        {
            Add(new ModelInfo()
            {
                Id = ModelsList.Count - 1,
                Model = name,
                Origin = origin.ToString(),
                Angles = angles.ToString(),
                Parameters = dict,
            });
        }
        public void Add(ModelInfo model)
        {
            ModelsList.Add(model);
            SaveMap();
        }

        public ModelInfo Get(int id)
        {
            if (!Exists(id))
                return null;

            return ModelsList.Where(x => x.Id == id).FirstOrDefault();
        }
        public List<ModelInfo> Get(string name)
        {
            if (!Exists(name))
                return new List<ModelInfo>();

            return ModelsList.Where(x => x.Model == name).ToList();
        }

        public void Remove(int id)
        {
            if (!Exists(id))
                return;
            Remove(ModelsList.Where(x => x.Id == id).FirstOrDefault());
        }
        public void Remove(ModelInfo modelinfo)
        {
            ModelsList.Remove(modelinfo);
            foreach (var model in ModelsList.Where(x => x.Id > modelinfo.Id))
                model.Id -= 1;
            SaveMap();
        }

        public bool Exists(int id)
        {
            foreach (var model in ModelsList)
                if (model.Id == id)
                    return true;
            return false;
        }
        public bool Exists(string name)
        {
            foreach (var model in ModelsList)
                if (model.Model == name)
                    return true;
            return false;
        }

        public void SpawnAll()
        {
            if (SpawnedModels == null)
                SpawnedModels = new List<Entity>();
            foreach (var model in ModelsList)
            {
                Spawn(model);
            }
        }

        public Entity Spawn(ModelInfo model)
        {
            Entity entity = null;
            bool done = false;
            string type = GetParam(model, "Type");
            Vector3 Origin = new Vector3(), Angles = new Vector3();
            if (!TryParse(model.Angles, out Angles) || !TryParse(model.Origin, out Origin))
            {
                if (type == "Model" || type == "Turret")
                {
                    Log.Error("There is an currpoted Vector3 at model id: " + model.Id);
                    return null;
                }
            }

            if (model.Parameters.Count() == 0 || model.Parameters == null || type == "Model" && !done)
            {
                entity = Function.Call<Entity>("spawn", GetParam(model, "Spawn-Type"), Origin);
                entity.Call("setmodel", model.Model);
                entity.SetField("angles", Angles);
                done = true;
            }
            //if (type == "Turret")
            //{
            //    string minigunType = ContainsParam(model, "MinigunType") ? GetParam(model, "MinigunType") : "sentry_minigun_mp";// "sentry_minigun_mp" : "pavelow_minigun_mp"
            //    string minigunModel = ContainsParam(model, "MinigunModel") ? GetParam(model, "MinigunModel") : "sentry_minigun";// "weapon_minigun" : "sentry_minigun"

            //    entity = Main.Instance.Script.Call<Entity>("spawn", "script_model");
            //    entity.SetField("angles", new Parameter(Angles));
            //    if (Angles.Equals(null))
            //        Angles = new Vector3(0f, 0f, 0f);
            //    entity = Main.Instance.Script.Call<Entity>("spawnTurret", "misc_turret", new Parameter(Origin), minigunType);
            //    entity.Call("setmodel", minigunModel);
            //    entity.SetField("angles", Angles);
            //}
            else
            {
                if (GetParam(model, "Start") == null && GetParam(model, "End") == null)
                    return null;
                TryParse(GetParam(model, "Start"), out Vector3 Start);
                TryParse(GetParam(model, "End"), out Vector3 End);
                bool hide = ContainsParam(model, "Hide") ? bool.Parse(GetParam(model, "Hide")) : false;

                if (type == "Ramp" && !done)
                {
                    float num = Start.DistanceTo(End);
                    int num2 = (int)Math.Ceiling((double)(num / 30f));
                    Vector3 vector = new Vector3((Start.X - End.X) / (float)num2, (Start.Y - End.Y) / (float)num2, (Start.Z - End.Z) / (float)num2);
                    Vector3 vector2 = Main.Instance.Script.Call<Vector3>("vectortoangles", Start - End);
                    Vector3 angles = new Vector3(vector2.Z, vector2.Y + 90f, vector2.X);
                    for (int i = 0; i <= num2; i++)
                    {
                        SpawnCrate(End + (vector * (float)i), angles, hide ? null : "com_plasticcase_trap_friendly");
                    }
                    done = true;
                }

                if (type == "Wall" && !done)
                {
                    Vector3 start = Start, end = End;
                    float num = new Vector3(start.X, start.Y, 0f).DistanceTo(new Vector3(end.X, end.Y, 0f));
                    float num2 = new Vector3(0f, 0f, start.Z).DistanceTo(new Vector3(0f, 0f, end.Z));
                    int num3 = (int)Math.Round((double)(num / 55f), 0);
                    int num4 = (int)Math.Round((double)(num2 / 30f), 0);
                    Vector3 vector = end - start;
                    Vector3 vector2 = new Vector3(vector.X / (float)num3, vector.Y / (float)num3, vector.Z / (float)num4);
                    float num5 = vector2.X / 4f;
                    float num6 = vector2.Y / 4f;
                    Vector3 vector3 = Main.Instance.Script.Call<Vector3>("vectortoangles", vector);
                    vector3 = new Vector3(0f, vector3.Y, 90f);
                    entity = Main.Instance.Script.Call<Entity>("spawn", "script_origin", new Vector3((start.X + end.X) / 2f, (start.Y + end.Y) / 2f, (start.Z + end.Z) / 2f));

                    for (int i = 0; i < num4; i++)
                    {
                        Entity entity2 = SpawnCrate(start + new Vector3(num5, num6, 10f) + new Vector3(0f, 0f, vector2.Z) * (float)i, vector3, hide ? null : "com_plasticcase_trap_friendly");
                        entity2.Call("enablelinkto", new Parameter[0]);
                        entity2.Call("linkto", entity);
                        for (int j = 0; j < num3; j++)
                        {
                            entity2 = SpawnCrate(start + new Vector3(vector2.X, vector2.Y, 0f) * (float)j + new Vector3(0f, 0f, 10f) + new Vector3(0f, 0f, vector2.Z) * (float)i, vector3, hide ? null : "com_plasticcase_trap_friendly");
                            entity2.Call("enablelinkto", new Parameter[0]);
                            entity2.Call("linkto", entity);
                        }
                        entity2 = SpawnCrate(new Vector3(end.X, end.Y, start.Z) + new Vector3(num5 * -1f, num6 * -1f, 10f) + new Vector3(0f, 0f, vector2.Z) * (float)i, vector3, hide ? null : "com_plasticcase_trap_friendly");
                        entity2.Call("enablelinkto", new Parameter[0]);
                        entity2.Call("linkto", entity);
                    }
                    done = true;
                }

                if(type == "Floor" && !done)
                {
                    CreateFloor(Start, End, hide);
                }

                if(type == "Teleport")
                {
                    Entity flag = Main.Instance.Script.Call<Entity>("spawn", "script_model", Start);
                    Entity flag2 = Main.Instance.Script.Call<Entity>("spawn", "script_model",End);
                    Entity flag1op = flag1op = Main.Instance.Script.Call<Entity>("spawn", "script_model", Start);
                    int _curObjID = model.Id;
                    if (!hide)
                    {
                        flag.Call("setModel", GetAlliesFlag());
                        flag2.Call("setModel", "weapon_oma_pack");
                        flag1op.Call("setmodel", "weapon_scavenger_grenadebag");

                        Main.Instance.Script.Call(431, _curObjID, "active"); // objective_add
                        Main.Instance.Script.Call(435, _curObjID, new Parameter(flag.Origin)); // objective_position
                        Main.Instance.Script.Call(434, _curObjID, "compass_waypoint_bomb"); // objective_icon
                    }
                    else
                    {
                        flag1op = null;
                        flag.Call("setModel", "weapon_scavenger_grenadebag");
                        flag2.Call("setModel", "weapon_oma_pack");
                    }

                    if (ContainsParam(model, "model-enter"))
                        flag.Call("setmode", GetParam(model, "model-enter"));
                    if (ContainsParam(model, "model-exit"))
                        flag2.Call("setmode", GetParam(model, "model-exit"));
                    if(ContainsParam(model, "model-enter-ext") && !hide)
                        flag1op.Call("setmode", GetParam(model, "model-enter-ext"));
                    if(ContainsParam(model, "objective_icon") && !hide)
                        Main.Instance.Script.Call(434, _curObjID, GetParam(model, "objective_icon"));
                    if (ContainsParam(model, "icon") && !hide)
                        Main.Instance.Script.Call(434, _curObjID, GetParam(model, "icon"));

                    Main.Instance.Script.OnInterval(100, () =>
                    {
                        foreach (Entity player in Main.Instance.Script.Players)
                        {
                            if (player.Origin.DistanceTo(Start) <= 50)
                            {
                                player.Call("setorigin", new Parameter(End));
                            }
                        }
                        return true;
                    });
                }

                if(type == "Door" && !done)
                {
                    int size = int.Parse(GetParam(model, "Size"));
                    int hp = int.Parse(GetParam(model, "Health"));
                    int height = int.Parse(GetParam(model, "Heigth"));

                    double offset = (((size / 2) - 0.5) * -1.0);
                    Entity center = Main.Instance.Script.Call<Entity>("spawn", "script_model", new Parameter(Start));
                    for (int j = 0; j < size; j++)
                    {
                        Entity door = SpawnCrate(Start + new Vector3(0, 30f, 0) * ((float)offset), new Vector3(0, 0, 0));
                        door.Call("setModel", "com_plasticcase_enemy");
                        door.Call("enablelinkto", 0);
                        door.Call("linkto", center);
                        for (int h = 1; h < height; h++)
                        {
                            Entity door2 = SpawnCrate(Start + new Vector3(0, 30f, 0) * ((float)offset) - new Vector3(70, 0, 0) * h, new Vector3(0, 0, 0));
                            door2.Call("setModel", "com_plasticcase_enemy");
                            door2.Call("enablelinkto", 0);
                            door2.Call("linkto", center);
                        }
                        offset += 1.0;
                    }
                    center.SetField("type", "Door");
                    center.SetField("range", 100);
                    center.SetField("angles", new Parameter(Angles));
                    center.SetField("state", "open");
                    center.SetField("hp", hp);
                    center.SetField("maxhp", hp);
                    center.SetField("open", new Parameter(Start));
                    center.SetField("close", new Parameter(End));

                    SpawnedModels.Add(center);
                    done = true;
                }

                if(type == "Elevator" && !done)
                {
                    Entity ent1 = Main.Instance.Script.Call<Entity>("spawn", "script_origin", Start);
                    Entity ent2 = CreateFloor(new Vector3(Start.X - 45f, Start.Y - 45f, Start.Z), new Vector3(Start.X + 45f, Start.Y + 45f, Start.Z), hide);
                    ent2.Call("enablelinkto", 0);
                    ent2.Call("linkto", ent1);
                    ent1.SetField("currentPos", "pos1");
                    ent1.SetField("pos1", Start);
                    ent1.SetField("pos2", End);
                    Main.Instance.Script.OnInterval(5000, () => {
                        if (ent1.GetField<string>("currentPos") == "pos1")
                        {
                            Main.Instance.Script.Call("playsoundatpos", ent1.Origin, "elev_run_start");
                            ent1.Call("moveto", ent1.GetField<Vector3>("pos2"), 2f);
                            Main.Instance.Script.AfterDelay(500, () => ent1.SetField("currentPos", "pos2"));
                            Main.Instance.Script.OnInterval(50, () => {
                                foreach (Entity player in Main.Instance.Script.Players)
                                {
                                    if (player.Origin.DistanceTo(ent1.Origin) <= 80f)
                                        player.Call("setorigin", ent1.Origin + new Vector3(0, 0, 15f));
                                }
                                if (ent1.Origin.ToString() == ent1.GetField<Vector3>("pos2").ToString())
                                {
                                    Main.Instance.Script.Call("playsoundatpos", ent1.Origin, "elev_bell_ding");
                                    return false;
                                }
                                return true;
                            });
                        }
                        else
                        {
                            ent1.Call("moveto", ent1.GetField<Vector3>("pos1"), 2);
                            Main.Instance.Script.AfterDelay(500, () => ent1.SetField("currentPos", "pos1"));
                        }
                        return true;
                    });
                }
            }

            if (entity != null)
            {
                foreach (var param in model.Parameters)
                {

                    if (param.Key == "origin" && TryParse(param.Value, out Vector3 newOrigin))
                        entity.SetField("origin", Origin + newOrigin);
                    else if (param.Key == "angles" && TryParse(param.Value, out Vector3 newAngles))
                        entity.SetField("angles", Angles + newAngles);
                    else if (param.Key.StartsWith("precache"))
                        Main.Instance.Script.Call(param.Key, param.Value);
                    else if (param.Key.StartsWith("call|"))
                        entity.Call(param.Key.Split('|')[1], param.Value);
                    else
                        entity.SetField(param.Key, param.Value);
                }

                SpawnedModels.Add(entity);
            }
            return entity;
        }

        public Entity CreateFloor(Vector3 corner1, Vector3 corner2, bool hidden)
        {
            float width = corner1.X - corner2.X;
            if (width < 0) width = width * -1;
            float length = corner1.Y - corner2.Y;
            if (length < 0) length = length * -1;

            int bwide = (int)Math.Round(width / 50, 0);
            int blength = (int)Math.Round(length / 30, 0);
            Vector3 C = corner2 - corner1;
            Vector3 A = new Vector3(C.X / bwide, C.Y / blength, 0);
            Entity center = Main.Instance.Script.Call<Entity>("spawn", "script_origin", (new Vector3(
                (corner1.X + corner2.X) / 2, (corner1.Y + corner2.Y) / 2, corner1.Z)));
            for (int i = 0; i < bwide; i++)
            {
                for (int j = 0; j < blength; j++)
                {
                    Entity crate = SpawnCrate(corner1 + (new Vector3(A.X, 0, 0) * i) + (new Vector3(0, A.Y, 0) * j), new Vector3(0, 0, 0), hidden ? null : "com_plasticcase_enemy");
                    crate.Call("enablelinkto", 0);
                    crate.Call("linkto", center);
                }
            }
            return center;
        }

        public Entity SpawnCrate(Vector3 origin, Vector3 angles, string model = null)
        {
            Entity entity = Main.Instance.Script.Call<Entity>("spawn", "script_model", origin);
            entity.SetField("angles", angles);
            entity.Call(33353, _airdropCollision);
            if (model != null)
                entity.Call("setmodel", model);
            return entity;
        }


        public string GetAlliesFlag()
        {
            string mapname = Main.Instance.Script.Call<string>("getdvar", "mapname");
            switch (mapname)
            {
                case "mp_alpha":
                case "mp_dome":
                case "mp_exchange":
                case "mp_hardhat":
                case "mp_interchange":
                case "mp_lambeth":
                case "mp_radar":
                case "mp_cement":
                case "mp_hillside_ss":
                case "mp_morningwood":
                case "mp_overwatch":
                case "mp_park":
                case "mp_qadeem":
                case "mp_restrepo_ss":
                case "mp_terminal_cls":
                case "mp_roughneck":
                case "mp_boardwalk":
                case "mp_moab":
                case "mp_nola":
                    return "prop_flag_delta";
                case "mp_bootleg":
                case "mp_bravo":
                case "mp_carbon":
                case "mp_mogadishu":
                case "mp_village":
                case "mp_shipbreaker":
                    return "prop_flag_pmc";
                case "mp_paris":
                    return "prop_flag_gign";
                case "mp_plaza2":
                case "mp_seatown":
                case "mp_underground":
                case "mp_aground_ss":
                case "mp_courtyard_ss":
                case "mp_italy":
                case "mp_meteora":
                    return "prop_flag_sas";
            }
            return "";
        }
        public string GetAxisFlag()
        {
            string mapname = Main.Instance.Script.Call<string>("getdvar", "mapname");
            switch (mapname)
            {
                case "mp_alpha":
                case "mp_bootleg":
                case "mp_dome":
                case "mp_exchange":
                case "mp_hardhat":
                case "mp_interchange":
                case "mp_lambeth":
                case "mp_paris":
                case "mp_plaza2":
                case "mp_radar":
                case "mp_underground":
                case "mp_cement":
                case "mp_hillside_ss":
                case "mp_overwatch":
                case "mp_park":
                case "mp_restrepo_ss":
                case "mp_terminal_cls":
                case "mp_roughneck":
                case "mp_boardwalk":
                case "mp_moab":
                case "mp_nola":
                    return "prop_flag_speznas";
                case "mp_bravo":
                case "mp_carbon":
                case "mp_mogadishu":
                case "mp_village":
                case "mp_shipbreaker":
                    return "prop_flag_africa";
                case "mp_seatown":
                case "mp_aground_ss":
                case "mp_courtyard_ss":
                case "mp_meteora":
                case "mp_morningwood":
                case "mp_qadeem":
                case "mp_italy":
                    return "prop_flag_ic";
            }
            return "";
        }

        public void HandleUseables(Entity player)
        {
            if (player.GetField<string>("sessionteam") == "spectator") return;
            foreach (Entity ent in SpawnedModels)
            {
                if (ent.HasField("type"))
                {
                    if (ent.GetField<string>("type") == "Door")
                    {
                        if (player.Origin.DistanceTo(ent.Origin) < 100f)
                        {
                            usedDoor(ent, player);
                        }
                    }
                }
            }
        }

        public void UsablesHud(Entity player)
        {
            HudElem message = HudElem.CreateFontString(player, "hudbig", 0.6f);
            message.SetPoint("CENTER", "CENTER", 0, 70);
            Main.Instance.Script.OnInterval(100, () =>
            {
                bool _changed = false;
                foreach (Entity ent in SpawnedModels)
                {
                    if (ent.HasField("type"))
                    {
                        if (ent.GetField<string>("type") == "Door")
                        {
                            if (player.Origin.DistanceTo(ent.Origin) < 100f)
                            {
                                message.SetText(getDoorText(ent, player));
                                _changed = true;
                            }
                        }
                    }
                }
                if (!_changed)
                {
                    message.SetText("");
                }
                return true;
            });
        }


        public string getDoorText(Entity door, Entity player)
        {
            int hp = door.GetField<int>("hp");
            int maxhp = door.GetField<int>("maxhp");
            if (player.GetField<string>("sessionteam") == "allies")
            {
                switch (door.GetField<string>("state"))
                {
                    case "open":
                        if (player.CurrentWeapon == "defaultweapon_mp")
                            return "Door is Open. Press ^3[{+activate}] ^7to repair it. (" + hp + "/" + maxhp + ")";
                        return "Door is Open. Press ^3[{+activate}] ^7to close it. (" + hp + "/" + maxhp + ")";
                    case "close":
                        if (player.CurrentWeapon == "defaultweapon_mp")
                            return "Door is Closed. Press ^3[{+activate}] ^7to repair it. (" + hp + "/" + maxhp + ")";
                        return "Door is Closed. Press ^3[{+activate}] ^7to open it. (" + hp + "/" + maxhp + ")";
                    case "broken":
                        if (player.CurrentWeapon == "defaultweapon_mp")
                            return "Door is Broken. Press ^3[{+activate}] ^7to repair it. (" + hp + "/" + maxhp + ")";
                        return "^1Door is Broken.";
                }
            }
            else if (player.GetField<string>("sessionteam") == "axis")
            {
                switch (door.GetField<string>("state"))
                {
                    case "open":
                        return "Door is Open.";
                    case "close":
                        return "Press ^3[{+activate}] ^7to attack the door.";
                    case "broken":
                        return "^1Door is Broken .";
                }
            }
            return "";
        }

        private void repairDoor(Entity door, Entity player)
        {
            if (player.GetField<int>("repairsleft") == 0) return; // no repairs left on weapon

            if (door.GetField<int>("hp") < door.GetField<int>("maxhp"))
            {
                door.SetField("hp", door.GetField<int>("hp") + 1);
                player.SetField("repairsleft", player.GetField<int>("repairsleft") - 1);
                player.Call("iprintlnbold", "Repaired Door! (" + player.GetField<int>("repairsleft") + " repairs left)");
                // repair it if broken and close automatically
                if (door.GetField<string>("state") == "broken")
                {
                    door.Call(33399, new Parameter(door.GetField<Vector3>("close")), 1); // moveto
                    Main.Instance.Script.AfterDelay(300, () =>
                    {
                        door.SetField("state", "close");
                    });
                }
            }
            else
            {
                player.Call("iprintlnbold", "Door has full health!");
            }
        }

        private void usedDoor(Entity door, Entity player)
        {
            if (!player.IsAlive) return;
            // has repair weapon. do repair door
            if (player.CurrentWeapon.Equals("defaultweapon_mp"))
            {
                repairDoor(door, player);
                return;
            }
            if (door.GetField<int>("hp") > 0)
            {
                if (player.GetField<string>("sessionteam") == "allies")
                {
                    if (door.GetField<string>("state") == "open")
                    {
                        door.Call(33399, door.GetField<Vector3>("close"), 1); // moveto
                        Main.Instance.Script.AfterDelay(300, () => door.SetField("state", "close"));
                    }
                    else if (door.GetField<string>("state") == "close")
                    {
                        door.Call(33399, door.GetField<Vector3>("open"), 1); // moveto
                        Main.Instance.Script.AfterDelay(300, () => door.SetField("state", "open"));
                    }
                }
                else if (player.GetField<string>("sessionteam") == "axis")
                {
                    if (door.GetField<string>("state") == "close")
                    {
                        if (player.GetField<int>("attackeddoor") == 0)
                        {
                            int hitchance = 0;
                            switch (player.Call<string>("getstance", 0))
                            {
                                case "prone":
                                    hitchance = 20;
                                    break;
                                case "couch":
                                    hitchance = 45;
                                    break;
                                case "stand":
                                    hitchance = 90;
                                    break;
                                default:
                                    break;
                            }
                            if (_rng.Next(100) < hitchance)
                            {
                                door.SetField("hp", door.GetField<int>("hp") - 1);
                                player.Call("iprintlnbold", "Hit: " + door.GetField<int>("hp") + "/" + door.GetField<int>("maxhp"));
                            }
                            else
                            {
                                player.Call("iprintlnbold", "^1MISS");
                            }
                            player.SetField("attackeddoor", 1);
                            player.AfterDelay(1000, (e) => player.SetField("attackeddoor", 0));
                        }
                    }
                }
            }
            else if (door.GetField<int>("hp") == 0 && door.GetField<string>("state") != "broken")
            {
                if (door.GetField<string>("state") == "close")
                    door.Call(33399, door.GetField<Vector3>("open"), 1f); // moveto
                door.SetField("state", "broken");
            }
        }

        public bool ContainsParam(ModelInfo model, string key)
        {
            if (model.Parameters.ContainsKey(key))
                return true;
            return false;
        }
        public string GetParam(ModelInfo model, string key)
        {
            //if (model.Parameters.Count() == 0 || model.Parameters == null)
            //    return false;
            if (model.Parameters.ContainsKey(key))
                return model.Parameters[key];
            return null;
        }

        /// <summary>
        /// Parse string to Vector3
        /// </summary>
        /// <param name="str">Target string to parse</param>
        /// <returns></returns>
        public Vector3 Parse(string str)
        {
            str = str.Replace(" ", string.Empty);

            if (!str.StartsWith("(") && !str.EndsWith(")"))
                throw new Exception("Wrong Vector3 Format At " + str);

            str = str.Replace("(", "").Replace(")", "");
            string[] array = str.Split(',');

            if (array.Length < 3)
                throw new Exception("Wrong Vector3 Format At " + str);

            return new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));
        }

        /// <summary>
        /// Parse string to Vector3 and return true if string is in correct format, overwise returs false
        /// </summary>
        /// <param name="str">Target string to parse</param>
        /// <param name="Vector3">Output parsed Vector3</param>
        /// <returns></returns>
        public bool TryParse(string str, out Vector3 Vector3)
        {
            str = str.Replace(" ", string.Empty);

            if (!str.StartsWith("(") && !str.EndsWith(")"))
            {
                Vector3 = new Vector3();
                return false;
            }
            str = str.Replace("(", "").Replace(")", "");
            string[] array = str.Split(',');

            if (array.Length < 3)
            {
                Vector3 = new Vector3();
                return false;
            }

            Vector3 = new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));
            return true;
        }
    }
}
