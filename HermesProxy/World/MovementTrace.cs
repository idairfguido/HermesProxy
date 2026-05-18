using System;

namespace HermesProxy.World;

/// <summary>
/// Opt-in diagnostic gate for SMSG_ON_MONSTER_MOVE / CreateObject MovementSpline wire emission.
/// Set the <c>HERMES_TRACE_MOVEMENT</c> environment variable to any non-empty value before
/// launching HermesProxy to capture per-packet traces in the proxy log. Default off; zero
/// overhead on the hot path when disabled (single static-readonly bool check).
/// </summary>
public static class MovementTrace
{
    public static readonly bool Enabled =
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HERMES_TRACE_MOVEMENT"));
}
