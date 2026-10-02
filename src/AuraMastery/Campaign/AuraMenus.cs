using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using AuraMastery.Core;
using AuraMastery.Techniques;

namespace AuraMastery.CampaignBehaviors
{
    public static class AuraMenus
    {
        private const string AuraMenuId = "aura_menu";

        // Cooldown storage to prevent infinite book farming from the settlement menu.
        // Key: technique id, value: last purchase time as elapsed game days since campaign start.
        // Verified against TaleWorlds.CampaignSystem.dll metadata (1.3.15): CampaignTime has no
        // "Value"/"Value64" members; ElapsedDaysUntilNow is an INSTANCE property (float), so it
        // must be read via CampaignTime.Now.ElapsedDaysUntilNow.
        private static readonly System.Collections.Generic.Dictionary<string, float> _lastBookGivenDays =
            new System.Collections.Generic.Dictionary<string, float>();

        // Books are not available during the first in-game year after session start.
        private const float BookGiveCooldownDays = 365f;

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
                    // Local copy for stable closure capture.
                    AuraTechniqueDefinition techLocal = technique;

                    starter.AddGameMenuOption(AuraMenuId, $"aura_study_{techLocal.Id}", $"Study {techLocal.Name}",
                        args => GetBehavior()?.CanStudyBook(Hero.MainHero, techLocal) == true,
                        args => OnStudyConsequence(techLocal), false);

                    starter.AddGameMenuOption(AuraMenuId, $"aura_select_{techLocal.Id}", $"Select {techLocal.Name}",
                        args => GetBehavior()?.HasTechnique(Hero.MainHero, techLocal.Id) == true,
                        args => OnSelectConsequence(techLocal), false);
                }

                // Отладочная опция
                starter.AddGameMenuOption(AuraMenuId, "aura_debug_give_wave_book", "Debug: Give Aura Wave Book",
                    args => true,
                    args => OnGiveDebugBookConsequence(AuraTechniqueDefinition.WaveStrike.BookItemId), false);

                // Back
                starter.AddGameMenuOption(AuraMenuId, "aura_back", "Back",
                    args => true,
                    args => GoToMenu("game_menu"), true);

                RegisterSettlementMenuOptions(starter);

                AuraLogger.Log("AuraMenus.Register: done");
            }
            catch (Exception ex)
            {
                AuraLogger.Log($"AuraMenus.Register ERROR: {ex}");
            }
        }

        #region Settlement menu options

        // Adds a line to the settlement menus ("town" and "castle") that gives
        // the player an aura tome, following the same AddGameMenuOption pattern
        // as the ArtisanBeer mod (see its AddWorkshopButton method):
        //   - showIfCantChoose: false (option hidden when the condition fails),
        //   - disabled: false,
        //   - sortPriority: 9 (same slot as ArtisanBeer's brewery button).
        // The current settlement is resolved via Settlement.CurrentSettlement,
        // exactly like ArtisanBeer does it.
        private static void RegisterSettlementMenuOptions(CampaignGameStarter starter)
        {
            int optionIndex = 0;
            foreach (string menuId in new[] { "town", "castle" })
            {
                foreach (AuraTechniqueDefinition technique in AuraTechniqueDefinition.All)
                {
                    string optionId = $"aura_give_book_{technique.Id}_{menuId}_{optionIndex++}";

                    // One-time init of the cooldown timer so books are not free on day one.
                    if (!_lastBookGivenDays.ContainsKey(technique.Id))
                    {
                        _lastBookGivenDays[technique.Id] = 0f;
                    }

                    // Local copy for stable closure capture.
                    AuraTechniqueDefinition techLocal = technique;

                    // TextObject has no operator+ (only op_Equality/op_Inequality exist,
                    // verified against TaleWorlds.Localization.dll metadata). Build the option
                    // label by mutating a single TextObject via SetTextVariable("TAG", value)
                    // and referencing the {TAG} token inside the localized string.
                    // AddGameMenuOption takes optionText as String (verified signature), so the
                    // finished TextObject is rendered with ToString().
                    starter.AddGameMenuOption(menuId, optionId,
                        GetBuyTomeText(techLocal).ToString(),
                        args =>
                        {
                            // Submenu leave-type keeps the player inside the settlement menu context.
                            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                            return CanGiveBookFromSettlement(Settlement.CurrentSettlement, techLocal);
                        },
                        args => OnGiveBookFromSettlementConsequence(Settlement.CurrentSettlement, techLocal),
                        isLeave: false);
                }
            }
        }

        // Cached TextObject per technique for the settlement menu option label.
        // SetTextVariable returns void, so the object is prepared once and reused.
        private static readonly System.Collections.Generic.Dictionary<string, TextObject> _buyTomeTexts =
            new System.Collections.Generic.Dictionary<string, TextObject>();

        private static TextObject GetBuyTomeText(AuraTechniqueDefinition technique)
        {
            if (!_buyTomeTexts.TryGetValue(technique.Id, out TextObject text))
            {
                text = new TextObject("{=auraTownGiveBook}Buy Tome: {TECHNAME}");
                text.SetTextVariable("TECHNAME", technique.Name);
                _buyTomeTexts[technique.Id] = text;
            }
            return text;
        }

        private static bool CanGiveBookFromSettlement(Settlement settlement, AuraTechniqueDefinition technique)
        {
            if (settlement == null || technique == null)
            {
                return false;
            }

            // Books are only sold in towns and castles with markets.
            if (!(settlement.IsTown || settlement.IsCastle))
            {
                return false;
            }

            // Do not offer a book for an already known technique.
            if (GetBehavior()?.HasTechnique(Hero.MainHero, technique.Id) == true)
            {
                return false;
            }

            // Cooldown: one copy per technique per BookGiveCooldownDays game days.
            // ElapsedDaysUntilNow is an instance property on CampaignTime (verified in metadata),
            // read through the static CampaignTime.Now snapshot; value is elapsed game days
            // since campaign start.
            float lastGivenDays = _lastBookGivenDays.TryGetValue(technique.Id, out float t) ? t : 0f;
            float elapsedDays = CampaignTime.Now.ElapsedDaysUntilNow - lastGivenDays;
            return elapsedDays >= BookGiveCooldownDays;
        }

        private static void OnGiveBookFromSettlementConsequence(Settlement settlement, AuraTechniqueDefinition technique)
        {
            AuraLogger.Log($"Settlement book consequence called for {technique.Id}");

            var behavior = GetBehavior();
            if (behavior == null || Hero.MainHero == null)
            {
                return;
            }

            int price = technique.RequiredAuraLevel * 1500;
            if (Hero.MainHero.Gold < price)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Not enough gold: the tome costs {price}"));
                return;
            }

            Hero.MainHero.ChangeHeroGold(-price);
            behavior.GiveBook(technique.BookItemId);
            // Store the purchase moment as elapsed game days since campaign start.
            _lastBookGivenDays[technique.Id] = CampaignTime.Now.ElapsedDaysUntilNow;
            InformationManager.DisplayMessage(new InformationMessage(
                $"Purchased '{technique.Name}' tome for {price} gold"));
        }

        #endregion

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
