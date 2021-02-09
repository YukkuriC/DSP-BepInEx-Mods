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
                // 修复错乱制造机
                self.FixAssemblers();
                self.FixGhosts();

                // 寻找最优制造机并替换
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
                        => self.SyncEntity(idx, LDB.items.Select(self.entityPool[refId].protoId));

        public static void SyncEntity(this PlanetFactory self, int idx, ItemProto proto)
        {
            var prefab = proto.prefabDesc;
            // 同步模型
            self.entityPool[idx].modelIndex = (short)prefab.modelIndex;
            self.entityPool[idx].protoId = (short)proto.ID;
            // 同步耗电
            int curPower = self.entityPool[idx].powerConId;
            if (curPower > 0)
            {
                var powers = self.powerSystem.consumerPool;
                powers[curPower].idleEnergyPerTick = prefab.idleEnergyPerTick;
                powers[curPower].workEnergyPerTick = prefab.workEnergyPerTick;
            }
        }
    }

    public static class SaveFixer
    {
        static Dictionary<ERecipeType, ItemProto> mapper;

        public static void InitMapper()
        {
            mapper = new Dictionary<ERecipeType, ItemProto>();
            foreach (var proto in LDB.items.dataArray)
            {
                if (proto == null || proto.prefabDesc == null) continue;
                var prefab = proto.prefabDesc;
                var recipe = prefab.assemblerRecipeType;
                if (recipe == ERecipeType.None) continue;

                bool toChoose = true;
                if (mapper.ContainsKey(recipe))
                {
                    var curr = mapper[recipe];
                    if (curr.prefabDesc.assemblerSpeed < prefab.assemblerSpeed)
                        toChoose = false;
                }
                if (toChoose) mapper[recipe] = proto;
            }
        }

        public static void FixAssemblers(this PlanetFactory self)
        {
            if (mapper == null) InitMapper();
            var assemblers = self.factorySystem.assemblerPool;
            for (int i = 0; i < assemblers.Length; i++)
            {
                var eid = assemblers[i].entityId;
                var shouldRecipe = assemblers[i].recipeType; // 错乱前保有配方为需求类型
                var currRecipe = self.GetRecipeType(eid);    // 原型配方为当前类型
                if (shouldRecipe == ERecipeType.None
                 || currRecipe == ERecipeType.None
                 || shouldRecipe == currRecipe) continue;
                // 修复建筑
                var targetProto = mapper[shouldRecipe];
                assemblers[i].speed = targetProto.prefabDesc.assemblerSpeed;
                self.SyncEntity(eid, targetProto);
            }
        }

        public static void FixGhosts(this PlanetFactory self)
        {
            if (mapper == null) InitMapper();
            var assemblers = self.factorySystem.assemblerPool;
            for (int i = 0; i < assemblers.Length; i++)
            {
                var eid = assemblers[i].entityId;
                if (self.entityPool[eid].modelIndex == 0) // 模型缺失
                {
                    self.RemoveEntityWithComponents(eid);
                }
            }
        }
    }
}
