using UnityEngine;
using BepInEx;
using HarmonyLib;

namespace DSPMod
{
    [BepInPlugin(UID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    [HarmonyPatch]
    public partial class ModMain : BaseUnityPlugin
    {
        static Harmony patcher;
        void Start()
        {
            patcher = new Harmony(UID);
            patcher.PatchAll();
        }
    }
}
