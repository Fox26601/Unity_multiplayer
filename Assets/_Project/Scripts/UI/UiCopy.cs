namespace FusionMultiplayer.UI
{
    /// <summary>English UI copy shown to players (all in-game messages).</summary>
    public static class UiCopy
    {
        public const string MainMenuTitle = "FUSION MULTIPLAYER";
        public const string MainMenuSubtitle =
            "Host a room or browse sessions by game mode (up to 10 players, Shared Mode).";
        public const string MainMenuLandingSubtitle =
            "Choose to host a new room or join an existing one.";
        public const string MainMenuGoCreate = "Create room";
        public const string MainMenuGoJoin = "Join room";
        public const string MainMenuBack = "Back";
        public const string CreatePageTitle = "CREATE ROOM";
        public const string JoinPageTitle = "JOIN ROOM";
        public const string JoinRoomDropdownLabel = "Available rooms";
        public const string JoinRoomPlaceholder = "Select a room…";
        public const string JoinRoomButton = "Join room";

        public const string NicknameLabel = "Nickname";
        public const string RoomLabel = "Room name";
        public const string NicknamePlaceholder = "Enter your nickname";
        public const string RoomPlaceholder = "Enter a name for your room";
        public const string PlayerColorLabel = "Player color";
        public const string RandomColorButton = "Random color";
        public const string CreateHostButton = "Create / Host room";

        public const string SessionBrowserTitle = "SESSION BROWSER";
        public const string GameModeLabel = "Game mode";
        public const string MapFilterLabel = "Preferred map";
        public const string HiddenSessionLabel = "Hide from browser list";
        public const string SessionBrowserListTitle = "Available sessions";
        public const string SessionBrowserRefresh = "Refresh";
        public const string SessionBrowserJoinSelected = "Join selected";
        public const string SessionBrowserLoading = "Loading sessions…";
        public const string SessionBrowserEmpty = "No sessions found for this mode / map filter.";
        public const string SessionBrowserStartedOnly =
            "Sessions are listed below but already started — select an open room or create a new one.";
        public const string SessionBrowserOffline =
            "Session browser needs Photon Cloud. Disable Offline Play for multiplayer.";
        public const string SessionBrowserNoConnection = "ConnectionManager is missing in this scene.";

        public const string LobbyTitle = "LOBBY";
        public const string LobbySessionInfoFormat = "Mode: {0}  ·  Map: {1}";
        public const string LobbySessionStarted = "This session is IN PROGRESS — new players cannot join.";
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
            "Character ready. WASD move · Mouse look · Enter chat · E place · Q remove · LMB shoot (Combat/Sandbox).";
        public static string CharacterSpawnedBanner(int slot) =>
            $"Slot {slot} · WASD · Mouse · Enter chat · E/Q build · LMB shoot";
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
        public const string PhotonAuthFailed =
            "Photon authentication failed (OpAuthenticate). Replace the repo template App Id with YOUR Fusion App Id: " +
            "Tools → Fusion Multiplayer → Configure Fusion App Id. " +
            "Or enable Offline Play for local tests without cloud.";

        public const string SessionGuardTitle = "GAME SCENE — NO ACTIVE SESSION";
        public const string SessionGuardBody =
            "You opened a game map scene without joining through the main menu.\n\n" +
            "This scene expects an active Fusion session (ConnectionManager + NetworkRunner).\n" +
            "Without that, there is no main menu, no player camera, and no match flow.\n\n" +
            "HOW TO PLAY:\n" +
            "1. Stop Play.\n" +
            "2. Tools → Fusion Multiplayer → Use Main Menu As Play Mode Start Scene.\n" +
            "3. Press Play on Player 1, pick game mode, create or join a session.\n" +
            "4. Player 2 (MPPM clone) auto-joins the same room — wait for green status on both.\n" +
            "5. In the lobby, the master clicks Start Game, then pick character slots.\n\n" +
            "MPPM: turn OFF Offline Play for multi-client tests. Player clones must not start on a map scene alone.";

        public const string GameOverResultsTitle = "RESULTS";
        public const string GameOverResultsHeader = "PLAYER\tKILLS\tDEATHS";
        public const string GameOverNoScores = "No score data yet.";
        public const string LeaderboardTitle = "SCOREBOARD";
        public const string CombatHealthFormat = "HP {0}/{1}";
        public const string CombatHealthDead = "DEAD — respawning…";
        public const string GameHudModeFormat = "Mode: {0}";
    }
}
