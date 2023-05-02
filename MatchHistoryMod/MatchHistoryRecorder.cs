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
        public GameData GameData;
        public LobbyData LobbyData;
        public byte[] GetEncoded()
        {
            string json = JsonConvert.SerializeObject(this);
            byte[] bytes = Encoding.ASCII.GetBytes(json);
            return bytes;
        }
        public byte[] GetCompressed()
        {
            var data = GetEncoded();
            FileLog.Log($"Uncompressed size: {data.Length}");
            MemoryStream output = new MemoryStream();
            using (GZipStream dstream = new GZipStream(output, CompressionMode.Compress))
            {
                dstream.Write(data, 0, data.Length);
            }
            byte[] outArr = output.ToArray();
            string outStr = Convert.ToBase64String(outArr);
            byte[] encoded = Encoding.ASCII.GetBytes(outStr);

            FileLog.Log($"Compressed size: {encoded.Length}");
            return encoded;
        }
    }
    public class CompressedPacket
    {
        public string ModVersion;
        public string MatchId;
        public LobbyData LobbyData;
        public string GameData;
        public CompressedPacket(UploadPacket original)
        {
            ModVersion = original.ModVersion;
            MatchId = original.LobbyData.MatchId;
            LobbyData = original.LobbyData;
            GameData = SerializeAndCompress(original.GameData);
        }
        public byte[] GetBytes()
        {
            string json = JsonConvert.SerializeObject(this);
            byte[] data = Encoding.ASCII.GetBytes(json);
            return data;
        }
        public string SerializeAndCompress(object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
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
    }

    [HarmonyPatch]
    public static class MatchHistoryRecorder
    {
        public static void UploadMatchData(UploadPacket packet)
        {
            MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Uploading match history..."));
            FileLog.Log($"Starting upload...");
            //const string _UploadURL = "http://statsoficarus.xyz/submit_match_history";
            const string _UploadURL = "http://localhost/submit_match_history";
            var request = (HttpWebRequest)WebRequest.Create(_UploadURL);
            CompressedPacket compressedPacket = new CompressedPacket(packet);
            var data = compressedPacket.GetBytes();
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
            //FileLog.Log($"Upload finished.\n{recordJSON}");
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), "Start")]
        private static void MissionStarted()
        {
            FileLog.Log($"Mission started");
            MatchDataRecorder.Reset();
        }

        // Called when match ends and post game screen is shown.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIManager.UIMatchCompleteState), "Enter")]
        private static void MatchComplteStateEnter()
        {
            //LobbyData d = new LobbyData(MatchLobbyView.Instance, Mission.Instance);
            //UploadMatchData(d);
            FileLog.Log("Match ended.");
            // Use to get time of match and some other stats
            MatchActions.GetMatchStats(new Muse.Networking.ExtensionResponseDelegate(GetMatchStats));

            File.WriteAllText("FullMatchData.json", MatchDataRecorder.GetJSONDump());
            File.WriteAllText("AccuracyTable.txt", MatchDataRecorder.GetTableDump());
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

            LobbyData d = new LobbyData(MatchLobbyView.Instance, Mission.Instance)
            {
                MatchTime = dictionary5["Time Completed"]
            };
            //FileLog.Log(JsonConvert.SerializeObject(d));

            Thread uploadThread = new Thread(() => UploadMatchData(new UploadPacket()
            {
                LobbyData = d,
                GameData = MatchDataRecorder.ActiveGameData
            }));
            uploadThread.Start();
            //UploadMatchData(new UploadPacket()
            //{
            //    LobbyData = d,
            //    GameData = MatchDataRecorder.ActiveGameData
            //});
        }
    }


}
