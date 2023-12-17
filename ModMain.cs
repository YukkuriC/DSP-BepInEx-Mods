using UnityEngine;
using BepInEx;
using HarmonyLib;
using System.Collections.Generic;

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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_Click), "CheckBuildConditions")]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), "CheckBuildConditions")]
        static void UnlockEquator(object __instance, ref bool __result)
        {
            var hasMidified = false;
            var isBlueprint = __instance is BuildTool_BlueprintPaste;
            IEnumerable<BuildPreview> data;
            if (isBlueprint) data = (__instance as BuildTool_BlueprintPaste).bpPool;
            else data = (__instance as BuildTool).buildPreviews;
            foreach (var bp in data)
            {
                if (bp.condition == EBuildCondition.BuildInEquator)
                {
                    hasMidified = true;
                    bp.condition = EBuildCondition.Ok;
                }
            }

            if(hasMidified)
            {
                var newRet = true;
                foreach (var bp in data) if(bp.condition!= EBuildCondition.Ok)
                    {
                        newRet = false;
                        break;
                    }
                __result = newRet;
            }
        }
    }
}
