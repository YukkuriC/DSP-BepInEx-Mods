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

        [HarmonyPatch(typeof(PlayerAction_Build), "CheckBuildConditions")]
        [HarmonyPostfix]
        static void UnlockEquator(PlayerAction_Build __instance, ref bool __result)
        {
            bool newResult = true;
            foreach (var preview in __instance.buildPreviews)
            {
                if (preview.condition != EBuildCondition.Ok)
                {
                    if (preview.condition == EBuildCondition.BuildInEquator) preview.condition = EBuildCondition.Ok;
                    else newResult = false;
                }
            }
            __result = newResult;
        }
    }
}
