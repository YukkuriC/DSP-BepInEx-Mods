using UnityEngine;
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
            CFG.Init();
            Harmony.CreateAndPatchAll(typeof(ModMain));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "GameTick")]
        static void RunFunctions(Player __instance)
        {
            var plr = __instance;

            if (Input.GetKeyDown(KeyCode.Keypad0)) Semigod.FinishDyson();
            if (Input.GetKeyDown(KeyCode.Keypad1)) CFG.Toggle("overdrive");
        }
    }
}
