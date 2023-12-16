using UnityEngine;
using BepInEx;
using HarmonyLib;
using ABN;

namespace NoCheck
{
    [BepInPlugin(UID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public partial class ModMain : BaseUnityPlugin
    {
        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(ModMain));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameAbnormalityData_0925), nameof(GameAbnormalityData_0925.TriggerAbnormality))]
        static bool NoTrigger()
        {
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameAbnormalityData_0925), nameof(GameAbnormalityData_0925.NothingAbnormal))]
        [HarmonyPatch(typeof(AchievementLogic), "get_isSelfFormalGame")]
        [HarmonyPatch(typeof(AchievementLogic), "get_active")]
        static bool AlwaysTrue(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameAbnormalityData_0925), nameof(GameAbnormalityData_0925.IsAbnormalTriggerred))]
        static bool AlwaysFalse(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}
