#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// MPPM virtual players use a read-only Asset Database. Unity logs a known harmless warning
/// when scripted importers (Fusion NetworkProjectConfig) sync custom dependencies (UUM-96906).
/// Suppress it in all Editor instances so the main console stays clean during MPPM Play.
/// </summary>
[InitializeOnLoad]
internal static class MppmReadOnlyLogFilter
{
    private static readonly string[] BenignFragments =
    {
        "Asset Database is set to Read Only, but it has found out-of-date assets",
        "CloneInternalRuntime",
        "TriggerCloneRefreshMessage"
    };

    private static bool _installed;

    static MppmReadOnlyLogFilter()
    {
        EditorApplication.delayCall += EnsureInstalled;
    }

    private static void EnsureInstalled()
    {
        if (_installed)
            return;

        _installed = true;
        var inner = Debug.unityLogger.logHandler;
        Debug.unityLogger.logHandler = new FilterHandler(inner);
    }

    private sealed class FilterHandler : ILogHandler
    {
        private readonly ILogHandler _inner;

        public FilterHandler(ILogHandler inner) => _inner = inner;

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            var message = args == null || args.Length == 0 ? format : string.Format(format, args);
            if (IsBenignMppmReadOnlyWarning(message))
                return;

            _inner.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, Object context)
        {
            if (exception != null && IsBenignMppmReadOnlyWarning(exception.Message))
                return;

            _inner.LogException(exception, context);
        }

        private static bool IsBenignMppmReadOnlyWarning(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            foreach (var fragment in BenignFragments)
            {
                if (message.IndexOf(fragment, StringComparison.Ordinal) >= 0)
                    return true;
            }

            return false;
        }
    }
}
#endif
