using System.Collections.Generic;
using System.Text;
using Fusion;
using UnityEngine;

namespace FusionMultiplayer.Chat
{
    /// <summary>Structured console logging for chat bind, send, receive, and PM roster.</summary>
    public static class ChatDiagnostics
    {
        private const string Prefix = "[FusionMultiplayer][Chat]";

        public static void Log(string message)
        {
            Debug.Log($"{Prefix} {message}");
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }

        public static void LogBind(int buildVersion, bool logOk, bool inputOk, bool sendOk, bool dropdownOk,
            string logPath)
        {
            Log(
                $"bind v{buildVersion}: log={(logOk ? "ok" : "MISSING")} input={(inputOk ? "ok" : "MISSING")} " +
                $"send={(sendOk ? "ok" : "MISSING")} dropdown={(dropdownOk ? "ok" : "MISSING")} path={logPath}");
        }

        public static void LogWelcome(int textLength)
        {
            Log($"welcome len={textLength}");
        }

        public static void LogSend(string preview, bool whisper, PlayerRef target, bool managerOk, bool objectValid,
            bool sent)
        {
            var targetLabel = whisper ? target.ToString() : "global";
            Log(
                $"send whisper={whisper} target={targetLabel} manager={(managerOk ? "ok" : "null")} " +
                $"objectValid={objectValid} sent={sent} text=\"{Truncate(preview, 40)}\"");
        }

        public static void LogReceive(PlayerRef sender, string text, bool isWhisper, PlayerRef recipient)
        {
            var route = isWhisper ? $"PM→{recipient}" : "global";
            Log($"receive {route} from={sender} len={text?.Length ?? 0} \"{Truncate(text, 40)}\"");
        }

        public static void LogPmRoster(int activePlayerCount, int localPlayerId,
            IReadOnlyList<string> remoteNicks, int dropdownOptionCount)
        {
            var sb = new StringBuilder();
            sb.Append($"PM roster active={activePlayerCount} local={localPlayerId} options={dropdownOptionCount}");
            if (remoteNicks.Count == 0)
            {
                sb.Append(" remotes=(none)");
            }
            else
            {
                sb.Append(" remotes=[");
                for (var i = 0; i < remoteNicks.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(remoteNicks[i]);
                }

                sb.Append(']');
            }

            Log(sb.ToString());

            if (activePlayerCount > 1 && remoteNicks.Count == 0)
                LogWarning("players connected but no remote entries resolved for PM dropdown");
        }

        public static void LogDropdownCaption(string caption, float captionWidth, float dropdownWidth, int optionCount)
        {
            Log(
                $"dropdown caption=\"{Truncate(caption, 32)}\" captionW={captionWidth:F0} " +
                $"dropdownW={dropdownWidth:F0} options={optionCount}");
            if (optionCount > 0 && captionWidth < 8f)
                LogWarning("dropdown caption rect width near zero — text will not be visible");
        }

        public static void LogPmSelection(int index, string nick, PlayerRef target)
        {
            Log($"PM selected index={index} nick=\"{Truncate(nick, 24)}\" target={target}");
        }

        public static void LogPmChannelList(bool open, int optionCount)
        {
            Log($"PM channel list {(open ? "open" : "closed")} options={optionCount}");
        }

        public static void LogPmChannelOptions(int optionCount, IReadOnlyList<string> remoteNicks)
        {
            var sb = new StringBuilder();
            sb.Append($"PM channel options={optionCount} remotes=[");
            if (remoteNicks != null && remoteNicks.Count > 0)
            {
                for (var i = 0; i < remoteNicks.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(remoteNicks[i]);
                }
            }
            else
            {
                sb.Append("none");
            }

            sb.Append(']');
            Log(sb.ToString());
        }

        public static void LogDropdownExpanded(bool expanded, int itemCount)
        {
            Log($"dropdown {(expanded ? "expanded" : "collapsed")} items={itemCount}");
        }

        public static void LogDropdownOptions(IReadOnlyList<string> optionTexts)
        {
            if (optionTexts == null || optionTexts.Count == 0)
            {
                Log("dropdown options=(empty)");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("dropdown options=[");
            for (var i = 0; i < optionTexts.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('"').Append(Truncate(optionTexts[i], 24)).Append('"');
            }

            sb.Append(']');
            Log(sb.ToString());
        }

        public static void LogAppend(int totalLength, bool logBound, float rectHeight = 0f, float preferredHeight = 0f)
        {
            Log(
                $"append logBound={logBound} totalLen={totalLength} rectH={rectHeight:F0} preferredH={preferredHeight:F0}");
        }

        public static void LogChatManagerState(string phase, bool instanceOk, bool objectValid)
        {
            Log($"manager {phase}: instance={(instanceOk ? "ok" : "null")} objectValid={objectValid}");
        }

        public static void LogPresenterMissing(string context)
        {
            LogWarning($"no chat presenter attached — {context}");
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value.Substring(0, max) + "…";
        }
    }
}
