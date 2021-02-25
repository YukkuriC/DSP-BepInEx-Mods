using UnityEngine;
using BepInEx;
using HarmonyLib;

namespace DSPMod
{
    [BepInPlugin(UID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public partial class ModMain : BaseUnityPlugin
    {
        const float fireAnimTime = 10f;
        const int loopInterval = 60;

        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(ModMain));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EjectorComponent), "InternalUpdate")]
        static void TryNext(ref EjectorComponent __instance, DysonSwarm swarm, AnimData[] animPool)
        {
            bool justFired = animPool[__instance.entityId].time == fireAnimTime;
            bool invalidOrbit = __instance.targetState == EjectorComponent.ETargetState.AngleLimit;
            bool isIdle = __instance.orbitId <= 0;
            if (justFired || invalidOrbit || (isIdle && (GameMain.gameTick + __instance.id) % loopInterval == 0))
            {
                __instance.orbitId++;
                if (__instance.orbitId >= swarm.orbitCursor)
                    __instance.orbitId = justFired ? 1 : 0; // 轮询每loop秒发生一次
            }
        }
    }
}
