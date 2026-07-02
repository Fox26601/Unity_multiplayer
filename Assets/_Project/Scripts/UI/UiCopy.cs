namespace FusionMultiplayer.UI
{
    /// <summary>English UI copy shown to players (all in-game messages).</summary>
    public static class UiCopy
    {
        public const string MainMenuTitle = "FUSION MULTIPLAYER";
        public const string MainMenuSubtitle =
            "Create or join a room (up to 10 players). Shared Mode session.";

        public const string NicknameLabel = "Nickname";
        public const string RoomLabel = "Room name";
        public const string NicknamePlaceholder = "Enter your nickname";
        public const string RoomPlaceholder = "Same room name for all players";

        public const string LobbyTitle = "LOBBY";
        public const string LobbyPlayersWaiting = "ROOM PLAYERS\n\nConnecting to session...";
        public const string LobbyNotConnected =
            "Not connected. Open scene 00_MainMenu, press Play, then Create or Join.";
        public const string LobbyMasterStatus =
            "You are the session master. Press START GAME when all players are in the lobby.";
        public const string LobbyClientStatus =
            "Waiting for the session master to start the game...";

        public const string CharacterSelectPrompt =
            "Pick a character slot (0–9). Each slot has its own spawn point.";
        public const string CharacterSelectNotConnected =
            "Not connected — return to the main menu and join a session.";
        public const string CharacterSelectWaitingForGame =
            "Connecting game systems… pick a slot again in a moment.";
        public const string CharacterSelectGameManagerTimeout =
            "Game systems did not connect in time. Leave to main menu and rejoin the room.";
        public static string CharacterSelectRequesting(int index) =>
            $"Requesting slot {index}… waiting for session master.";
        public const string CharacterSpawned =
            "Character ready. WASD move · Mouse look · Enter chat · E place · Q remove.";
        public static string CharacterSpawnedBanner(int slot) =>
            $"Slot {slot} · WASD · Mouse · Enter chat · E place · Q remove";
        public static string CharacterSlotTaken(int index) =>
            $"Slot {index} is already taken — choose another.";

        public const string ChatWhisperUnknown = "[System] Whisper failed: nickname not found in this room.";
        public const string ChatWhisperPrefix = "[PM] ";
        public const string ChatSelfNickFormat = "You: {0}";
        public const string ChatWhisperToLabel = "PM:";
        public const string ChatPmGlobalOption = "Everyone (global)";
        public const string ChatPmInputPlaceholderGlobal = "Type a message…";
        public const string ChatPmInputPlaceholderFormat = "Private message to {0}…";
        public const string ChatPmTargetLeft = "[System] PM target left the session.\n";
        public const string ChatWelcome =
            "[System] Global chat. Press Enter to type. PM: pick a player in the dropdown.\n";
        public const string ChatSoloHint =
            "[System] Press Enter to chat. PM unlocks when others join.\n";

        public const string StatusConnecting = "Connecting to Photon room...";
        public const string StatusConnectingOffline = "Starting offline session (no Photon Cloud)...";
        public const string StatusEnteringLobby = "Connected. Entering lobby...";
        public const string StatusInLobby = "You are in the lobby.";
        public const string StatusInGame = "Game scene loaded.";
        public const string StatusFailedPrefix = "Connection failed: ";

        public const string SessionGuardTitle = "GAME SCENE — NO ACTIVE SESSION";
        public const string SessionGuardBody =
            "You opened scene 02_Game without joining through the main menu.\n\n" +
            "This scene expects an active Fusion session (ConnectionManager + NetworkRunner).\n" +
            "Without that, there is no main menu, no player camera, and no match flow.\n\n" +
            "HOW TO PLAY:\n" +
            "1. Stop Play.\n" +
            "2. Tools → Fusion Multiplayer → Use Main Menu As Play Mode Start Scene.\n" +
            "3. Press Play on Player 1, enter nickname and room, click Create / Host.\n" +
            "4. Player 2 (MPPM clone) auto-joins the same room — wait for green status on both.\n" +
            "5. In the lobby, the master clicks Start Game, then pick character slots.\n\n" +
            "MPPM: turn OFF Offline Play for multi-client tests. Player 2 must not start on 02_Game alone.";
    }
}
