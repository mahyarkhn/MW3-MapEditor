using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapEdit.Core.Reflection;
using InfinityScript;

namespace MapEdit.Misc
{
    public class Common
    {
        public void ToggleFly(Entity player)
        {
            if(!player.HasField("fly") || player.GetField<string>("fly") != "true")
            {
                player.Call("allowspectateteam", "frelook", true);
                player.SetField("sessionstate", "spectator");
                player.Call("setcontents", 0);
                player.SetField("fly", "true");
            }
            else
            {
                player.Call("allowspectateteam", "frelook", false);
                player.SetField("sessionstate", "playing");
                player.Call("setcontents", 100);
                player.SetField("fly", "false");
            }
        }
        public void RawSayToMultiline(Entity player, string[] message, int delay)
        {
            int num = 0;
            foreach (string str in message)
            {
                string messagez = str;
                Main.Instance.Script.AfterDelay(num * delay, (() => RawSayTo(player, messagez)));
                ++num;
            }
        }
        public void RawSayTo(Entity sender, string message) => Utilities.RawSayTo(sender, "^7[^3AG^7]: " + message);
        public void RawSayToCondense(Entity player, string[] messages, int delay = 1000, int condenselevel = 40, string separator = ", ")
        {
            RawSayToMultiline(player, Condense(messages, condenselevel, separator), delay);
        }
        public string NoColor(string message)
        {
            for (int x = 0; x < 10; x++)
            {
                message = message.Replace("^" + x, "").Replace("^:", "").Replace("^;", "");
            }
            return message;
        }
        public string[] Condense(string[] arr, int condenselevel = 40, string separator = ", ")
        {
            if (arr.Length < 1)
                return arr;
            List<string> lines = new List<string>();
            int index = 0;
            string line = arr[index++];
            while (index < arr.Length)
            {
                if (NoColor((line + separator + arr[index])).Length > condenselevel)
                {
                    lines.Add(line);
                    line = arr[index];
                    index++;
                    continue;
                }
                line += separator + arr[index];
                index++;
            }
            lines.Add(line);
            return lines.ToArray();
        }
    }
}
