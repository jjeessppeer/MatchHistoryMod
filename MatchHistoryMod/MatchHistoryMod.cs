using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BepInEx;
using HarmonyLib;
using UnityEngine;


using Newtonsoft.Json;

using Muse.Goi2.Entity;

namespace MatchHistoryMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class MatchHistoryMod : BaseUnityPlugin
    {
        public const string pluginGuid = "whereami.matchhistory.mod";
        public const string pluginName = "Match History Mod";
        public const string pluginVersion = "0.1";

        public void Awake()
        {
            var harmony = new Harmony(pluginGuid);
            harmony.PatchAll();
        }
    }
    
}
