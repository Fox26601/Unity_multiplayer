using System;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>Minecraft-style modes: gameplay locks the cursor; menu/chat unlocks it.</summary>
    public enum GameplayInputModeKind
    {
        Menu,
        Gameplay
    }

    /// <summary>Central cursor and input gate for gameplay vs UI (chat, menus).</summary>
    public static class GameplayInputMode
    {
        public static event Action<GameplayInputModeKind> Changed;

        public static GameplayInputModeKind Current { get; private set; } = GameplayInputModeKind.Menu;

        public static bool IsGameplay => Current == GameplayInputModeKind.Gameplay;

        /// <summary>When true, gameplay cursor lock must not be restored (chat compose open).</summary>
        public static bool ChatBlockingGameplay { get; set; }

        public static void SetMenu()
        {
            var changed = Current != GameplayInputModeKind.Menu;
            Current = GameplayInputModeKind.Menu;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (changed)
                Changed?.Invoke(Current);
        }

        public static void SetGameplay()
        {
            var changed = Current != GameplayInputModeKind.Gameplay;
            Current = GameplayInputModeKind.Gameplay;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (changed)
                Changed?.Invoke(Current);
        }

        /// <summary>Re-apply cursor state after focus loss (e.g. alt-tab).</summary>
        public static void RefreshCursor()
        {
            if (Current == GameplayInputModeKind.Gameplay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
