using System.Collections.Generic;
using Fusion;

namespace FusionMultiplayer.Core
{
    /// <summary>Assignment 3 session metadata: game modes, maps, and Photon custom properties.</summary>
    public static class SessionCatalog
    {
        public const string PropGameMode = "gm";
        public const string PropMap = "map";
        public const string PropPhase = "phase";

        public const float MapFilterTimeoutSeconds = 8f;

        public enum GameModeKind
        {
            Build = 0,
            Combat = 1,
            Sandbox = 2
        }

        public enum MapKind
        {
            Any = -1,
            Arena = 0,
            Plaza = 1,
            Ruins = 2
        }

        public enum SessionPhase
        {
            Lobby = 0,
            Started = 1
        }

        public static readonly GameModeKind[] AllGameModes =
            { GameModeKind.Build, GameModeKind.Combat, GameModeKind.Sandbox };

        public static readonly MapKind[] SelectableMaps =
            { MapKind.Arena, MapKind.Plaza, MapKind.Ruins };

        public static string LobbyNameForMode(GameModeKind mode) => $"FM_{mode}";

        public static string GetModeLabel(GameModeKind mode) => mode switch
        {
            GameModeKind.Build => "Build",
            GameModeKind.Combat => "Combat",
            GameModeKind.Sandbox => "Sandbox",
            _ => mode.ToString()
        };

        public static string GetMapLabel(MapKind map) => map switch
        {
            MapKind.Any => "Any map",
            MapKind.Arena => "Arena",
            MapKind.Plaza => "Plaza",
            MapKind.Ruins => "Ruins",
            _ => map.ToString()
        };

        public static string GetModeDescription(GameModeKind mode) => mode switch
        {
            GameModeKind.Build => "Place and remove blocks (E / Q). No weapons.",
            GameModeKind.Combat => "Shoot projectiles (LMB). Score hidden until game ends.",
            GameModeKind.Sandbox => "Build, break other players' blocks (Q), and shoot (LMB).",
            _ => string.Empty
        };

        public static string GetMapDescription(MapKind map) => map switch
        {
            MapKind.Plaza => "Open plaza with columns and a central platform.",
            MapKind.Ruins => "Broken walls, rubble, and elevated platforms.",
            MapKind.Arena => "Walled combat arena with circular spawns.",
            _ => string.Empty
        };

        public static MapKind ResolveMapForHost(MapKind selected) =>
            selected == MapKind.Any ? MapKind.Arena : selected;

        public static int GetSceneBuildIndex(MapKind map) => SceneIndices.GetBuildIndex(map);

        public static Dictionary<string, SessionProperty> BuildProperties(GameModeKind mode, MapKind map,
            SessionPhase phase)
        {
            return new Dictionary<string, SessionProperty>
            {
                { PropGameMode, (int)mode },
                { PropMap, (int)map },
                { PropPhase, (int)phase }
            };
        }

        public static bool TryGetGameMode(SessionInfo info, out GameModeKind mode)
        {
            mode = GameModeKind.Build;
            if (!TryGetIntProperty(info, PropGameMode, out var raw))
                return false;
            if (raw < 0 || raw > (int)GameModeKind.Sandbox)
                return false;
            mode = (GameModeKind)raw;
            return true;
        }

        public static bool TryGetMap(SessionInfo info, out MapKind map)
        {
            map = MapKind.Arena;
            if (!TryGetIntProperty(info, PropMap, out var raw))
                return false;
            if (raw < 0 || raw > (int)MapKind.Ruins)
                return false;
            map = (MapKind)raw;
            return true;
        }

        public static bool TryGetPhase(SessionInfo info, out SessionPhase phase)
        {
            phase = SessionPhase.Lobby;
            if (!TryGetIntProperty(info, PropPhase, out var raw))
                return false;
            if (raw < 0 || raw > (int)SessionPhase.Started)
                return false;
            phase = (SessionPhase)raw;
            return true;
        }

        public static bool IsSessionStarted(SessionInfo info)
        {
            if (!info.IsValid)
                return false;
            if (!info.IsOpen)
                return true;
            return TryGetPhase(info, out var phase) && phase == SessionPhase.Started;
        }

        public static string FormatSessionRow(SessionInfo info)
        {
            if (!info.IsValid)
                return string.Empty;

            var mode = TryGetGameMode(info, out var gm) ? GetModeLabel(gm) : "?";
            var map = TryGetMap(info, out var mp) ? GetMapLabel(mp) : "?";
            var started = IsSessionStarted(info) ? " · STARTED" : string.Empty;
            var hidden = info.IsVisible ? string.Empty : " · HIDDEN";
            return $"{info.Name}  ({mode} / {map})  {info.PlayerCount}/{info.MaxPlayers}{started}{hidden}";
        }

        private static bool TryGetIntProperty(SessionInfo info, string key, out int value)
        {
            value = 0;
            if (!info.IsValid || info.Properties == null || !info.Properties.TryGetValue(key, out var prop))
                return false;
            value = prop;
            return true;
        }
    }
}
