using System;
using System.Runtime.CompilerServices;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using AuraMastery.Battle;
using AuraMastery.Core;

namespace AuraMastery.Patches
{
    public static class MissionPatches
    {
        private static readonly ConditionalWeakTable<Mission, object> _initialized = new ConditionalWeakTable<Mission, object>();
        private static DateTime _lastLogTime = DateTime.MinValue;
        private static int _tickCount = 0;

        public static void MissionTickPostfix(Mission __instance)
        {
            _tickCount++;

            if ((DateTime.Now - _lastLogTime).TotalSeconds >= 10)
            {
                _lastLogTime = DateTime.Now;
                bool isCampaign = Game.Current?.GameType is TaleWorlds.CampaignSystem.Campaign;
                AuraLogger.Log($"Mission.Tick called (count={_tickCount}, type={__instance?.GetType().Name}, isCampaign={isCampaign})");
            }

            EnsureAuraBehavior(__instance);
        }

        private static void EnsureAuraBehavior(Mission mission)
        {
            if (mission == null) return;

            if (_initialized.TryGetValue(mission, out _)) return;

            _initialized.Add(mission, new object());

            try
            {
                mission.AddMissionBehavior(new AuraMissionBehavior());
                AuraLogger.Log($"AuraMissionBehavior added to mission: {mission.GetType().Name}");
            }
            catch (Exception ex)
            {
                AuraLogger.Log($"MissionPatches.EnsureAuraBehavior ERROR: {ex}");
            }
        }
    }
}