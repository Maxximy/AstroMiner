using Unity.Entities;

/// <summary>
/// Downward drift speed for placeholder entities.
/// </summary>
public struct DriftData : IComponentData
{
    /// <summary>
    /// Units per second, drifting downward (positive = down).
    /// </summary>
    public float Speed;
}

/// <summary>
/// Spin rotation speed for placeholder asteroid entities.
/// </summary>
public struct SpinData : IComponentData
{
    /// <summary>
    /// Rotation speed in radians per second.
    /// </summary>
    public float RadiansPerSecond;
}

/// <summary>
/// Tag component to identify placeholder entities for cleanup and rendering sync.
/// </summary>
public struct PlaceholderTag : IComponentData
{
}
