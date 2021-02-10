using System;
using System.Collections;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace DSPMod
{
    [BepInPlugin(UID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public partial class ModMain : BaseUnityPlugin
    {
        static string[] ControlInfo = {
            "按住左Ctrl单击传送至该天体",
            "按住左Ctrl+Alt单击可在不聚焦视野的同时连续传送",
        };

        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(ModMain));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIStarmap), "OnPlanetClick")]
        static bool PlanetWarp(UIStarmapPlanet planet)
        {
            if (!Input.GetKey(KeyCode.LeftControl)) return true;

            var dest = planet.planet;
            Helper.PlayerWarp(dest.uPosition, dest.realRadius);

            return !Input.GetKey(KeyCode.LeftAlt);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIStarmap), "OnStarClick")]
        static bool StarWarp(UIStarmapStar star)
        {
            if (!Input.GetKey(KeyCode.LeftControl)) return true;

            var dest = star.star;
            Helper.PlayerWarp(dest.uPosition, dest.physicsRadius * 1.5f);

            return !Input.GetKey(KeyCode.LeftAlt);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStarmap), "UpdateCursorView")]
        static void WarpTip(UIStarmap __instance)
        {
            var self = __instance;
            var go = Traverse.Create(self).Field<RectTransform>("cursorViewTrans").Value;
            var text = Traverse.Create(self).Field<Text>("cursorViewText").Value;

            if (!go.gameObject.activeSelf) return;

            // 判断是否已添加说明
            if (text.text.Length > 0 && text.text[text.text.Length - 1] == '\r') return;

            // 添加说明
            text.text += '\n' + string.Join("\n", ControlInfo);
            text.text += '\r';
        }
    }

    public static class Helper
    {
        public static VectorLF3 RandSphere(float radius)
        {
            var randPos = UnityEngine.Random.onUnitSphere * radius;
            return new VectorLF3(randPos.x, randPos.y, randPos.z);
        }

        public static void PlayerWarp(VectorLF3 center, float radius)
        {
            var target = center + Helper.RandSphere(radius);
            var plr = GameMain.data.mainPlayer;
            UIRoot.instance                                   // 随手薅的一个MonoBehavior_(:з」∠)_
                .StartCoroutine(WarpFromGround(plr, target)); // 挂载传送协程
        }

        // 协程传送
        static IEnumerator WarpFromGround(Player plr, VectorLF3 target)
        {
            if (!plr.sailing) // 起飞+短暂停顿
            {
                plr.movementState = EMovementState.Sail;
                plr.warpState = 1; // 鸽子扭脖子螺旋桨.gif
                plr.uPosition = (target + plr.uPosition) / 2;
                plr.uVelocity = (target - plr.uPosition).normalized * 2000;
                yield return new WaitForSeconds(0.2f);
            }
            plr.uPosition = target; // 写入坐标
        }
    }
}
