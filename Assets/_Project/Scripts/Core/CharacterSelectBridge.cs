using System;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>UI bridge for character-select approve / reject / slot occupancy changes.</summary>
    public static class CharacterSelectBridge
    {
        public static event Action<int, Vector3, Quaternion> Approved;
        public static event Action<int> Rejected;
        public static event Action SlotsChanged;

        public static void NotifyApproved(int index, Vector3 position, Quaternion rotation) =>
            Approved?.Invoke(index, position, rotation);

        public static void NotifyRejected(int index) =>
            Rejected?.Invoke(index);

        public static void NotifySlotsChanged() =>
            SlotsChanged?.Invoke();
    }
}
