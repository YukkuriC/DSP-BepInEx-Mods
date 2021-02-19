using System;
using System.Collections.Generic;
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

        [HarmonyPrefix, HarmonyPatch(typeof(PlanetTransport), "GameTick")]
        static void RefillWarper(PlanetTransport __instance)
        {
            var self = __instance;

            // valid stations
            var allStations = new List<StationComponent>();
            for (int i = 1; i < self.stationCursor; i++)
            {
                var station = self.stationPool[i];
                if (station == null || station.id != i) continue;
                allStations.Add(station);
            }

            // supply
            var supplyStations = new List<StationStore[]>();
            var supplySlotIds = new List<int>();
            int totalSupply = 0;
            foreach (var station in allStations)
            {
                for (int sid = 0; sid < station.storage.Length; sid++)
                {
                    if (station.storage[sid].itemId != StationComponent.WARPER_ITEMID || station.storage[sid].count <= 0) continue;
                    supplyStations.Add(station.storage);
                    supplySlotIds.Add(sid);
                    totalSupply += station.storage[sid].count;
                }
            }

            // need
            var needStations = new List<StationComponent>();
            foreach (var station in allStations)
            {
                if (station.warperCount < station.warperMaxCount) needStations.Add(station);
            }

            // auto supply
            int warpDrain = needStations.Count;
            if (totalSupply >= warpDrain)
            {
                foreach (var station in needStations) station.warperCount++;
                for (int p = 0; p < supplySlotIds.Count; p++)
                {
                    int dWarp = Math.Min(supplyStations[p][supplySlotIds[p]].count, warpDrain);
                    supplyStations[p][supplySlotIds[p]].count -= dWarp;
                    warpDrain -= dWarp;
                    if (warpDrain <= 0) break;
                }
            }
        }
    }
}
