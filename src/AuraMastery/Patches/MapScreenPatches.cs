using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;
using AuraMastery.Config;
using AuraMastery.Core;

namespace AuraMastery.Patches
{
    public static class MapScreenPatches
    {
        private static bool _wasMenuKeyPressed = false;

        // Патчим MapScreen.OnFrameTick - вызывается каждый кадр на карте
        [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.GameState.MapScreen), "OnFrameTick")]
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
                // Игнорируем, чтобы не ронять игру
            }
        }
    }
}