using SPTarkov.Server.Core.Models.Common;
using System.Text.Json.Serialization;

namespace DrakiaXYZ.GildedKeyStorage.Models;

public class ModConfig
{
    public bool KeyInsuranceEnabled { get; set; }
    public bool CasesInsuranceEnabled { get; set; }
    public bool CasesFleaBanned { get; set; }
    public bool WeightlessKeys { get; set; }
    public bool NoKeyUseLimit { get; set; }
    public bool KeysAreDiscardable { get; set; }
    public bool AllKeysInSecure { get; set; }
    public bool AllowCasesInSpecial { get; set; }
    public bool AllowCasesInSecure { get; set; }
    public bool AllowCasesInBackpacks { get; set; }
    public MongoId[] CasesAllowedIn { get; set; } = [];
    public MongoId[] CasesDisallowedIn { get; set; } = [];
    public bool UseFiniteKeysList { get; set; }
    public MongoId[] FiniteKeysList { get; set; } = [];
    public ModDebugConfig? Debug { get; set; }
}

public class ModDebugConfig
{
    public bool LogMissingKeys { get; set; }
    public bool LogRareKeys { get; set; }
}