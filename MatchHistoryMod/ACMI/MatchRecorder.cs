using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace MatchHistoryMod.ACMI
{
    class MatchRecorder
    {
        private static MatchRecorder ActiveMatchRecorder;

        static void Start()
        {
            ActiveMatchRecorder = new MatchRecorder();
        }
        static void Stop()
        {
            ActiveMatchRecorder = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Ship), "OnRemoteUpdate")]
        private static void ShipUpdate(Ship __instance)
        {
            try
            {
                recorder.AddShipPosition(__instance);
            }
            catch
            {
                FileLog.Log("Failed to add ship position");
            }
        }
    }
}
