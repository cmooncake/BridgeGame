using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ThirdPartyDefineSymbols
{
    private static readonly string[] RequiredDefines =
    {
        "DOTWEEN",
        "UNITASK_DOTWEEN_SUPPORT"
    };

    private static readonly string[] ForbiddenDefines =
    {
        "DOTWEEN_EPO",
        "EPO_DOTWEEN"
    };

    static ThirdPartyDefineSymbols()
    {
        SyncDefines();
    }

    [MenuItem("Tools/MoonBridge/Fix DOTween Defines")]
    public static void SyncDefines()
    {
        foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
        {
            if (group == BuildTargetGroup.Unknown)
            {
                continue;
            }

            try
            {
                SyncGroup(group);
            }
            catch (ArgumentException)
            {
            }
        }
    }

    private static void SyncGroup(BuildTargetGroup group)
    {
        var current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        if (current == null)
        {
            current = string.Empty;
        }

        var symbols = new System.Collections.Generic.List<string>();
        var parts = current.Split(';');
        for (var i = 0; i < parts.Length; i++)
        {
            var symbol = parts[i].Trim();
            if (symbol.Length == 0 || IsForbidden(symbol) || symbols.Contains(symbol))
            {
                continue;
            }

            symbols.Add(symbol);
        }

        for (var i = 0; i < RequiredDefines.Length; i++)
        {
            if (!symbols.Contains(RequiredDefines[i]))
            {
                symbols.Add(RequiredDefines[i]);
            }
        }

        var next = string.Join(";", symbols.ToArray());
        if (next != current)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, next);
            Debug.Log("Updated scripting defines for " + group + ": " + next);
        }
    }

    private static bool IsForbidden(string symbol)
    {
        for (var i = 0; i < ForbiddenDefines.Length; i++)
        {
            if (symbol == ForbiddenDefines[i])
            {
                return true;
            }
        }

        return false;
    }
}
