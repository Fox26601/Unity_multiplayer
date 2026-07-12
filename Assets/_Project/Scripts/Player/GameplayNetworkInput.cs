using Fusion;

namespace FusionMultiplayer.Player
{
    /// <summary>Per-tick gameplay input collected in ConnectionManager.OnInput.</summary>
    public struct GameplayNetworkInput : INetworkInput
    {
        public float MoveForward;
        public float Strafe;
        public float LookDeltaX;
        public float LookDeltaY;
        public NetworkButtons Buttons;
    }

    public static class GameplayButton
    {
        public const int Place = 1;
        public const int Remove = 2;
        public const int Fire = 4;
        public const int Jump = 8;
    }
}
