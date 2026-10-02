using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using AuraMastery.Battle;
using AuraMastery.CampaignBehaviors;
using AuraMastery.Config;
using AuraMastery.Core;

namespace AuraMastery
{
    public class SubModule : MBSubModuleBase
    {
        private const string HarmonyId = "com.auramastery.mod";
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            AuraLogger.Log("SubModule.OnSubModuleLoad START");
            try
            {
                _harmony = new Harmony(HarmonyId);
                PatchMissionInjection();
                // Патч MapScreen ищем рефлексивно (тип переезжал между namespace'ами в 1.x)
                Patches.MapScreenPatches.Apply(_harmony);
                // НЕ вызываем PatchAll — патчим только то, что нужно
                AuraLogger.Log("SubModule.OnSubModuleLoad SUCCESS");
            }
            catch (Exception ex)
            {
                AuraLogger.Log($"SubModule.OnSubModuleLoad ERROR: {ex}");
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            AuraLogger.Log("SubModule.OnSubModuleUnloaded");
            _harmony?.UnpatchAll(HarmonyId);
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            AuraLogger.Log($"SubModule.OnGameStart: GameType={game?.GameType?.GetType().Name}, Starter={gameStarterObject?.GetType().Name}");
            try
            {
                AuraConfigLoader.ForceCreateConfig();
                AuraLogger.Log($"Config path: {AuraConfigLoader.GetConfigPath()}");

                if (game.GameType is TaleWorlds.CampaignSystem.Campaign && gameStarterObject is CampaignGameStarter campaignStarter)
                {
                    campaignStarter.AddBehavior(new AuraCampaignBehavior());
                    campaignStarter.AddBehavior(new AuraMapCampaignBehavior());
                    AuraLogger.Log("AuraCampaignBehavior and AuraMapCampaignBehavior added");
                }
            }
            catch (Exception ex)
            {
                AuraLogger.Log($"SubModule.OnGameStart ERROR: {ex}");
            }
        }

        private void PatchMissionInjection()
        {
            try
            {
                MethodInfo tickMethod = AccessTools.Method(typeof(Mission), "Tick", new[] { typeof(float) });
                if (tickMethod != null)
                {
                    _harmony.Patch(tickMethod, postfix: new HarmonyMethod(typeof(Patches.MissionPatches), nameof(Patches.MissionPatches.MissionTickPostfix)));
                    AuraLogger.Log("Patched Mission.Tick (float)");
                }
                else
                {
                    AuraLogger.Log("Mission.Tick (float) not found");
                }
            }
            catch (Exception ex)
            {
                AuraLogger.Log($"PatchMissionInjection ERROR: {ex.Message}");
            }
        }
    }
}