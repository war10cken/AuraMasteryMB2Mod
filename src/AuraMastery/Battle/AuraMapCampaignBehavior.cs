using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using AuraMastery.Config;
using AuraMastery.Core;

namespace AuraMastery.Battle
{
    public class AuraMapCampaignBehavior : CampaignBehaviorBase
    {
        private bool _wasMenuKeyPressed = false;
        private int _tickCounter = 0;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AuraLogger.Log("AuraMapCampaignBehavior: Session launched");
        }

        private void OnHourlyTick()
        {
            _tickCounter++;
            
            // Логируем каждые 10 тиков для диагностики
            if (_tickCounter % 10 == 1)
            {
                AuraLogger.Log($"AuraMapCampaignBehavior.OnHourlyTick: tick={_tickCounter}");
            }

            CheckMenuKey();
        }

        public void CheckMenuKey()
        {
            try
            {
                var config = AuraConfigLoader.GetConfig();
                if (config == null) return;

                var openKey = config.GetOpenMenuKey();
                bool keyDown = Input.IsKeyDown(openKey);

                if (keyDown && !_wasMenuKeyPressed)
                {
                    AuraLogger.Log($"OpenMenuKey PRESSED: {openKey}");
                    try
                    {
                        GameMenu.SwitchToMenu("aura_menu");
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
                AuraLogger.Log($"CheckMenuKey ERROR: {ex.Message}");
            }
        }
    }
}