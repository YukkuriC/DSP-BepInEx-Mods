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
            Harmony.CreateAndPatchAll(typeof(ModMain));
        }

        [HarmonyPatch(typeof(StarGen), "SetStarAge")]
        [HarmonyPostfix]
        static void IncreaseLuminosity(ref StarData star)
        {
            if (star.spectr >= ESpectrType.O)
                star.luminosity *= 1000;
        }
    }
}
