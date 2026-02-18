using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Systems
{
    /// <summary>
    /// Handles Overcharge skill activation and buff duration management.
    /// When Skill4 pressed and off cooldown: activates buff and sets cooldown.
    /// Every frame: decrements active buff duration.
    /// Emits SkillEvent on activation for VFX bridge.
    /// </summary>
    [BurstCompile]
    public partial struct OverchargeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkillInputData>();
            state.RequireForUpdate<SkillCooldownData>();
            state.RequireForUpdate<OverchargeBuffData>();
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<InputData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.Phase != GamePhase.Playing)
                return;

            float dt = SystemAPI.Time.DeltaTime;
            var skillInput = SystemAPI.GetSingleton<SkillInputData>();
            var cooldown = SystemAPI.GetSingletonRW<SkillCooldownData>();
            var overcharge = SystemAPI.GetSingletonRW<OverchargeBuffData>();
            var input = SystemAPI.GetSingleton<InputData>();

            // Activation: Skill4 pressed and cooldown ready
            if (skillInput.Skill4Pressed && cooldown.ValueRO.Skill4Remaining <= 0f)
            {
                cooldown.ValueRW.Skill4Remaining = cooldown.ValueRO.Skill4MaxCooldown;
                overcharge.ValueRW.RemainingDuration = GameConstants.OverchargeDuration;

                // Emit SkillEvent for VFX bridge
                var skillEventBuffer = SystemAPI.GetSingletonBuffer<SkillEvent>();
                skillEventBuffer.Add(new SkillEvent
                {
                    SkillType = 3,
                    OriginPos = new float2(GameConstants.ShipPositionX, GameConstants.ShipPositionZ),
                    TargetPos = input.MouseWorldPos,
                    ChainCount = 0,
                    Radius = 0f
                });
            }

            // Decrement active buff duration every frame
            if (overcharge.ValueRO.RemainingDuration > 0f)
            {
                overcharge.ValueRW.RemainingDuration = math.max(0f, overcharge.ValueRO.RemainingDuration - dt);
            }
        }
    }
}
