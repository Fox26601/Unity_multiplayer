#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// MPPM virtual players use a read-only Asset Database. Unity logs a known harmless error
/// when scripted importers (Fusion NetworkProjectConfig) sync custom dependencies (UUM-96906).
/// Suppresses managed logs and prunes native Asset Database asserts from clone/main consoles.
/// </summary>
[InitializeOnLoad]
internal static class MppmReadOnlyLogFilter
{
    private static readonly string[] BenignFragments =
    {
        "Asset Database is set to Read Only, but it has found out-of-date assets",
        "CloneInternalRuntime",
        "TriggerCloneRefreshMessage",
        "Refresh completed but there are assets queued up for importing"
    };

    private static bool _installed;
    private static int _pruneFramesRemaining;
    private static Type _logEntriesType;
    private static MethodInfo _getCount;
    private static MethodInfo _getEntryInternal;
    private static MethodInfo _deleteEntry;
    private static Type _logEntryType;
    private static bool _logApiResolved;
    private static bool _logApiAvailable;

    static MppmReadOnlyLogFilter()
    {
        InstallLogHandler();
        ResolveLogEntriesApi();
        Application.logMessageReceived += OnLogMessageReceived;
        EditorApplication.update += OnEditorUpdate;

        if (MppmEditorApi.IsCloneEditor)
            _pruneFramesRemaining = 600;
    }

    private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (!IsBenignMppmReadOnlyWarning(condition) && !IsBenignMppmReadOnlyWarning(stackTrace))
            return;

        _pruneFramesRemaining = Math.Max(_pruneFramesRemaining, 60);
        PruneBenignConsoleEntries();
    }

    private static void OnEditorUpdate()
    {
        if (_pruneFramesRemaining <= 0)
            return;

        _pruneFramesRemaining--;
        PruneBenignConsoleEntries();
    }

    private static void InstallLogHandler()
    {
        if (_installed)
            return;

        _installed = true;
        var inner = Debug.unityLogger.logHandler;
        Debug.unityLogger.logHandler = new FilterHandler(inner);
    }

    private static void ResolveLogEntriesApi()
    {
        if (_logApiResolved)
            return;

        _logApiResolved = true;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (name == null || !name.StartsWith("UnityEditor", StringComparison.Ordinal))
                continue;

            var type = assembly.GetType("UnityEditor.LogEntries");
            if (type == null)
                continue;

            BindLogEntriesType(type);
            if (_logApiAvailable)
                return;
        }
    }

    private static void BindLogEntriesType(Type logEntriesType)
    {
        _logEntriesType = logEntriesType;
        _getCount = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        _logEntryType = logEntriesType.GetNestedType("LogEntry", BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var method in logEntriesType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.Name == "DeleteEntry" && method.GetParameters().Length == 1)
            {
                _deleteEntry = method;
                break;
            }
        }

        foreach (var method in logEntriesType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.Name != "GetEntryInternal")
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length == 2)
            {
                _getEntryInternal = method;
                _logEntryType = parameters[1].ParameterType;
                break;
            }

            if (parameters.Length == 5)
            {
                _getEntryInternal = method;
                break;
            }
        }

        _logApiAvailable = _getCount != null && _getEntryInternal != null && _deleteEntry != null;
    }

    private static void PruneBenignConsoleEntries()
    {
        if (!_logApiAvailable)
        {
            ResolveLogEntriesApi();
            if (!_logApiAvailable)
                return;
        }

        var count = (int)_getCount.Invoke(null, null);
        for (var row = count - 1; row >= 0; row--)
        {
            if (!TryGetLogText(row, out var text))
                continue;

            if (!IsBenignMppmReadOnlyWarning(text))
                continue;

            _deleteEntry.Invoke(null, new object[] { row });
        }
    }

    private static bool TryGetLogText(int row, out string text)
    {
        text = null;
        var parameters = _getEntryInternal.GetParameters();

        if (parameters.Length == 2)
        {
            var entryType = parameters[1].ParameterType;
            var args = new object[] { row, entryType.IsValueType ? Activator.CreateInstance(entryType) : null };
            if (_getEntryInternal.Invoke(null, args) is not bool ok || !ok)
                return false;

            text = ReadLogEntryText(args[1], entryType);
            return !string.IsNullOrEmpty(text);
        }

        if (parameters.Length == 5)
        {
            var args = new object[] { row, null, null, 0, 0 };
            if (_getEntryInternal.Invoke(null, args) is not bool ok || !ok)
                return false;

            text = args[1] as string;
            return !string.IsNullOrEmpty(text);
        }

        return false;
    }

    private static string ReadLogEntryText(object entry, Type entryType)
    {
        if (entry == null || entryType == null)
            return null;

        if (entry is string s)
            return s;

        var messageField = entryType.GetField("message", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                           ?? entryType.GetField("Message", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return messageField?.GetValue(entry) as string;
    }

    private sealed class FilterHandler : ILogHandler
    {
        private readonly ILogHandler _inner;

        public FilterHandler(ILogHandler inner) => _inner = inner;

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            var message = args == null || args.Length == 0 ? format : string.Format(format, args);
            if (IsBenignMppmReadOnlyWarning(message))
            {
                _pruneFramesRemaining = Math.Max(_pruneFramesRemaining, 60);
                return;
            }

            _inner.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, Object context)
        {
            if (exception != null && IsBenignMppmReadOnlyWarning(exception.Message))
            {
                _pruneFramesRemaining = Math.Max(_pruneFramesRemaining, 60);
                return;
            }

            _inner.LogException(exception, context);
        }
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
#endif
