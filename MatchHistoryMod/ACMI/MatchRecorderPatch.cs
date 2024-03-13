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
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), "Start")]
        private static void MissionStart(Mission __instance)
        {
            FileLog.Log("Initializing recorder...");
            MatchRecorder.InitializeRecorder(__instance);
            FileLog.Log("done.");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MatchLobbyView), "OnRemoteUpdate")]
        private static void MlvUpdate(MatchLobbyView __instance)
        {
            if (MatchRecorder.InitializingMatchRecorder == null) return;
            FileLog.Log("Starting recorder...");
            MatchRecorder.StartRecorder();
            FileLog.Log("done.");
        }

        // Called when match ends and post game screen is shown.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Mission), "OnDisable")]
        private static void MissionOnDisable()
        {
            FileLog.Log("Mission disabled...");
            MatchRecorder.StopRecorder();
        }

        // Called when match ends and post game screen is shown.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIManager.UIMatchCompleteState), "Enter")]
        private static void MatchComplteStateEnter()
        {
            FileLog.Log("Match completed...");
            MatchRecorder.StopRecorder();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Ship), "OnRemoteUpdate")]
        private static void ShipUpdate(Ship __instance)
        {
            // Update ship position.
            MatchRecorder.CurrentMatchRecorder?.UpdateShipPosition(__instance);
        }

        // Shell projectile was fired.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BaseShell), "OnLaunch")]
        private static void ShellLaunched(BaseShell __instance)
        {
            MatchRecorder.CurrentMatchRecorder?.ShellFired(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BaseShell), "OnShellDestruction")]
        private static void ShellDestruction(BaseShell __instance)
        {
            MatchRecorder.CurrentMatchRecorder?.ShellDetonated(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Repairable), "Update")]
        private static void RepairableUpdated(Repairable __instance)
        {
            MatchRecorder.CurrentMatchRecorder?.RepairableUpdate(__instance);
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(NetworkedPlayer), "Update")]
        //private static void PlayerUpdate(Repairable __instance)
        //{
        //    MatchRecorder.CurrentMatchRecorder?.RepairableUpdate(__instance);
        //}
    }


}
