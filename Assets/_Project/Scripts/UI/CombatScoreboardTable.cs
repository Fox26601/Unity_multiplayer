using System.Collections.Generic;
using System.Text;
using FusionMultiplayer.Player;

namespace FusionMultiplayer.UI
{
    /// <summary>Shared kills/deaths table for Tab leaderboard and game-over results.</summary>
    public static class CombatScoreboardTable
    {
        private struct Row
        {
            public string Nick;
            public int Kills;
            public int Deaths;
            public int PlayerId;
        }

        public static bool AppendRows(StringBuilder sb, string headerLine, string separatorLine = null)
        {
            sb.AppendLine(headerLine);
            sb.AppendLine(separatorLine ?? "----------------------------------------");

            var rows = CollectRows();
            if (rows.Count == 0)
                return false;

            rows.Sort((a, b) =>
            {
                var byKills = b.Kills.CompareTo(a.Kills);
                if (byKills != 0)
                    return byKills;
                return a.Deaths.CompareTo(b.Deaths);
            });

            foreach (var row in rows)
                sb.AppendLine($"{row.Nick}\t{row.Kills}\t{row.Deaths}");

            return true;
        }

        private static List<Row> CollectRows()
        {
            var rows = new List<Row>();
            foreach (var pd in UnityEngine.Object.FindObjectsByType<PlayerData>(UnityEngine.FindObjectsSortMode.None))
            {
                if (pd.Object == null || !pd.Object.IsValid)
                    continue;

                var nick = pd.Nick.ToString();
                if (string.IsNullOrWhiteSpace(nick))
                    nick = $"Player {pd.Object.InputAuthority.PlayerId}";

                rows.Add(new Row
                {
                    Nick = nick,
                    Kills = pd.Score,
                    Deaths = pd.Deaths,
                    PlayerId = pd.Object.InputAuthority.PlayerId
                });
            }

            return rows;
        }
    }
}
