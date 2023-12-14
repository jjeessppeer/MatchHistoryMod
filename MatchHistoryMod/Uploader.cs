using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO.Compression;
using System.IO;
using System.Net;

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

        public static string UploadMatchData(UploadPacket packet)
        {
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
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                int responseCode = (int)response.StatusCode;
                return responseString;
            }
            catch (System.Net.WebException e)
            {
                int status = (int)e.Status;
                if (status == 7)
                {
                    var responseString = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                    return responseString;
                }
                else
                {
                    return "Upload failed...";
                }
            }
        }
    }
}
