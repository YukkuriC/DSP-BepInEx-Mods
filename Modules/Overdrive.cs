using UnityEngine;
using BepInEx;
using HarmonyLib;

namespace DSPMod
{
    public partial class ModMain : BaseUnityPlugin
    {
        const int MINE_SIZE = (int)1e9;

        [HarmonyPatch(typeof(MechaDroneLogic), "UpdateDrones")]
        [HarmonyPrefix]
        static void AlwaysWork(MechaDroneLogic __instance)
        {
            if (!CFG.Get("overdrive")) return;
            var drones = __instance.player.mecha.drones;
            for (int i = 0; i < drones.Length; i++)
            {
                if (drones[i].movement < 2)
                    drones[i].movement = 2;
            }
        }

        [HarmonyPatch(typeof(MechaDrone), "Update")]
        [HarmonyPrefix]
        static void SpeedUp(ref float dt)
        {
            if (!CFG.Get("overdrive")) return;
            //dt *= 5;
            dt *= 10 * Random.value;
        }

        // 玩家挖矿补充+高速挖矿
        [HarmonyPatch(typeof(PlayerAction_Mine), "GameTick")]
        [HarmonyPrefix]
        static void PlayerAutoRefill(PlayerAction_Mine __instance)
        {
            if (!CFG.Get("overdrive")) return;
            var self = __instance;
            self.miningTick += (int)(self.player.mecha.miningSpeed * 100000f);
            if (self.miningId == 0 || self.miningType != EObjectType.Vein) return;
            PlanetFactory factory = self.player.factory;
            if (factory == null) return;

            RefillMine(factory.planet, ref factory.veinPool[self.miningId]);
        }

        // 通用矿点填充函数
        static void RefillMine(PlanetData planet, ref VeinData vein)
        {
            int toFill = MINE_SIZE - vein.amount;
            if (toFill == 0) return;

            vein.amount += toFill;
            planet.veinGroups[vein.groupIndex].amount += toFill;
            planet.veinAmounts[(int)vein.type] += toFill;
        }
    }
}
