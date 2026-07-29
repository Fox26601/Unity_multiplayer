namespace FusionMultiplayer.UI
{
    /// <summary>English UI copy shown to players (all in-game messages).</summary>
    public static class UiCopy
    {
        public const string MainMenuTitle = "FUSION MULTIPLAYER";
        public const string MainMenuSubtitle =
            "Host a room or browse sessions (Client-Server / Dedicated Server, up to 10 players).";
        public const string MainMenuLandingSubtitle =
            "Choose to host a new room or join an existing one.";
        public const string MainMenuGoCreate = "Create room";
        public const string MainMenuGoJoin = "Join room";
        public const string MainMenuBack = "Back";
        public const string MainMenuReconnect = "Reconnect to last room";
        public const string MainMenuQuickJoin = "Quick join";
        public const string MainMenuLeaderboard = "Leaderboard";
        public const string CreatePageTitle = "CREATE ROOM";
        public const string JoinPageTitle = "JOIN ROOM";
        public const string JoinRoomDropdownLabel = "Available rooms";
        public const string JoinRoomPlaceholder = "Select a room…";
        public const string JoinRoomButton = "Join room";
        public const string MaxPlayersLabel = "Max players";
        public const string DifficultyLabel = "Difficulty";
        public static string JoinRoomFailed(string detail) =>
            $"Could not join room: {detail}";
        public const string JoinRoomClosed =
            "That match already started. Use Reconnect to last room if you were in it, or wait for a new lobby.";
        public const string ConnectionManagerMissing =
            "ConnectionManager is missing — reopen 00_MainMenu (or press Play from 00_Boot).";
        public const string ReconnectFailed =
            "Reconnect failed. The host may have left, or your session token is no longer valid.";
        public const string QuickJoinNoMatch =
            "No open rooms match the selected mode / map / difficulty. Use Create room, Join by name, or Reconnect if you left a started match.";
        public static string QuickJoinNoMatchDetail(string filters) =>
            $"No open rooms match {filters}. Started matches are not quick-joinable — use Reconnect if you were in one, or Create / Join a lobby.";
        public const string DisconnectNoticePrefix = "Disconnected: ";
        public const string DisconnectedByServer =
            "Disconnected by server (nickname taken or join refused). Change your nickname and try again.";
        public const string ReconnectPrompt =
            "Previous session found. Press Reconnect to restore control.";
        public const string CareerStatsTitle = "CAREER STATS";
        public const string CareerStatsEmpty = "No saved matches yet.";
        public const string CareerStatsClose = "Close";
        public const string NicknameTaken =
            "Nickname already in use. Choose another name and try again.";

        public static string NicknameTakenDetail(string nickname)
        {
            var nick = string.IsNullOrWhiteSpace(nickname) ? "that name" : $"\"{nickname.Trim()}\"";
            return $"Nickname {nick} is already taken in this room. Change your nickname and try again.";
        }

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
        public const string SessionBrowserNoConnection =
            "ConnectionManager missing — Play from 00_Boot (or open 00_MainMenu).";
        public const string SessionBrowserLobbyFailed =
            "Could not join session lobby. Check Photon AppId / network, then press Refresh.";

        public const string LobbyTitle = "LOBBY";
        public const string LobbyLeave = "Leave lobby";
        public const string LobbyStartButton = "START GAME";
        public const string LobbySessionInfoFormat = "Mode: {0}  ·  Map: {1}";
        public const string LobbySessionStarted = "This session is IN PROGRESS — new players cannot join.";
        public const string LobbyPlayersWaiting = "ROOM PLAYERS\n\nConnecting to session...";
        public const string LobbyNotConnected =
            "Not connected. Open scene 00_MainMenu, press Play, then Create or Join.";
        public const string LobbyMasterStatus =
            "You are the session host/server authority. Press START GAME when ready.";
        public const string LobbyClientStatus =
            "Waiting for the host/server. You can press START to request a start.";
        public const string LobbyStartRequested = "Start requested — waiting for server…";

        public const string CharacterSelectPrompt =
            "Pick a character slot (0–9). Each slot has its own spawn point.";
        public const string CharacterSlotFree = "FREE";
        public const string ChatPanelTitle = "CHAT";
        public const string ChatSendButton = "Send";
        public const string CharacterSelectNotConnected =
            "Not connected — return to the main menu and join a session.";
        public const string CharacterSelectWaitingForGame =
            "Connecting game systems… pick a slot again in a moment.";
        public const string CharacterSelectRestoring =
            "Restoring your character…";
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
        public const string GameOverVoteTitle = "VOTE NEXT MAP";
        public const string GameOverVoteRestart = "Restart map";
        public const string GameOverVoteCountsFormat = "Votes — Restart:{0}  Arena:{1}  Plaza:{2}  Ruins:{3}";
        public const string GameOverVoteTimerFormat = "Voting ends in {0}s";
        public const string GameOverVotedFormat = "You voted: {0}";
        public const string GameOverLeave = "Leave to main menu";
        public const string LeaderboardTitle = "SCOREBOARD";
        public const string CombatHealthFormat = "HP {0}/{1}";
        public const string CombatHealthDead = "DEAD — respawning…";
        public const string GameHudModeFormat = "Mode: {0}";
        public const string MatchInfoMetaFormat = "{0}  ·  {1}  ·  {2}";
        public const string MatchInfoKdFormat = "K {0}  D {1}";
        public const string MatchInfoBotBadge = "BOT";
        public const string MatchEventCalm = "Event: Calm";
        public const string MatchEventStorm = "Event: Storm";
        public const string MatchEventLowGravity = "Event: Low Gravity";
        public const string MatchEventUnknown = "Event: —";

        public const string PauseTitle = "PAUSED";
        public const string PauseResume = "Resume";
        public const string PauseLeave = "Leave to main menu";
        public const string PauseControls =
            "WASD move · Mouse look · LMB shoot · E/Q build · Tab scores · Enter chat · Esc pause";
    }
}
