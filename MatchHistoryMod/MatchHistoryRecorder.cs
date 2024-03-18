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
        // Called when match ends and post game screen is shown.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIManager.UIMatchCompleteState), "Enter")]
        private static void MatchComplteStateEnter()
        {
            // Use to get time of match and some other stats
            //MatchActions.GetMatchStats(new Muse.Networking.ExtensionResponseDelegate(GetMatchStats));

            LobbyData lobbyData = new LobbyData(MatchLobbyView.Instance, Mission.Instance)
            {
                MatchTime = (int)Math.Round(MatchLobbyView.Instance.ElapsedTime.TotalSeconds)
            };


            MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Uploading match history..."));
            string reply = Uploader.UploadMatchData(new LobbyUploadPacket(lobbyData));
            MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console(reply));
            //Thread uploadThread = new Thread(() =>
            //{
            //    MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Uploading match history..."));
            //    string reply = UploadPacket.UploadMatchData(new UploadPacket(lobbyData));
            //    MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console(reply));
            //});
            //uploadThread.Start();
            //File.WriteAllText("AccuracyTable.txt", MatchDataRecorder.GetTableDump());
            MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Done."));
        }


    }


}
