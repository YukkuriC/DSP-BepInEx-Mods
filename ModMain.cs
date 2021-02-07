using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;

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

        [HarmonyPatch(typeof(PlanetFactory), "Import")]
        [HarmonyPostfix]
        public static void AutoUpgrade(PlanetFactory __instance)
        {
            var self = __instance;

            // 抓手升级
            var inserters = self.factorySystem.inserterPool;
            if (inserters.Length > 0)
            {
                var bestInserter = default(InserterComponent);
                bestInserter.stt = 1919810;
                foreach (var ins in inserters)
                    if (ins.stt > 0 && ins.stt < bestInserter.stt)
                    {
                        bestInserter = ins;
                    }
                if (bestInserter.stt != 1919810)
                    for (int i = 0; i < inserters.Length; i++)
                    {
                        if (inserters[i].stt > bestInserter.stt)
                        {
                            inserters[i].stt = bestInserter.stt;
                            inserters[i].delay = bestInserter.delay;
                            inserters[i].canStack = bestInserter.canStack;
                            inserters[i].stackCount = bestInserter.stackCount;
                            self.SyncEntity(inserters[i].entityId, bestInserter.entityId);
                        }
                    }
            }

            // 制造机
            var assemblers = self.factorySystem.assemblerPool;
            if (assemblers.Length > 0)
            {
                var bestAssembler = default(AssemblerComponent);
                bestAssembler.speed = -1;
                foreach (var ass in assemblers)
                    if (self.GetRecipeType(ass.entityId) == ERecipeType.Assemble &&
                        ass.speed > bestAssembler.speed) bestAssembler = ass;
                if (bestAssembler.speed != -1)
                    for (int i = 0; i < assemblers.Length; i++)
                    {
                        if (self.GetRecipeType(assemblers[i].entityId) == ERecipeType.Assemble &&
                            assemblers[i].speed < bestAssembler.speed)
                        {
                            assemblers[i].speed = bestAssembler.speed;
                            self.SyncEntity(assemblers[i].entityId, bestAssembler.entityId);
                        }
                    }
            }

            // 传送带
            var belts = self.cargoTraffic.beltPool;
            if (belts.Length > 0)
            {
                var bestBelt = default(BeltComponent);
                bestBelt.speed = -1;
                foreach (var bel in belts)
                    if (bel.speed > bestBelt.speed) bestBelt = bel;
                if (bestBelt.speed != -1)
                {
                    for (int i = 0; i < belts.Length; i++)
                    {
                        if (belts[i].speed < bestBelt.speed)
                        {
                            belts[i].speed = bestBelt.speed;
                            self.SyncEntity(belts[i].entityId, bestBelt.entityId);
                        }
                    }
                    // 工作数据
                    foreach (var path in self.cargoTraffic.pathPool)
                    {
                        if (path == null || path.chunks == null) continue;
                        for (int i = 2; i < path.chunks.Length; i += 3)
                            if (path.chunks[i] < bestBelt.speed)
                                path.chunks[i] = bestBelt.speed;
                    }
                }
            }
        }
    }

    public static class Helper
    {
        public static ERecipeType GetRecipeType(this PlanetFactory self, int idx)
        {
            var entity = self.entityPool[idx];
            var proto = LDB.items.Select(entity.protoId);
            if (proto == null || proto.prefabDesc == null) return ERecipeType.None;
            return proto.prefabDesc.assemblerRecipeType;
        }

        public static void SyncEntity(this PlanetFactory self, int idx, int refId)
        {
            // 同步模型
            self.entityPool[idx].modelIndex = self.entityPool[refId].modelIndex;
            self.entityPool[idx].protoId = self.entityPool[refId].protoId;
            // 同步耗电
            int curPower = self.entityPool[idx].powerConId,
                refPower = self.entityPool[refId].powerConId;
            if (curPower > 0 && refPower > 0)
            {
                var powers = self.powerSystem.consumerPool;
                var refPowerCon = powers[refPower];
                powers[curPower].idleEnergyPerTick = refPowerCon.idleEnergyPerTick;
                powers[curPower].workEnergyPerTick = refPowerCon.workEnergyPerTick;
            }
        }
    }
}
