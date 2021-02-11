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
            "=============================================",
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
            if (dest == GameMain.localPlanet) return true;
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
            if (self.mouseHoverPlanet == null || self.mouseHoverPlanet.planet != GameMain.localPlanet)
                text.text += '\n' + string.Join("\n", ControlInfo);
            text.text += '\r';
            // 更新外框大小
            go.sizeDelta = new Vector2(text.preferredWidth * 0.5f + 44f, text.preferredHeight * 0.5f + 14f);
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
            // 从地面起飞
            if (!plr.sailing)
            {
                plr.movementState = EMovementState.Sail;
                plr.warpState = 1; // 鸽子扭脖子螺旋桨.gif
                var dir = (target - plr.uPosition).normalized;
                plr.uPosition = GameMain.localPlanet.uPosition + dir * (GameMain.localPlanet.realRadius * 1.5f);
                yield return new WaitForSeconds(0.1f);
            }
            plr.uPosition = target; // 写入坐标

            // 大小校正
            yield return new WaitForSeconds(0.1f);
            plr.transform.localScale = Vector3.one;
        }
    }
}
