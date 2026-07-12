using System;
using System.Collections.Generic;
using System.IO;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Lightweight JSON file database for match stats (read + write on the server).
    /// </summary>
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
                foreach (var pd in UnityEngine.Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
                {
                    if (pd.Object == null || !pd.Object.IsValid)
                        continue;

                    var nick = pd.Nick.ToString();
                    if (string.IsNullOrWhiteSpace(nick))
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
