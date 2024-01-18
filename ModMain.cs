using UnityEngine;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;

namespace DSPMod
{
    [BepInPlugin(UID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public partial class ModMain : BaseUnityPlugin
    {
        const int loopInterval = 60;

        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(ModMain));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(EjectorComponent), "InternalUpdate")]
        static void FetchBeforeFireState(bool ___fired, ref bool __state)
        {
            __state = ___fired;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EjectorComponent), "InternalUpdate")]
        static void TryNext(ref EjectorComponent __instance, DysonSwarm swarm, bool ___fired, bool __state)
        {
            bool justFired = (!__state) && ___fired;
            bool invalidOrbit = __instance.targetState == EjectorComponent.ETargetState.AngleLimit;
            bool isIdle = __instance.orbitId <= 0;
            bool idleCheck = isIdle && (GameMain.gameTick + __instance.id) % loopInterval == 0;

            if (justFired || invalidOrbit || idleCheck)
            {
                __instance.orbitId++;
                if (__instance.orbitId >= swarm.orbitCursor)
                {
                    __instance.orbitId = justFired ? 1 : 0;
                }
            }
        }
    }
}
