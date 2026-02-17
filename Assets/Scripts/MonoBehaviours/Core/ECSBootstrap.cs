using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class ECSBootstrap : MonoBehaviour
{
    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var em = world.EntityManager;

        // Create GameState singleton
        var gameStateEntity = em.CreateEntity(typeof(GameStateData));
        em.SetComponentData(gameStateEntity, new GameStateData
        {
            Phase = GamePhase.Playing,
            Timer = 0f,
            Credits = 0
        });

        // Create Input singleton
        var inputEntity = em.CreateEntity(typeof(InputData));
        em.SetComponentData(inputEntity, new InputData
        {
            MouseWorldPos = float2.zero,
            MouseValid = false
        });

        Debug.Log("ECS Bootstrap complete: singletons created");
    }
}
