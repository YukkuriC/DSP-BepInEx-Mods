using System;
using System.Collections;
using UnityEngine;
using BepInEx;
using HarmonyLib;
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

        [HarmonyPrefix, HarmonyPatch(typeof(UIStarmap), "OnEnemyClick")]
        static bool TinderWarp(int enemyId, SpaceSector ___spaceSector)
        {
            if (!Input.GetKey(KeyCode.LeftControl)) return true;

            var dest = ___spaceSector.enemyPool[enemyId];
            Helper.PlayerWarp(dest.pos, 0);

            return !Input.GetKey(KeyCode.LeftAlt);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIStarmap), "OnHiveClick")]
        static bool HiveWarp(UIStarmapDFHive hive, SpaceSector ___spaceSector)
        {
            if (!Input.GetKey(KeyCode.LeftControl)) return true;

            var dest = ___spaceSector.astros[hive.hive.hiveAstroId - 1000000].uPos;
            Helper.PlayerWarp(dest, 20000);

            return !Input.GetKey(KeyCode.LeftAlt);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStarmap), "UpdateCursorView")]
        static void WarpTip(UIStarmap __instance, RectTransform ___cursorViewTrans, Text ___cursorViewText)
        {
            var self = __instance;
            var go = ___cursorViewTrans;
            var text = ___cursorViewText;

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

        [HarmonyPrefix, HarmonyPatch(typeof(ABN_MechaPosition), "OnGameTick")]
        static bool NoCheck()
        {
            return false;
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
            var oldStar = GameMain.localStar;

            // 暴力挪transform
            plr.uPosition = target;
            plr.transform.position = target;
            GameMain.data.DetermineLocalPlanet();
            GameMain.data.DetermineRelative();

            // 大小校正
            yield return new WaitForSeconds(0.1f);
            plr.transform.localScale = Vector3.one;

            // 清除原星系恒星
            var newStar = GameMain.localStar;
            if (oldStar != null && oldStar != newStar)
            {
                var starObj = GameMain.universeSimulator.FindStarSimulator(oldStar);
                starObj.massRenderer.gameObject.SetActive(false);
                starObj.atmosRenderer.gameObject.SetActive(false);
                starObj.effect.gameObject.SetActive(false);
            }
        }
    }
}
