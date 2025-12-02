using CajunCoding;
using DrakiaXYZ.GildedKeyStorage.Helpers;
using DrakiaXYZ.GildedKeyStorage.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DrakiaXYZ.GildedKeyStorage;

[Injectable(InjectionType.Singleton)]
public class Config
{
    private readonly ModConfig config;
    public Config(FileUtil fileUtil, CustomJsonUtil jsonUtil)
    {
        // Load the config
        var modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var userConfigPath = Path.Join(modPath, "config", "config.json");
        var defaultConfigPath = Path.Join(modPath, "config", "config.default.json");

        // Copy the default config if no user config exists yet
        if (!Path.Exists(userConfigPath))
        {
            File.Copy(defaultConfigPath, userConfigPath);
        }

        // Merge the default and user config, to make sure we have any new properties
        // To be able to use SPT's converters, we first have to deserialize into a typed object, then convert to a Node
        // It's ugly, but it works
        var userConfig = jsonUtil.Deserialize<ModConfig>(fileUtil.ReadFile(userConfigPath));
        var userNode = jsonUtil.SerializeToNode(userConfig);
        var defaultConfig = jsonUtil.Deserialize<ModConfig>(fileUtil.ReadFile(defaultConfigPath));
        var defaultNode = jsonUtil.SerializeToNode(defaultConfig);
        var mergedConfig = defaultNode!.Merge(userNode, false);

        config = jsonUtil.Deserialize<ModConfig>(mergedConfig)!;
    }

    public ModConfig GetConfig()
    {
        return config;
    }
}