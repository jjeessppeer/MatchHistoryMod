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
    public class UploadPacket
    {
        public string ModVersion = MatchHistoryMod.pluginVersion;
        public string MatchId;
        public string CompressedGunneryData;
        public string CompressedPositionData;
        public LobbyData LobbyData;

        public UploadPacket(LobbyData lobbyData, GunneryData gunneryData, List<ShipPositionData> positionData)
        {
            ModVersion = MatchHistoryMod.pluginVersion;
            LobbyData = lobbyData;
            MatchId = lobbyData.MatchId;
            CompressedGunneryData = SerializeAndCompress(gunneryData);
            CompressedPositionData = SerializeAndCompress(positionData);
        }

        public static string SerializeAndCompress(object obj)
        {
            string json = JsonConvert.SerializeObject(obj, new VectorJsonConverter());
            byte[] data = Encoding.ASCII.GetBytes(json);
            MemoryStream output = new MemoryStream();
            using (GZipStream dstream = new GZipStream(output, CompressionMode.Compress))
            {
                dstream.Write(data, 0, data.Length);
            }
            byte[] outArr = output.ToArray();
            string outStr = Convert.ToBase64String(outArr);
            return outStr;
        }

        public byte[] GetByteEncoded()
        {
            string json = JsonConvert.SerializeObject(this, new VectorJsonConverter());
            byte[] bytes = Encoding.ASCII.GetBytes(json);
            return bytes;
        }
    }


    [HarmonyPatch]
    public static class MatchHistoryRecorder
    {
        public static void UploadMatchData(UploadPacket packet)
        {
            MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Uploading match history..."));
            //const string _UploadURL = "http://statsoficarus.xyz/submit_match_history";
            const string _UploadURL = "http://localhost/submit_match_history";
            var request = (HttpWebRequest)WebRequest.Create(_UploadURL);
            var data = packet.GetByteEncoded();
            request.Method = "POST";
            request.Timeout = 8000;
            request.ContentType = "application/json";
            request.ContentLength = data.Length;

            try
            {
                //MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Uploading match history..."));
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                int responseCode = (int)response.StatusCode;
            }
            catch (System.Net.WebException e)
            {
                int status = (int)e.Status;
                if (status == 7)
                {
                    var responseString = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                    MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console(responseString));
                }
                else if (status == 14)
                {
                    MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Upload failed: Match history server unresponsive."));
                }
                else
                {
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), "Start")]
        private static void MissionStarted()
        {
            MatchDataRecorder.Reset();
        }

        // Called when match ends and post game screen is shown.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIManager.UIMatchCompleteState), "Enter")]
        private static void MatchComplteStateEnter()
        {
            // Use to get time of match and some other stats
            MatchActions.GetMatchStats(new Muse.Networking.ExtensionResponseDelegate(GetMatchStats));
            //File.WriteAllText("FullMatchData.json", MatchDataRecorder.GetJSONDump());
            //File.WriteAllText("AccuracyTable.txt", MatchDataRecorder.GetTableDump());
        }


        public static void GetMatchStats(Muse.Networking.ExtensionResponse resp)
        {
            // TODO: remove 90% of this.
            JsonData jsonData = resp.JsonData;
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            IDictionary dictionary2 = jsonData["stats"];
            if (dictionary2 != null)
            {
                IDictionaryEnumerator enumerator = dictionary2.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        object obj = enumerator.Current;
                        DictionaryEntry dictionaryEntry = (DictionaryEntry)obj;
                        dictionary[dictionaryEntry.Key.ToString()] = (int)((JsonData)dictionaryEntry.Value);
                    }
                }
                finally
                {
                    IDisposable disposable;
                    if ((disposable = (enumerator as IDisposable)) != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
            else
            {
                MuseLog.InfoFormat("no statsData", new object[0]);
            }
            Dictionary<int, float> dictionary3 = new Dictionary<int, float>();
            IDictionary dictionary4 = jsonData["quests"];
            if (dictionary4 != null)
            {
                IDictionaryEnumerator enumerator2 = dictionary4.GetEnumerator();
                try
                {
                    while (enumerator2.MoveNext())
                    {
                        object obj2 = enumerator2.Current;
                        DictionaryEntry dictionaryEntry2 = (DictionaryEntry)obj2;
                        int num = -1;
                        if (int.TryParse(dictionaryEntry2.Key.ToString(), out num) && num != -1)
                        {
                            dictionary3[num] = (float)((JsonData)dictionaryEntry2.Value);
                        }
                    }
                }
                finally
                {
                    IDisposable disposable2;
                    if ((disposable2 = (enumerator2 as IDisposable)) != null)
                    {
                        disposable2.Dispose();
                    }
                }
            }
            else
            {
                MuseLog.InfoFormat("no quests data", new object[0]);
            }
            Dictionary<string, int> dictionary5 = new Dictionary<string, int>();
            IDictionary dictionary6 = jsonData["crewStats"];
            if (dictionary6 != null)
            {
                IDictionaryEnumerator enumerator3 = dictionary6.GetEnumerator();
                try
                {
                    while (enumerator3.MoveNext())
                    {
                        object obj3 = enumerator3.Current;
                        DictionaryEntry dictionaryEntry3 = (DictionaryEntry)obj3;
                        string key = dictionaryEntry3.Key.ToString();
                        dictionary5[key] = (int)((JsonData)dictionaryEntry3.Value);
                    }
                }
                finally
                {
                    IDisposable disposable3;
                    if ((disposable3 = (enumerator3 as IDisposable)) != null)
                    {
                        disposable3.Dispose();
                    }
                }
                UIMatchEndCrewPanel.instance.SetStats(dictionary5);
            }
            else
            {
                MuseLog.InfoFormat("no crewStats data", new object[0]);
            }

            LobbyData lobbyData = new LobbyData(MatchLobbyView.Instance, Mission.Instance)
            {
                MatchTime = dictionary5["Time Completed"]
            };
            //FileLog.Log(JsonConvert.SerializeObject(d));

            Thread uploadThread = new Thread(() =>
                UploadMatchData(new UploadPacket(lobbyData, MatchDataRecorder.ActiveGunneryData, MatchDataRecorder.ShipPositions))
            );
            uploadThread.Start();

        }
    }


}
