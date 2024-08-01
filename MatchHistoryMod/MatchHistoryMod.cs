using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;

namespace MatchHistoryMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class MatchHistoryMod : BaseUnityPlugin
    {
        public const string pluginGuid = "whereami.matchhistory.mod";
        public const string pluginName = "Match History Mod";
        public const string pluginVersion = "2.0.0";

        internal static ModConfig BoundConfig { get; private set; } = null;

        public void Awake()
        {
            BoundConfig = new ModConfig(base.Config);

            var harmony = new Harmony(pluginGuid);
            harmony.PatchAll();
        }

        internal class ModConfig
        {
            // We define our config variables in a public scope
            public readonly ConfigEntry<string> UploadUrl;
            public readonly ConfigEntry<bool> SaveReplays;

            public ModConfig(ConfigFile cfg)
            {
                UploadUrl = cfg.Bind(
                    "Server",
                    "URL",
                    "localhost",
                    "The url of the upload server.");
                SaveReplays = cfg.Bind(
                    "Replays",
                    "SaveLocally",
                    false,
                    "Save .acmi replays locally. (saved in <GAME_DIRECTORY>/replays)");
            }
        }

    }
}

    
