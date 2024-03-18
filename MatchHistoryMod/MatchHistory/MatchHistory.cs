using MuseBase.Multiplayer;
using MuseBase.Multiplayer.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatchHistoryMod.MatchHistory
{
    class MatchHistory
    {
        public static void SaveMatchHistory()
        {
            if (!MatchLobbyView.Instance || !Mission.Instance) return;
            LobbyData lobbyData = new LobbyData(MatchLobbyView.Instance, Mission.Instance)
            {
                MatchTime = (int)Math.Round(MatchLobbyView.Instance.ElapsedTime.TotalSeconds)
            };
            MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Uploading match history..."));
            UploadPacket packet = new LobbyUploadPacket(lobbyData);
            string response = Uploader.UploadMatchData(packet);
            MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console(response));
        }
    }
}
