using System;
using BepInEx;
using HarmonyLib;

namespace DSPMod
{
    [BepInPlugin(UID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public partial class ModMain : BaseUnityPlugin
    {
        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(ModMain));
        }

        [HarmonyPatch(typeof(MechaDroneLogic), "UpdateDrones")]
        [HarmonyPrefix]
        static void AlwaysWork(MechaDroneLogic __instance)
        {
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
            dt *= 5;
        }
    }
}
