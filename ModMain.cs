using System;
using BepInEx;
using HarmonyLib;

namespace DSPMod
{
    [BepInPlugin(UID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public partial class ModMain : BaseUnityPlugin
    {
        const int MINE_SIZE = (int)1e9;

        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(ModMain));
        }

        // 无限沙土
        [HarmonyPatch(typeof(Player), "SetSandCount")]
        [HarmonyPrefix]
        static void MoarSand(ref int newSandCount)
        {
            newSandCount = (int)1e9;
        }

        // 矿机挖矿补充
        [HarmonyPatch(typeof(MinerComponent), "InternalUpdate")]
        [HarmonyPrefix]
        static void MinerAutoRefill(MinerComponent __instance, ref PlanetFactory factory, ref VeinData[] veinPool)
        {
            var self = __instance;
            if (self.type != EMinerType.Vein || self.veins.Length == 0) return;

            int num = self.veins[self.currentVeinIndex];
            RefillMine(factory.planet, ref factory.veinPool[num]);
        }

        // 玩家挖矿补充
        [HarmonyPatch(typeof(PlayerAction_Mine), "GameTick")]
        [HarmonyPrefix]
        static void PlayerAutoRefill(PlayerAction_Mine __instance)
        {
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
