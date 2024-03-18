using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using System.IO.Compression;
using System.IO;


namespace MatchHistoryMod
{
    public class VectorJsonConverter : JsonConverter<Vector3>
    {
        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, Vector3 value, Newtonsoft.Json.JsonSerializer serializer)
        {
            //writer.WriteValue(value.ToString());
            writer.WriteValue($"[{value.x},{value.y},{value.z}]");
        }
        public override Vector3 ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {

            throw new NotImplementedException();
        }
    }

    public class UploadPacket
    {
        public string ModVersion = MatchHistoryMod.pluginVersion;

        //public static string SerializeAndCompress(object obj)
        //{
        //    string json = JsonConvert.SerializeObject(obj, new VectorJsonConverter());
        //    byte[] data = Encoding.ASCII.GetBytes(json);
        //    MemoryStream output = new MemoryStream();
        //    using (GZipStream dstream = new GZipStream(output, CompressionMode.Compress))
        //    {
        //        dstream.Write(data, 0, data.Length);
        //    }
        //    byte[] outArr = output.ToArray();
        //    string outStr = Convert.ToBase64String(outArr);
        //    return outStr;
        //}

        public byte[] GetByteEncoded()
        {
            string json = JsonConvert.SerializeObject(this, new VectorJsonConverter());
            byte[] bytes = Encoding.ASCII.GetBytes(json);
            return bytes;
        }
    }

    public class LobbyUploadPacket : UploadPacket
    {
        public string MatchId;
        public MatchHistory.LobbyData LobbyData;

        public LobbyUploadPacket(MatchHistory.LobbyData lobbyData)
        {
            LobbyData = lobbyData;
            MatchId = lobbyData.MatchId;
        }
    }

    public class ReplayUploadPacket : UploadPacket
    {
        public string MatchId;
        public string AcmiString;

        public ReplayUploadPacket(ACMI.AcmiFile acmiFile, string matchId)
        {
            MatchId = matchId;
            AcmiString = acmiFile.ToString();
        }
    }
}
