using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;

namespace DSPMod
{
    [BepInPlugin(UID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public partial class ModMain : BaseUnityPlugin
    {
        static HashSet<int> alteredFuncs = new HashSet<int>(new int[] {
            15,
            16,
            20,
            21,
        });

        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(ModMain));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameHistoryData), "UnlockTechFunction")]
        static bool AlterTechFunc(GameHistoryData __instance, int func, double value, int level)
        {
            if (!alteredFuncs.Contains(func)) return true;

            var self = __instance;
            float add2mul = (float)(1 + value),
                  mul2sub = (float)(1 - value);
            switch (func)
            {
                case 15:
                    //self.logisticDroneSpeedScale += (float)value;
                    self.logisticDroneSpeedScale *= add2mul;
                    return false;
                case 16:
                    //self.logisticShipSpeedScale += (float)value;
                    self.logisticShipSpeedScale *= add2mul;
                    return false;
                case 20:
                    //self.miningCostRate *= (float)value;
                    self.miningCostRate = Math.Max(0, self.miningCostRate - mul2sub);
                    return false;
                case 21:
                    //self.miningSpeedScale += (float)value;
                    self.miningSpeedScale *= add2mul;
                    return false;
            }

            throw new Exception("未记录的函数");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameHistoryData), "Import")]
        static void RecalcTechValues(GameHistoryData __instance)
        {
            var self = __instance;

            // reset params
            {
                self.logisticDroneSpeedScale = 1f;
                self.logisticShipSpeedScale = 1f;
                self.miningCostRate = Configs.freeMode.miningCostRate;
                self.miningSpeedScale = Configs.freeMode.miningSpeedScale;
            }

            // grab all unlocked tech IDs
            var techs = new List<TechProto>();
            var techLevels = new List<int>();
            foreach (var pair in self.techStates)
            {
                var tid = pair.Key;
                var state = pair.Value;
                if (state.unlocked || state.curLevel > 0)
                {
                    var tech = LDB.techs.Select(tid);

                    // check function types
                    bool toChoose = false;
                    foreach (int func in tech.UnlockFunctions)
                    {
                        if (alteredFuncs.Contains(func))
                        {
                            toChoose = true;
                            break;
                        }
                    }
                    if (!toChoose) continue;

                    techs.Add(tech);
                    techLevels.Add(Math.Max(state.curLevel, 1));
                }
            }

            // re-unlock selected
            for (int i = 0; i < techs.Count; i++)
            {
                var tech = techs[i];
                var level = techLevels[i];
                for (int idx = 0; idx < tech.UnlockFunctions.Length; idx++)
                {
                    var func = tech.UnlockFunctions[idx];
                    if (!alteredFuncs.Contains(func)) continue;
                    for (int lvl = 0; lvl < level; lvl++)
                        self.UnlockTechFunction(func, tech.UnlockValues[idx], lvl);
                }
            }
        }
    }
}
