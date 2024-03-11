using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using MuseBase.Multiplayer.Unity;
using MuseBase.Multiplayer;

namespace MatchHistoryMod.ACMI
{
    [HarmonyPatch]
    class MatchRecorderPatch
    {
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(MatchLobbyView), "Start")]
        //private static void MissionStart()
        //{
        //    // Called after a new match is started.
        //    FileLog.Log("Initializing mission.");
        //    MatchRecorder.Start();
        //    FileLog.Log("Recorder intialized.");
        //}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), "Start")]
        private static void MissionStart(Mission __instance)
        {
            // Called after a new match is started.
            FileLog.Log("Initializing mission.");
            MatchRecorder.Start(__instance);
            FileLog.Log("Recorder intialized.");
        }

        // Called when match ends and post game screen is shown.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIManager.UIMatchCompleteState), "Enter")]
        private static void MatchComplteStateEnter()
        {
            MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Saving replay..."));
            MatchRecorder.Stop();
            MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Done."));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Ship), "OnRemoteUpdate")]
        private static void ShipUpdate(Ship __instance)
        {
            // Update ship position.
            MatchRecorder.CurrentMatchRecorder?.UpdateShipPosition(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BaseShell), "OnLaunch")]
        private static void ShellLaunched(BaseShell __instance)
        {
            // Shell projectile was fired.
            MatchRecorder.CurrentMatchRecorder?.ShellFired(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BaseShell), "OnShellDestruction")]
        private static void ShellDestruction(BaseShell __instance)
        {
            MatchRecorder.CurrentMatchRecorder?.ShellDetonated(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Repairable), "OnRemoteUpdate")]
        private static void RepairableUpdated(Repairable __instance)
        {
            //FileLog.Log($"Repairable updated");
        }
    }

    
}
