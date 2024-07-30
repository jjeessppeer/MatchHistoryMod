using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BepInEx;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;


using Newtonsoft.Json;

using Muse.Goi2.Entity;

namespace MatchHistoryMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class MatchHistoryMod : BaseUnityPlugin
    {
        public const string pluginGuid = "whereami.matchhistory.mod";
        public const string pluginName = "Match History Mod";
        public const string pluginVersion = "2.0.0";

        public ConfigEntry<string> configUploadUrl;
        public ConfigEntry<bool> configSaveReplays;

        public void Awake()
        {
            // Set up config.
            configUploadUrl = Config.Bind(
                "Server", 
                "URL", 
                "localhost", 
                "The url of the upload server.");
            configSaveReplays = Config.Bind(
                "Replays", 
                "Save locally", 
                false, 
                "Save .acmi replays locally. (saved in <GAME_DIRECTORY>/replays)");

            string s = configUploadUrl.Value;
            string s1 = (string) Config["Server", "URL"].BoxedValue;
            FileLog.Log(s1);
            FileLog.Log("aaa");

            FileLog.Log(s1);

            var harmony = new Harmony(pluginGuid);
            harmony.PatchAll();
        }
    }
    
}
