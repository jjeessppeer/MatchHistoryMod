using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BepInEx;
using HarmonyLib;
using UnityEngine;


using Newtonsoft.Json;

using Muse.Goi2.Entity;

using System.IO;
using System.Net;
using System.Collections;
using LitJson;
using MuseBase.Multiplayer.Unity;
using MuseBase.Multiplayer;
using System.IO.Compression;
using System.Threading;

namespace MatchHistoryMod
{
    [HarmonyPatch]
    public static class MatchHistoryRecorder
    {
        static ACMI.ACMIRecorder recorder;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), "Start")]
        private static void MissionStarted()
        {
            FileLog.Log("Initializing mission.");
            MatchDataRecorder.Reset();
            recorder = new ACMI.ACMIRecorder();
            recorder.StartTimer();
            FileLog.Log("Recorder intialized.");
        }

        // Called when match ends and post game screen is shown.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIManager.UIMatchCompleteState), "Enter")]
        private static void MatchComplteStateEnter()
        {
            // Use to get time of match and some other stats
            //MatchActions.GetMatchStats(new Muse.Networking.ExtensionResponseDelegate(GetMatchStats));

            MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Saving replay..."));
            File.WriteAllText("MatchRecording.acmi", recorder.Output);
            recorder = null;
            //LobbyData lobbyData = new LobbyData(MatchLobbyView.Instance, Mission.Instance)
            //{
            //    MatchTime = (int)Math.Round((double)recorder.GetTimestampSeconds())
            //};
            //Thread uploadThread = new Thread(() =>
            //{
            //    MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Uploading match history..."));
            //    string reply = UploadPacket.UploadMatchData(new UploadPacket(lobbyData, MatchDataRecorder.ActiveGunneryData, MatchDataRecorder.ShipPositions));
            //    MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console(reply));
            //});
            //uploadThread.Start();
            //File.WriteAllText("AccuracyTable.txt", MatchDataRecorder.GetTableDump());
            MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Done."));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Ship), "OnRemoteUpdate")]
        private static void ShipUpdate(Ship __instance)
        {
            try
            {
                recorder.AddShipPosition(__instance);
            }
            catch {
                FileLog.Log("Failed to add ship position");
            }
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Turret), "Fire")]
        //private static void TurretFire(Turret __instance)
        //{

        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Turret), "Fire")]
        //private static void TurretFire(Turret __instance)
        //{
        //    FileLog.Log("Turret fired");
        //    //__instance.shots
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(BallisticShell), "OnRemoteUpdate")]
        //private static void ballisticUpdate(BallisticShell __instance)
        //{
        //    FileLog.Log($"BALLISTIC UPDATE");
        //}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BaseShell), "OnLaunch")]
        private static void ShellLaunched(BaseShell __instance)
        {
            //FileLog.Log($"SHELL LAUNCHED: {__instance.position}");
            //FileLog.Log($"Fields\n{__instance.Network.MyView.Fields.FieldsToString()}");


            //float gravity = __instance.Network.MyView.GetAppFixed(4);
            //Vector3 velocity = __instance.Network.GetFixedVector(1);
            //Vector3 position = __instance.position;
            //FileLog.Log($"gravity: {gravity}");
            if (recorder == null) return;
            recorder.ShellFired(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BaseShell), "OnShellDestruction")]
        private static void ShellDestruction(BaseShell __instance)
        {
            if (recorder == null) return;
            recorder.ShellDetonated(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Repairable), "OnRemoteUpdate")]
        private static void RepairableUpdated(Repairable __instance)
        {
            if (recorder == null) return;
            //{ __instance.Network.MyView.Fields.FieldsToString() }
            recorder.AddComponentChange(__instance);
            

        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Turret), "OnRemoteUpdate")]
        //private static void TurretUpdate(Turret __instance)
        //{
        //    int? oldAmmunition = Traverse.Create(__instance).Field("oldAmmunition").GetValue() as int?;
        //    int? ammounition = Traverse.Create(__instance).Field("ammunition").GetValue() as int?;
        //    int? clipSize = Traverse.Create(__instance).Field("ammunitionClipSize").GetValue() as int?;
        //    bool? reload = Traverse.Create(__instance).Field("reload").GetValue() as bool?;

        //    if (NetworkedPlayer.Local.CurrentShip != __instance.Ship) return;
        //    if (oldAmmunition == 0)
        //    {
        //        oldAmmunition = clipSize;
        //    }
        //    if (oldAmmunition <= ammounition) return; // Continue only if ammo decreased, meaning shot was fired.
        //    if (ammounition == 0 && oldAmmunition >= 2 && reload == true)
        //    {
        //        // Gun was reloaded early.
        //        // TODO: better conditions for low ammo guns
        //        // Will currently count reload at 1 ammo as a shot.
        //        return;
        //    }

        //    int shotsFired = (int)(oldAmmunition - ammounition);
        //    //FileLog.Log($"Fired {shotsFired} shots. R:{reload} O:{oldAmmunition} N:{ammounition}");
        //    for (int i = 0; i < shotsFired; i++)
        //    {
        //        try
        //        {
        //            recorder.TurretFired(__instance);
        //        }
        //        catch
        //        {

        //        }
        //    }

        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Ship), "OnRemoteDestroy")]
        //private static void ShipDestroy(Ship __instance)
        //{
        //    //ShipPositionData.TakeSnapshot(__instance, ShipPositions);
        //}
    }


}
