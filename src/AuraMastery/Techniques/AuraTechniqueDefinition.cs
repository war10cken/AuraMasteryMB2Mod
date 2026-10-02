using System.Collections.Generic;
using System.Linq;

namespace AuraMastery.Techniques
{
    public class AuraTechniqueDefinition
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }

        public string BookItemId { get; private set; }

        public float MaxChargeSeconds { get; private set; }
        public float CooldownSeconds { get; private set; }

        public float BaseRadius { get; private set; }
        public float RadiusPerCharge { get; private set; }

        public float BaseDamage { get; private set; }
        public float DamagePerCharge { get; private set; }

        public float ConeAngleDegrees { get; private set; }

        public int RequiredAuraLevel { get; private set; }

        public string ParticlePrefab { get; private set; }
        public string SoundEvent { get; private set; }

        public AuraTechniqueDefinition(
            string id,
            string name,
            string description,
            string bookItemId,
            float maxChargeSeconds,
            float cooldownSeconds,
            float baseRadius,
            float radiusPerCharge,
            float baseDamage,
            float damagePerCharge,
            float coneAngleDegrees,
            int requiredAuraLevel,
            string particlePrefab,
            string soundEvent)
        {
            Id = id;
            Name = name;
            Description = description;
            BookItemId = bookItemId;

            MaxChargeSeconds = maxChargeSeconds;
            CooldownSeconds = cooldownSeconds;

            BaseRadius = baseRadius;
            RadiusPerCharge = radiusPerCharge;

            BaseDamage = baseDamage;
            DamagePerCharge = damagePerCharge;

            ConeAngleDegrees = coneAngleDegrees;
            RequiredAuraLevel = requiredAuraLevel;

            ParticlePrefab = particlePrefab;
            SoundEvent = soundEvent;
        }

        public static readonly AuraTechniqueDefinition WaveStrike = new AuraTechniqueDefinition(
            id: "WaveStrike",
            name: "Aura Wave",
            description: "Releases a horizontal wave of aura energy in front of the player.",
            bookItemId: "Book_AuraWave",
            maxChargeSeconds: 1.5f,
            cooldownSeconds: 6f,
            baseRadius: 4f,
            radiusPerCharge: 6f,
            baseDamage: 15f,
            damagePerCharge: 55f,
            coneAngleDegrees: 120f,
            requiredAuraLevel: 1,
            particlePrefab: "",
            soundEvent: ""
        );

        public static readonly AuraTechniqueDefinition AuraBlast = new AuraTechniqueDefinition(
            id: "AuraBlast",
            name: "Aura Blast",
            description: "Releases a circular burst of aura energy around the player.",
            bookItemId: "Book_AuraBlast",
            maxChargeSeconds: 2.5f,
            cooldownSeconds: 12f,
            baseRadius: 3f,
            radiusPerCharge: 4f,
            baseDamage: 25f,
            damagePerCharge: 80f,
            coneAngleDegrees: 360f,
            requiredAuraLevel: 5,
            particlePrefab: "",
            soundEvent: ""
        );

        public static readonly IReadOnlyList<AuraTechniqueDefinition> All =
            new List<AuraTechniqueDefinition>
            {
                WaveStrike,
                AuraBlast
            }.AsReadOnly();

        public static AuraTechniqueDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return All.FirstOrDefault(t => t.Id == id);
        }
    }
}