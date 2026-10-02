using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.InputSystem;
using AuraMastery.CampaignBehaviors;
using AuraMastery.Techniques;
using AuraMastery.Config;

namespace AuraMastery.Battle
{
    public class AuraMissionBehavior : MissionBehavior
    {
        private bool _wasKeyDown;
        private bool _isCharging;
        private float _charge;
        private float _cooldown;
        
        private AuraConfig _config;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Logic;
        
        public override void OnBehaviorInitialize()
        {
            _config = AuraConfigLoader.GetConfig();
        }

        public override void OnPreMissionTick(float dt)
        {
            try 
            { 
                Tick(dt); 
            } 
            catch { }
        }

        private void Tick(float dt)
        {
            if (dt <= 0f) return;
            if (_cooldown > 0f) _cooldown -= dt;
            
            if (_config == null)
            {
                _config = AuraConfigLoader.GetConfig();
            }

            bool keyDown = _config.IsActivateKeyDown();
            Mission mission = Mission;
            Agent agent = mission?.MainAgent;

            if (mission == null || agent == null || agent.Health <= 0f)
            {
                ResetState(keyDown);
                return;
            }

            Hero hero = GetHero(agent);
            var behavior = TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<AuraCampaignBehavior>();

            if (hero == null || behavior == null)
            {
                ResetState(keyDown);
                return;
            }

            HandleTechniqueSwitch(hero, behavior);

            string activeTechniqueId = behavior.GetActiveTechniqueId(hero);
            AuraTechniqueDefinition technique = AuraTechniqueDefinition.GetById(activeTechniqueId);

            if (technique == null || !behavior.HasTechnique(hero, technique.Id) || behavior.GetAuraLevel(hero) < technique.RequiredAuraLevel)
            {
                ResetState(keyDown);
                return;
            }

            if (keyDown && !_wasKeyDown && _cooldown <= 0f)
            {
                _isCharging = true;
                _charge = 0f;
            }

            if (_isCharging && keyDown)
            {
                _charge = Math.Min(_charge + dt, technique.MaxChargeSeconds);
            }

            if (!keyDown && _wasKeyDown && _isCharging)
            {
                float chargeRatio = technique.MaxChargeSeconds <= 0f ? 0f : _charge / technique.MaxChargeSeconds;
                if (chargeRatio > 0.05f)
                {
                    Release(agent, technique, chargeRatio);
                    behavior.AddAuraXp(hero, 5f + 20f * chargeRatio);
                }
                _cooldown = technique.CooldownSeconds;
                ResetState(false);
            }
            else
            {
                _wasKeyDown = keyDown;
            }
        }
        
        private void HandleTechniqueSwitch(Hero hero, AuraCampaignBehavior behavior)
        {
            if (hero == null || behavior == null) return;

            if (_config.IsSwitchTechnique1Pressed())
            {
                if (behavior.HasTechnique(hero, "WaveStrike"))
                {
                    behavior.SetActiveTechnique(hero, "WaveStrike");
                    InformationManager.DisplayMessage(new InformationMessage("Active: Aura Wave"));
                }
            }

            if (_config.IsSwitchTechnique2Pressed())
            {
                if (behavior.HasTechnique(hero, "AuraBlast"))
                {
                    behavior.SetActiveTechnique(hero, "AuraBlast");
                    InformationManager.DisplayMessage(new InformationMessage("Active: Aura Blast"));
                }
            }
        }

        private void ResetState(bool keyDown)
        {
            _wasKeyDown = keyDown;
            _isCharging = false;
            _charge = 0f;
        }

        private Hero GetHero(Agent agent)
        {
            if (agent?.Character is CharacterObject characterObject)
            {
                return characterObject.HeroObject;
            }
            return null;
        }

        private void Release(Agent caster, AuraTechniqueDefinition technique, float chargeRatio)
        {
            float radius = technique.BaseRadius + technique.RadiusPerCharge * chargeRatio;
            float damage = (technique.BaseDamage + technique.DamagePerCharge * chargeRatio) * _config.DamageMultiplier;
            Vec3 forward = caster.Frame.rotation.f;

            TrySpawnEffect(caster, forward, technique);
            ApplyDamage(caster, forward, technique, radius, damage);
        }

        private void ApplyDamage(Agent caster, Vec3 forward, AuraTechniqueDefinition technique, float radius, float damage)
        {
            Mission mission = Mission;
            if (mission == null) return;

            bool useCone = technique.ConeAngleDegrees < 359f;
            float minDot = useCone ? (float)Math.Cos(technique.ConeAngleDegrees * Math.PI / 180.0) : -1f;

            foreach (Agent target in mission.Agents)
            {
                if (target == null || target == caster || target.Health <= 0f) continue;
                if (target.Team != null && caster.Team != null && target.Team == caster.Team) continue;

                Vec3 diff = target.Position - caster.Position;
                float distance = (float)Math.Sqrt(diff.x * diff.x + diff.y * diff.y + diff.z * diff.z);

                if (distance <= 0.01f || distance > radius) continue;

                if (useCone)
                {
                    float dot = (forward.x * diff.x + forward.y * diff.y + forward.z * diff.z) / distance;
                    if (dot < minDot) continue;
                }

                float falloff = Math.Max(0.2f, Math.Min(1f, 1f - distance / radius));
                float finalDamage = damage * falloff;

                if (finalDamage <= 0f) continue;

                try
                {
                    target.Health -= finalDamage;
                }
                catch { }
            }
        }

        private void TrySpawnEffect(Agent caster, Vec3 forward, AuraTechniqueDefinition technique)
        {
            if (string.IsNullOrEmpty(technique.ParticlePrefab)) return;
            try
            {
                MatrixFrame frame = caster.Frame;
                frame.origin = caster.Position + forward * 1.5f;
                GameEntity.Instantiate(Mission.Current.Scene, technique.ParticlePrefab, frame);
            }
            catch { }
        }
    }
}