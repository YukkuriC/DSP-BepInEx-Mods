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
        const float fireAnimTime = 10f;
        const int loopInterval = 60;

        static HashSet<int> firedLoop;

        void Start()
        {
            firedLoop = new HashSet<int>();
            Harmony.CreateAndPatchAll(typeof(ModMain));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EjectorComponent), "InternalUpdate")]
        static void TryNext(ref EjectorComponent __instance, DysonSwarm swarm, AnimData[] animPool)
        {
            bool justFired = animPool[__instance.entityId].time == fireAnimTime;
            bool invalidOrbit = __instance.targetState == EjectorComponent.ETargetState.AngleLimit;
            bool isIdle = __instance.orbitId <= 0;

            // 标记开火状态
            if (justFired) firedLoop.Add(__instance.id);

            // 切换轨道
            if (justFired || invalidOrbit || (isIdle && (GameMain.gameTick + __instance.id) % loopInterval == 0))
            {
                __instance.orbitId++;
                if (__instance.orbitId >= swarm.orbitCursor)
                {
                    __instance.orbitId = 0; // 默认进入等待模式
                    if (firedLoop.Contains(__instance.id))
                    {
                        __instance.orbitId = 1;
                        firedLoop.Remove(__instance.id);
                    }
                }
            }
        }
    }
}
