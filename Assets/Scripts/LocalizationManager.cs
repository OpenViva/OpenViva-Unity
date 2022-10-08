using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class LocalizationManager
{
    /// <summary>
    /// Returns a String from the specified table and entry
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="entryName"></param>
    /// <returns>A Localized String</returns>
    public static string GetLocalizedStringFromTable(string tableName, string entryName)
    {
        var stringtable = LocalizationSettings.StringDatabase.GetTable(tableName);
        return GetLocalizedString(stringtable, entryName);
    }

    private static string GetLocalizedString(StringTable table, string entryName)
    {
        var entry = table.GetEntry(entryName);
        return entry.GetLocalizedString(); 
    }
}
