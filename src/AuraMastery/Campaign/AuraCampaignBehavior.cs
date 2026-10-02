using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

using AuraMastery.Core;
using AuraMastery.Techniques;

namespace AuraMastery.CampaignBehaviors
{
    public class AuraCampaignBehavior : CampaignBehaviorBase
    {
        private List<string> _knownKeys = new List<string>();
        private List<string> _activeKeys = new List<string>();
        private List<string> _progressKeys = new List<string>();

        public override void RegisterEvents()
        {
            AuraLogger.Log("AuraCampaignBehavior.RegisterEvents");
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("aura_known_keys", ref _knownKeys);
            dataStore.SyncData("aura_active_keys", ref _activeKeys);
            dataStore.SyncData("aura_progress_keys", ref _progressKeys);

            _knownKeys ??= new List<string>();
            _activeKeys ??= new List<string>();
            _progressKeys ??= new List<string>();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AuraLogger.Log("AuraCampaignBehavior.OnSessionLaunched");
            try
            {
                AuraMenus.Register(starter);
                AuraLogger.Log("AuraMenus.Register completed");
            }
            catch (Exception ex)
            {
                AuraLogger.Log($"AuraCampaignBehavior.OnSessionLaunched ERROR: {ex}");
            }
        }

        #region Helpers

        private static string MakeKey(Hero hero, string techniqueId)
        {
            return $"{hero.StringId}|{techniqueId}";
        }

        private static ItemObject GetItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            try
            {
                return MBObjectManager.Instance?.GetObject<ItemObject>(itemId);
            }
            catch
            {
                return null;
            }
        }

        private static MobileParty PlayerParty =>
            MobileParty.MainParty ?? Hero.MainHero?.PartyBelongedTo;

        private static string MakePrefix(Hero hero)
        {
            return hero.StringId + "|";
        }

        #endregion

        #region Known techniques

        public bool HasTechnique(Hero hero, string techniqueId)
        {
            if (hero == null || string.IsNullOrEmpty(techniqueId))
            {
                return false;
            }

            return _knownKeys.Contains(MakeKey(hero, techniqueId));
        }

        public void LearnTechnique(Hero hero, string techniqueId)
        {
            if (hero == null || string.IsNullOrEmpty(techniqueId))
            {
                return;
            }

            if (!HasTechnique(hero, techniqueId))
            {
                _knownKeys.Add(MakeKey(hero, techniqueId));
            }
        }

        public List<AuraTechniqueDefinition> GetKnownTechniques(Hero hero)
        {
            var result = new List<AuraTechniqueDefinition>();

            if (hero == null)
            {
                return result;
            }

            foreach (var technique in AuraTechniqueDefinition.All)
            {
                if (HasTechnique(hero, technique.Id))
                {
                    result.Add(technique);
                }
            }

            return result;
        }

        #endregion

        #region Active technique

        public string GetActiveTechniqueId(Hero hero)
        {
            if (hero == null)
            {
                return null;
            }

            string prefix = MakePrefix(hero);
            string key = _activeKeys.FirstOrDefault(k => k.StartsWith(prefix, StringComparison.Ordinal));

            if (key == null)
            {
                return null;
            }

            return key.Substring(prefix.Length);
        }

        public void SetActiveTechnique(Hero hero, string techniqueId)
        {
            if (hero == null || string.IsNullOrEmpty(techniqueId))
            {
                return;
            }

            string prefix = MakePrefix(hero);

            _activeKeys.RemoveAll(k => k.StartsWith(prefix, StringComparison.Ordinal));
            _activeKeys.Add(MakeKey(hero, techniqueId));
        }

        #endregion

        #region Aura level and XP

        public float GetAuraXp(Hero hero)
        {
            if (hero == null)
            {
                return 0f;
            }

            string prefix = MakePrefix(hero);
            string key = _progressKeys.FirstOrDefault(k => k.StartsWith(prefix, StringComparison.Ordinal));

            if (key == null)
            {
                return 0f;
            }

            int separatorIndex = key.IndexOf('|');
            if (separatorIndex < 0 || separatorIndex + 1 >= key.Length)
            {
                return 0f;
            }

            string value = key.Substring(separatorIndex + 1);

            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float xp))
            {
                return xp;
            }

            return 0f;
        }

        public int GetAuraLevel(Hero hero)
        {
            float xp = GetAuraXp(hero);

            // 1 level at 0 XP, then +1 level per 100 XP.
            int level = 1 + (int)(xp / 100f);

            return Math.Max(1, level);
        }

        public void AddAuraXp(Hero hero, float xpToAdd)
        {
            if (hero == null || xpToAdd <= 0f)
            {
                return;
            }

            float currentXp = GetAuraXp(hero);
            float newXp = currentXp + xpToAdd;

            string prefix = MakePrefix(hero);
            _progressKeys.RemoveAll(k => k.StartsWith(prefix, StringComparison.Ordinal));
            _progressKeys.Add($"{hero.StringId}|{newXp.ToString(CultureInfo.InvariantCulture)}");

            TryAddNativeSkillXp(hero, xpToAdd);
        }

        private void TryAddNativeSkillXp(Hero hero, float xpToAdd)
        {
            try
            {
                SkillObject skill = AuraSkill.Skill;
                if (skill != null && hero.HeroDeveloper != null)
                {
                    hero.HeroDeveloper.AddSkillXp(skill, xpToAdd);
                }
            }
            catch { }
        }

        #endregion

        #region Books

        public bool CanStudyBook(Hero hero, AuraTechniqueDefinition technique)
        {
            if (hero == null || technique == null)
            {
                return false;
            }

            if (HasTechnique(hero, technique.Id))
            {
                return false;
            }

            if (GetAuraLevel(hero) < technique.RequiredAuraLevel)
            {
                return false;
            }

            return PartyHasItem(technique.BookItemId, 1);
        }

        public bool TryStudyBookForTechnique(Hero hero, AuraTechniqueDefinition technique)
        {
            if (!CanStudyBook(hero, technique))
            {
                return false;
            }

            RemovePartyItem(technique.BookItemId, 1);
            LearnTechnique(hero, technique.Id);

            if (string.IsNullOrEmpty(GetActiveTechniqueId(hero)))
            {
                SetActiveTechnique(hero, technique.Id);
            }

            AddAuraXp(hero, 25f);

            return true;
        }

        public void GiveBook(string itemId)
        {
            MobileParty party = PlayerParty;
            ItemObject item = GetItem(itemId);

            if (party == null || item == null)
            {
                return;
            }

            party.ItemRoster.AddToCounts(item, 1);
        }

        private static bool PartyHasItem(string itemId, int count)
        {
            MobileParty party = PlayerParty;
            ItemObject item = GetItem(itemId);

            if (party == null || item == null || count <= 0)
            {
                return false;
            }

            int amount = 0;

            foreach (ItemRosterElement element in party.ItemRoster)
            {
                if (element.EquipmentElement.Item == item)
                {
                    amount += element.Amount;
                }
            }

            return amount >= count;
        }

        private static bool RemovePartyItem(string itemId, int count)
        {
            MobileParty party = PlayerParty;
            ItemObject item = GetItem(itemId);

            if (party == null || item == null || count <= 0)
            {
                return false;
            }

            // In some Bannerlord builds negative AddToCounts is allowed.
            // If your build does not support it, iterate ItemRoster and remove manually.
            party.ItemRoster.AddToCounts(item, -count);

            return true;
        }

        #endregion
    }
}