using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using InfinityScript;
using System.IO;
using MapEdit.Core;

namespace MapEdit.Misc
{
    public class ScriptAdmins
    {
        public List<AdminsInfo> AdminsList { get; private set; }
        public class AdminsInfo
        {
            [JsonProperty("Id", Required = Required.Always)]
            public int Id;

            [JsonProperty("Name", Required = Required.Always)]
            public string Name;

            [JsonProperty("HWID", Required = Required.Always)]
            public string HWID;

            [JsonProperty("Parameters", Required = Required.Default)]
            public Dictionary<string, string> Parameters;
        }

        [Variable]
        public readonly string MapEditFolder = "scripts/MapEdit";
        [Variable]
        public readonly string AdminsJson = $"scripts/MapEdit/Admins.json";

        public ScriptAdmins()
        {
            AdminsList = new List<AdminsInfo>();
            Initialize();
        }

        public void Initialize()
        {
            bool exists = true;

            if (!Directory.Exists(MapEditFolder))
                Directory.CreateDirectory(MapEditFolder);
            if (!File.Exists(AdminsJson))
            {
                File.WriteAllLines(AdminsJson, new string[0]);
                exists = false;
            }

            if (AdminsList == null)
                AdminsList = new List<AdminsInfo>();

            if (!exists)
            {
                AdminsList.Add(new AdminsInfo()
                {
                    Id = -1,
                    Name = "Script Admin",
                    HWID = "00000000-00000000-00000000",
                    Parameters = new Dictionary<string, string>() { { "Perms", "NONE" } }
                });
                SaveAdmins();
            }

            LoadAdmins();
        }

        public void LoadAdmins()
        {
            if (AdminsList == null)
                AdminsList = new List<AdminsInfo>();
            if (File.Exists(AdminsJson))
                AdminsList = JsonConvert.DeserializeObject<List<AdminsInfo>>(File.ReadAllText(AdminsJson));
        }
        public void SaveAdmins()
        {
            if (AdminsList != null && AdminsList.Count > 0)
                File.WriteAllText(AdminsJson, JsonConvert.SerializeObject(AdminsList, Formatting.Indented));
        }
        public void ReloadAdmins()
        {
            SaveAdmins();
            LoadAdmins();
        }

        public void Add(string name, string hwid, Dictionary<string, string> dict)
        {
            Add(new AdminsInfo()
            {
                Id = AdminsList.Count - 1,
                Name = name,
                HWID = hwid,
                Parameters = dict,
            });
        }
        public void Add(AdminsInfo model)
        {
            AdminsList.Add(model);
            SaveAdmins();
        }

        public AdminsInfo Get(int id)
        {
            if (!Exists(id))
                return null;

            return AdminsList.Where(x => x.Id == id).FirstOrDefault();
        }
        public AdminsInfo Get(string hwid)
        {
            if (!Exists(hwid))
                return null;

            return AdminsList.Where(x => x.HWID == hwid).FirstOrDefault();
        }
        public List<AdminsInfo> GetAll(string name)
        {
            if (!Exists(name))
                return new List<AdminsInfo>();

            return AdminsList.Where(x => x.Name.ToLower().Contains(name.ToLower())).ToList();
        }

        public void Remove(int id)
        {
            if (!Exists(id))
                return;
            Remove(AdminsList.Where(x => x.Id == id).FirstOrDefault());
        }
        public void Remove(string hwid)
        {
            if (!Exists(hwid))
                return;
            Remove(AdminsList.Where(x => x.HWID == hwid).FirstOrDefault());
        }
        public void Remove(AdminsInfo admininfo)
        {
            AdminsList.Remove(admininfo);
            foreach (var admin in AdminsList.Where(x => x.Id > admininfo.Id))
                admin.Id -= 1;
            SaveAdmins();
        }

        public bool Exists(int id)
        {
            foreach (var model in AdminsList)
                if (model.Id == id)
                    return true;
            return false;
        }
        public bool Exists(string str, bool name = false)
        {
            foreach (var model in AdminsList)
            {
                if (name)
                {
                    if (model.Name.ToLower().Contains(str.ToLower()))
                        return true;
                }
                else
                {
                    if (model.HWID == str)
                        return true;
                }
            }
            return false;
        }
    }
}
