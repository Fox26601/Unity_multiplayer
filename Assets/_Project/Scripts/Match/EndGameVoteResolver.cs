using System.Collections.Generic;
using Fusion;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>End-game map vote tally and winner selection (server-side).</summary>
    public static class EndGameVoteResolver
    {
        public static void CountPlayersAndVotes(out int connected, out int voted, out int[] counts,
            out int totalVotes)
        {
            connected = 0;
            voted = 0;
            totalVotes = 0;
            counts = new int[GameManager.VoteOptionCount];

            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd.Object == null || !pd.Object.IsValid)
                    continue;

                connected++;
                var vote = pd.EndGameVote;
                if (!GameManager.IsValidVoteOption(vote))
                    continue;

                voted++;
                totalVotes++;
                counts[vote]++;
            }
        }

        public static int PickWinningOption(int[] counts, int totalVotes)
        {
            var max = -1;
            for (var i = 0; i < counts.Length; i++)
            {
                if (counts[i] > max)
                    max = counts[i];
            }

            var top = new List<int>(GameManager.VoteOptionCount);
            for (var i = 0; i < counts.Length; i++)
            {
                if (counts[i] == max)
                    top.Add(i);
            }

            if (top.Count == 1 && counts[top[0]] * 2 > totalVotes)
                return top[0];

            // Server random tie-break.
            return top[Random.Range(0, top.Count)];
        }
    }
}
