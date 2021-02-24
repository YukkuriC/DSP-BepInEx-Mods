using UnityEngine;
using BepInEx;
using HarmonyLib;

namespace DSPMod
{
    [BepInPlugin(UID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public partial class ModMain : BaseUnityPlugin
    {
        static EPlanetSingularity[] noTidal = {
            EPlanetSingularity.TidalLocked2,
            EPlanetSingularity.TidalLocked4,
            EPlanetSingularity.ClockwiseRotate,
        };

        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(ModMain));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlanetGen), "CreatePlanet")]
        static void Lock(PlanetData __result, int orbitAround)
        {
            if (orbitAround != 0) return;
            __result.rotationPeriod = __result.orbitalPeriod;
            __result.singularity |= EPlanetSingularity.TidalLocked;
            foreach (var flag in noTidal)
            {
                if ((__result.singularity & flag) != EPlanetSingularity.None)
                    __result.singularity ^= flag;
            }
        }
    }
}
