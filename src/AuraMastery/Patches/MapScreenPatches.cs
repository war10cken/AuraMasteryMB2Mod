using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using AuraMastery.Config;
using AuraMastery.Core;

namespace AuraMastery.Patches
{
    public static class MapScreenPatches
    {
        private static bool _wasMenuKeyPressed = false;

        // Патчим OnFrameTick на карте. Тип MapScreen ищется рефлексивно,
        // потому что в разных версиях игры (1.x) он находится в разных
        // пространствах имён (TaleWorlds.CampaignSystem.GameState / .ComponentModel),
        // а прямая ссылка ломает сборку (CS0234).
        public static void Apply(Harmony harmony)
        {
            try
            {
                Type mapScreenType = FindMapScreenType();
                if (mapScreenType == null)
                {
                    AuraLogger.Log("MapScreen type not found via reflection; hotkey patch skipped");
                    return;
                }

                MethodInfo tickMethod = AccessTools.Method(mapScreenType, "OnFrameTick");
                if (tickMethod == null)
                {
                    AuraLogger.Log("MapScreen.OnFrameTick not found; hotkey patch skipped");
                    return;
                }

                harmony.Patch(tickMethod,
                    postfix: new HarmonyMethod(typeof(MapScreenPatches), nameof(MapScreenOnFrameTickPostfix)));
                AuraLogger.Log($"Patched {mapScreenType.FullName}.OnFrameTick");
            }
            catch (Exception ex)
            {
                AuraLogger.Log($"MapScreenPatches.Apply ERROR: {ex.Message}");
            }
        }

        private static Type FindMapScreenType()
        {
            var campaignAsm = typeof(CampaignGameStarter).Assembly;
            var mnmAsm = typeof(Mission).Assembly;

            // NOTE: no direct "TaleWorlds.CampaignSystem.GameState.MapScreen" type reference here.
            // MapScreen was moved between namespaces across 1.x patches, so the exact string
            // is resolved at runtime via reflection only (the strings below are not compile-time
            // type references and therefore cannot break the build with CS0234).
            return campaignAsm.GetType("TaleWorlds.CampaignSystem.GameState.MapScreen")
                   ?? campaignAsm.GetType("TaleWorlds.CampaignSystem.ComponentInterfaces.MapScreen")
                   ?? mnmAsm.GetType("TaleWorlds.MountAndBlade.MapScreen")
                   ?? AppDomain.CurrentDomain.GetAssemblies()
                          .Select(a => a.GetTypes().FirstOrDefault(t => t.Name == "MapScreen"))
                          .FirstOrDefault(t => t != null);
        }

        [HarmonyPostfix]
        public static void MapScreenOnFrameTickPostfix()
        {
            CheckMenuKey();
        }

        private static void CheckMenuKey()
        {
            try
            {
                var config = AuraConfigLoader.GetConfig();
                if (config == null) return;

                var openKey = config.GetOpenMenuKey();
                bool keyDown = Input.IsKeyDown(openKey);

                // Проверяем, была ли клавиша только что нажата
                if (keyDown && !_wasMenuKeyPressed)
                {
                    AuraLogger.Log($"OpenMenuKey PRESSED: {openKey}");
                    try
                    {
                        TaleWorlds.CampaignSystem.GameMenus.GameMenu.SwitchToMenu("aura_menu");
                        AuraLogger.Log("SwitchToMenu called successfully");
                    }
                    catch (Exception ex)
                    {
                        AuraLogger.Log($"SwitchToMenu ERROR: {ex.Message}");
                    }
                }

                _wasMenuKeyPressed = keyDown;
            }
            catch (Exception ex)
            {
                // Swallow intentionally: never crash the map tick; log once via AuraLogger.
                AuraLogger.Log($"MapScreenPatches.CheckMenuKey ERROR: {ex.Message}");
            }
        }
    }
}