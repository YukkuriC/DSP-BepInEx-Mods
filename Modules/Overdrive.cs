using UnityEngine;
using BepInEx;
using HarmonyLib;

namespace DSPMod
{
    public partial class ModMain : BaseUnityPlugin
    {
        [HarmonyPatch(typeof(MechaDroneLogic), "UpdateDrones")]
        [HarmonyPrefix]
        static void AlwaysWork(MechaDroneLogic __instance)
        {
            if (!CFG.Get("overdrive")) return;
            var drones = __instance.player.mecha.drones;
            for (int i = 0; i < drones.Length; i++)
            {
                if (drones[i].movement < 2)
                    drones[i].movement = 2;
            }
        }

        [HarmonyPatch(typeof(MechaDrone), "Update")]
        [HarmonyPrefix]
        static void SpeedUp(ref float dt)
        {
            if (!CFG.Get("overdrive")) return;
            //dt *= 5;
            dt *= 10 * Random.value;
        }
    }
}
