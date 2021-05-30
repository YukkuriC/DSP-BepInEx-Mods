using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSPMod
{
    public static class CFG
    {
        static Dictionary<string, bool> data;
        public static void Init() => data = new Dictionary<string, bool>();

        public static void Toggle(string key) => data[key] = !Get(key);
        public static bool Get(string key) => data.TryGetValue(key, out bool tmp) && tmp;
    }
}
