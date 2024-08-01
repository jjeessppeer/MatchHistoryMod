using HarmonyLib;
using System.IO;
using System.Net;

namespace MatchHistoryMod
{
    public class Uploader
    {
        //public const string ServerAddress = "http://statsoficarus.xyz";
        //public const string ServerAddress = "http://localhost";

        public static string PostPacket(UploadPacket packet, string path)
        {

            FileLog.Log("real upload start");
            string url = $"{MatchHistoryMod.BoundConfig.UploadUrl.Value}/{path}";
            FileLog.Log(url);
            var request = (HttpWebRequest)WebRequest.Create(url);
            var data = packet.GetByteEncoded();
            request.Method = "POST";
            request.Timeout = 2000;
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
                    string responseString = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                    return responseString;
                }
                else if (status == 14)
                {
                    return "Upload failed. Server unresponsive.";
                }
                else
                {
                    return "Upload failed.";
                }
            }
        }
    }
    
}
