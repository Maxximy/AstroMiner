using Unity.Entities;

namespace ECS.Components
{
    /// <summary>
    /// Singleton: written by InputBridge each frame (keyboard + UI button activation).
    /// Skill systems consume these flags, then SkillCooldownSystem resets them at end of frame.
    /// </summary>
    public struct SkillInputData : IComponentData
    {
        public bool Skill1Pressed;  // Laser Burst (key 1)
        public bool Skill2Pressed;  // Chain Lightning (key 2)
        public bool Skill3Pressed;  // EMP Pulse (key 3)
        public bool Skill4Pressed;  // Overcharge (key 4)
    }

    /// <summary>
    /// Singleton: tracks remaining and max cooldown for each skill.
    /// Decremented by SkillCooldownSystem each frame.
    /// </summary>
    public struct SkillCooldownData : IComponentData
    {
        public float Skill1Remaining; public float Skill1MaxCooldown; // Laser: 8s
        public float Skill2Remaining; public float Skill2MaxCooldown; // Chain: 10s
        public float Skill3Remaining; public float Skill3MaxCooldown; // EMP: 12s
        public float Skill4Remaining; public float Skill4MaxCooldown; // Overcharge: 15s
    }

    /// <summary>
    /// Singleton: crit chance and multiplier (read by all damage systems).
    /// </summary>
    public struct CritConfigData : IComponentData
    {
        public float CritChance;     // 0.08 (8%)
        public float CritMultiplier; // 2.0
    }

    /// <summary>
    /// Singleton: tracks active overcharge buff.
    /// If RemainingDuration > 0, buff is active. MiningDamageSystem reads this.
    /// </summary>
    public struct OverchargeBuffData : IComponentData
    {
        public float RemainingDuration;  // 0 = no buff
        public float DamageMultiplier;   // 2.0 when active
        public float RadiusMultiplier;   // 1.5 when active
    }

    /// <summary>
    /// Per-entity component: added to burning asteroids by Laser Burst and EMP Pulse systems.
    /// BurningDamageSystem ticks damage and removes when expired.
    /// </summary>
    public struct BurningData : IComponentData
    {
        public float DamagePerTick;
        public float TickInterval;
        public float RemainingDuration;
        public float TickAccumulator;
    }
}
