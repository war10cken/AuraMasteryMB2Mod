using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using AuraMastery.Core;
using AuraMastery.Techniques;

namespace AuraMastery.CampaignBehaviors
{
    public static class AuraMenus
    {
        private const string AuraMenuId = "aura_menu";

        public static void Register(CampaignGameStarter starter)
        {
            AuraLogger.Log("AuraMenus.Register: adding game menu");

            try
            {
                starter.AddGameMenu(AuraMenuId, "Aura Techniques", OnAuraMenuInit);
                AuraLogger.Log($"Game menu '{AuraMenuId}' added");
            }
            catch (Exception ex)
            {
                AuraLogger.Log($"Failed to add game menu: {ex.Message}");
                return;
            }

            try
            {
                // Опции изучения техник
                foreach (AuraTechniqueDefinition technique in AuraTechniqueDefinition.All)
                {
                    starter.AddGameMenuOption(AuraMenuId, $"aura_study_{technique.Id}", $"Study {technique.Name}",
                        args => GetBehavior()?.CanStudyBook(Hero.MainHero, technique) == true,
                        args => OnStudyConsequence(technique), false);

                    starter.AddGameMenuOption(AuraMenuId, $"aura_select_{technique.Id}", $"Select {technique.Name}",
                        args => GetBehavior()?.HasTechnique(Hero.MainHero, technique.Id) == true,
                        args => OnSelectConsequence(technique), false);
                }

                // Отладочная опция
                starter.AddGameMenuOption(AuraMenuId, "aura_debug_give_wave_book", "Debug: Give Aura Wave Book",
                    args => true,
                    args => OnGiveDebugBookConsequence(AuraTechniqueDefinition.WaveStrike.BookItemId), false);

                // Назад
                starter.AddGameMenuOption(AuraMenuId, "aura_back", "Back",
                    args => true,
                    args => GoToMenu("game_menu"), true);

                AuraLogger.Log("AuraMenus.Register: done");
            }
            catch (Exception ex)
            {
                AuraLogger.Log($"AuraMenus.Register ERROR: {ex}");
            }
        }

        private static AuraCampaignBehavior GetBehavior()
        {
            return TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<AuraCampaignBehavior>();
        }

        private static void OnAuraMenuInit(MenuCallbackArgs args)
        {
            AuraLogger.Log("Aura menu init");
        }

        private static void OnStudyConsequence(AuraTechniqueDefinition technique)
        {
            AuraLogger.Log($"Study consequence called for {technique.Id}");
            var behavior = GetBehavior();
            if (behavior != null && behavior.TryStudyBookForTechnique(Hero.MainHero, technique))
            {
                InformationManager.DisplayMessage(new InformationMessage($"Aura technique learned: {technique.Name}"));
            }
            GoToMenu(AuraMenuId);
        }

        private static void OnSelectConsequence(AuraTechniqueDefinition technique)
        {
            AuraLogger.Log($"Select consequence called for {technique.Id}");
            var behavior = GetBehavior();
            if (behavior != null)
            {
                behavior.SetActiveTechnique(Hero.MainHero, technique.Id);
                InformationManager.DisplayMessage(new InformationMessage($"Active aura technique set: {technique.Name}"));
            }
            GoToMenu(AuraMenuId);
        }

        private static void OnGiveDebugBookConsequence(string bookItemId)
        {
            AuraLogger.Log($"Give book consequence called for {bookItemId}");
            var behavior = GetBehavior();
            if (behavior != null)
            {
                behavior.GiveBook(bookItemId);
                InformationManager.DisplayMessage(new InformationMessage($"Debug book added: {bookItemId}"));
            }
            GoToMenu(AuraMenuId);
        }

        private static void GoToMenu(string menuId)
        {
            try
            {
                AuraLogger.Log($"Switching to menu: {menuId}");
                GameMenu.SwitchToMenu(menuId);
            }
            catch (Exception ex)
            {
                AuraLogger.Log($"GoToMenu ERROR: {ex.Message}");
            }
        }
    }
}