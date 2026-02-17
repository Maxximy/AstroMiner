using Unity.Entities;
using Unity.Mathematics;

public struct GameStateData : IComponentData
{
    public GamePhase Phase;
    public float Timer;
    public long Credits;
}

public struct InputData : IComponentData
{
    public float2 MouseWorldPos;
    public bool MouseValid;
}
