using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Systems
{
    /// <summary>
    /// Decrements skill cooldown timers each frame and resets SkillInputData flags
    /// after all skill systems have consumed them.
    /// Runs before all skill systems to ensure cooldowns are up-to-date.
    /// Resets input flags at end of OnUpdate so presses are consumed exactly once.
    /// </summary>
    [BurstCompile]
    [UpdateBefore(typeof(LaserBurstSystem))]
    [UpdateBefore(typeof(ChainLightningSystem))]
    [UpdateBefore(typeof(EmpPulseSystem))]
    [UpdateBefore(typeof(OverchargeSystem))]
    public partial struct SkillCooldownSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkillCooldownData>();
            state.RequireForUpdate<SkillInputData>();
            state.RequireForUpdate<GameStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.Phase != GamePhase.Playing)
                return;

            float dt = SystemAPI.Time.DeltaTime;

            // Decrement all cooldowns
            var cooldown = SystemAPI.GetSingletonRW<SkillCooldownData>();
            cooldown.ValueRW.Skill1Remaining = math.max(0f, cooldown.ValueRO.Skill1Remaining - dt);
            cooldown.ValueRW.Skill2Remaining = math.max(0f, cooldown.ValueRO.Skill2Remaining - dt);
            cooldown.ValueRW.Skill3Remaining = math.max(0f, cooldown.ValueRO.Skill3Remaining - dt);
            cooldown.ValueRW.Skill4Remaining = math.max(0f, cooldown.ValueRO.Skill4Remaining - dt);

            // NOTE: SkillInputData flags are NOT reset here.
            // They are reset by SkillInputResetSystem which runs AFTER all skill systems.
            // This ensures skill systems can read the flags set by InputBridge/UI this frame.
        }
    }

    /// <summary>
    /// Resets SkillInputData flags to false after all skill systems have consumed them.
    /// Prevents a single press from firing on consecutive frames.
    /// Must run AFTER all skill systems.
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(LaserBurstSystem))]
    [UpdateAfter(typeof(ChainLightningSystem))]
    [UpdateAfter(typeof(EmpPulseSystem))]
    [UpdateAfter(typeof(OverchargeSystem))]
    public partial struct SkillInputResetSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkillInputData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var skillInput = SystemAPI.GetSingletonRW<SkillInputData>();
            skillInput.ValueRW.Skill1Pressed = false;
            skillInput.ValueRW.Skill2Pressed = false;
            skillInput.ValueRW.Skill3Pressed = false;
            skillInput.ValueRW.Skill4Pressed = false;
        }
    }
}
