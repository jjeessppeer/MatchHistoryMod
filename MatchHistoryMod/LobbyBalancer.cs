using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HarmonyLib;
using Newtonsoft.Json;
using MuseBase.Multiplayer;
using MuseBase.Multiplayer.Unity;
using System.Net;
using System.IO;

namespace LobbyBalancer
{
    [HarmonyPatch]
    class LobbyBalancer
    {
        static string ServerUrl = "http://localhost";


        public class BalanceRequestData
        {
            public List<int> playerIds;
            public int teamCount;
            public int teamSize;
            public int randomness;
            public bool keepPilots;
        }

        public class TeamData
        {
            public int teamIdx;
            public float balanceElo;
            public int realElo;
            public int memberCount;
            public List<string> playerNames;
            public List<int> playerElos;
        }

        

        private static void RequestLobbyBalance(BalanceRequestData requestData)
        {
            const string apiURL = "http://localhost/balance_lobby";
            string requestJSON = JsonConvert.SerializeObject(requestData);
            var packet = Encoding.ASCII.GetBytes(requestJSON);
            var request = (HttpWebRequest)WebRequest.Create(apiURL);
            request.Method = "POST";
            request.Timeout = 1000;
            request.ContentType = "application/json";
            request.ContentLength = packet.Length;

            try
            {
                Console.WriteLine("Starting api request");
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(packet, 0, packet.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                int responseCode = (int)response.StatusCode;
                Console.WriteLine(responseString);
                Console.WriteLine(responseCode);

                var teams = JsonConvert.DeserializeObject<List<TeamData>>(responseString);
                PublishBalancedTeams(teams);
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
                    MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("API server unresponsive."));
                }
                else
                {
                }
            }

        }

        private static void PublishBalancedTeams(List<TeamData> teams)
        {
            string msg = ".";
            for (int i = 0; i < teams.Count; ++i)
            {
                msg += $"\n__Team {i + 1}__ (mmr: {teams[i].realElo / teams[i].memberCount})";
                for (int p = 0; p < teams[i].memberCount; ++p)
                {
                    msg += $"\n {teams[i].playerNames[p]}";
                }
                //msg += "\n";
            }
            Console.WriteLine(msg);
            MuseWorldClient.Instance.ChatHandler.TrySendMessage(msg+ msg+ msg+msg + msg + msg + msg + msg + msg, "match");

        }

        private static BalanceRequestData LoadBalanceRequest(bool keepPilots, int randomness)
        {
            MatchLobbyView mlv = MatchLobbyView.Instance;
            List<int> playerIds = new List<int>();
            for (int c = 0; c < mlv.FlatCrews.Count; ++c)
            {
                var crew = mlv.FlatCrews[c];
                for (int i = 0; i < 4; ++i)
                {
                    if (crew.Slots[i] == null || crew.Slots[i].PlayerEntity == null) playerIds.Add(-1);
                    else playerIds.Add(crew.Slots[i].PlayerEntity.Id);
                }
            }

            BalanceRequestData req = new BalanceRequestData
            {
                playerIds = playerIds,
                teamCount = mlv.TeamCount,
                teamSize = mlv.CrewCount / mlv.TeamCount,
                randomness = randomness,
                keepPilots = false,
            };

            string serialized = JsonConvert.SerializeObject(req);

            Console.WriteLine(req);
            Console.WriteLine(serialized);
            return req;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MuseBase.Multiplayer.MessageClient), "AddMessage")]
        private static void ParseSentChatMessage(ChatMessage msg, MessageClient __instance)
        {
            string text = msg.Message;
            string[] words = text.Split(' ');
            if (words.Length == 0) return;
            if (words[0] != "/balance") return;

            if (words.Length >= 2 && words[1] == "help")
            {
                ChatMessage chatMsg = new ChatMessage
                {
                    Type = ChatMessage.MessageType.Console,
                    //UserName = "",
                    Message = 
                        "/balance [KeepPilots:optional] [randomness:optional]\n" +
                        "KeepPilots: Include to keep current pilots.\n" +
                        "Randomness: Include a number to increase randomness of lobby balance. Reasonable value 0-200."
                };
                __instance.AddMessage(chatMsg);
                return;
            }

            var req = LoadBalanceRequest(false, 100);
            RequestLobbyBalance(req);
            //foreach (var word in words)
            //{
            //    Console.WriteLine($"<{word}>");
            //}
        }
    }



}
