using System;
using System.Collections.Generic;
using System.IO;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>Local JSON match stats (written on each peer at game-over).</summary>
    public static class MatchStatsDatabase
    {
        [Serializable]
        private class StatsFile
        {
            public List<PlayerStatsRow> Players = new();
        }

        [Serializable]
        public class PlayerStatsRow
        {
            public string Nickname;
            public int Kills;
            public int Deaths;
            public int Matches;
        }

        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, "fusion_match_stats.json");

        public static void WriteMatchResults()
        {
            try
            {
                var file = Load();
                foreach (var pd in PlayerRegistry.AllPlayerData)
                {
                    if (pd == null || pd.Object == null || !pd.Object.IsValid)
                        continue;

                    // Career DB is for human players only — bot takeover seats are mid-match placeholders.
                    if (pd.IsBotControlled)
                        continue;

                    var nick = pd.Nick.ToString().Trim();
                    if (string.IsNullOrWhiteSpace(nick) || nick.StartsWith("BOT ", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var row = file.Players.Find(p =>
                        string.Equals(p.Nickname, nick, StringComparison.OrdinalIgnoreCase));
                    if (row == null)
                    {
                        row = new PlayerStatsRow { Nickname = nick };
                        file.Players.Add(row);
                    }

                    row.Kills += pd.Score;
                    row.Deaths += pd.Deaths;
                    row.Matches += 1;
                }

                var json = JsonUtility.ToJson(file, true);
                File.WriteAllText(FilePath, json);
                Debug.Log($"[FusionMultiplayer] Match stats written to {FilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FusionMultiplayer] Failed to write match stats: {ex.Message}");
            }
        }

        public static IReadOnlyList<PlayerStatsRow> ReadLeaderboard()
        {
            var file = Load();
            file.Players.RemoveAll(p =>
                p == null ||
                string.IsNullOrWhiteSpace(p.Nickname) ||
                p.Nickname.TrimStart().StartsWith("BOT ", StringComparison.OrdinalIgnoreCase));
            file.Players.Sort((a, b) => b.Kills.CompareTo(a.Kills));
            return file.Players;
        }

        private static StatsFile Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new StatsFile();

                var json = File.ReadAllText(FilePath);
                var file = JsonUtility.FromJson<StatsFile>(json);
                return file ?? new StatsFile();
            }
            catch
            {
                return new StatsFile();
            }
        }
    }
}
