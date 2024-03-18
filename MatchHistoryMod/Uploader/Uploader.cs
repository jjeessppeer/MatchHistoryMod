using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO.Compression;
using System.IO;
using System.Net;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BepInEx;
using HarmonyLib;
using UnityEngine;



using Muse.Goi2.Entity;

using System.Collections;
using LitJson;
using MuseBase.Multiplayer.Unity;
using MuseBase.Multiplayer;
using System.Threading;

namespace MatchHistoryMod
{
    public class Uploader
    {
        //const string SERVER_ADDRESS = "http://statsoficarus.xyz";
        const string SERVER_ADDRESS = "http://localhost";

        public static string UploadMatchData(UploadPacket packet)
        {
            string _UploadURL = $"{SERVER_ADDRESS}/submit_match_history";
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
                    string responseString = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
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
