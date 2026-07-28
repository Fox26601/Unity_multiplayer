using Fusion;

namespace FusionMultiplayer.Player
{
    /// <summary>Per-tick gameplay input collected in ConnectionManager.OnInput.</summary>
    public struct GameplayNetworkInput : INetworkInput
    {
        public float MoveForward;
        public float Strafe;
        /// <summary>Absolute body yaw (degrees) at sample time — avoids delta drift.</summary>
        public float LookYaw;
        /// <summary>Absolute camera pitch (degrees) at sample time.</summary>
        public float LookPitch;
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
