using System;
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

        [HarmonyPatch(typeof(PlayerAction_Build), "CreatePrebuilds")]
        [HarmonyPrefix]
        static void AutoClickWhenRelease(PlayerAction_Build __instance)
        {
            var self = __instance;
            var cmd = self.controller.cmd;

            if (!(cmd.mode == 3
                && cmd.stage == 1)) return;
            if (VFInput.rtsConfirm.onUp)
            {
                var axis = Traverse.Create(typeof(VFInput)).Field("axis_button").GetValue<VFInput.InputAxis>();
                axis.down[0] = true;
            }
        }
    }
}
