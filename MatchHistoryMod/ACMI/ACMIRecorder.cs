//using HarmonyLib;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Text.RegularExpressions;
//using UnityEngine;
//using MuseBase.Multiplayer.Unity;
//using MuseBase.Multiplayer;

//namespace MatchHistoryMod.ACMI
//{
//    class ACMIRecorder
//    {
//        Dictionary<string, float> ShipLastTimestamp = new Dictionary<string, float>();
//        Dictionary<string, bool> ShipLastDead = new Dictionary<string, bool>();
//        const float PositionSnapshotInterval = 2;

//        public ACMIRecorder()
//        {
//            TryInitializeACMI();
//        }

//        private void Initialize()
//        {
//            if (MatchLobbyView.Instance == null || MatchLobbyView.Instance.Map == null)
//            {
//                MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Failed to initialize match recording."));
//                return;
//            }


//            int mapId = MatchLobbyView.Instance.Map.Id;
//            string mapName = MatchLobbyView.Instance.Map.NameText.En;
//            var date = DateTime.Now.ToUniversalTime();
//        }

//        private void TryInitializeACMI()
//        {
//            if (MatchLobbyView.Instance == null || MatchLobbyView.Instance.Map == null)
//            {
//                return;
//            }
//            // TODO: unreliable. Map not always an object.
//            FileLog.Log("RECORDER STARTED");
//            int mapId = MatchLobbyView.Instance.Map.Id;
//            FileLog.Log($"Mapid: {mapId}");
//            string mapName = MatchLobbyView.Instance.Map.NameText.En;
//            FileLog.Log($"Mapname: {mapName}");
//            float mapLongOffset = ACMIConstants.GetMapOffset(mapName) + 0.5f;
//            FileLog.Log($"offset: {mapLongOffset}");

//            var date = DateTime.Now.ToUniversalTime();
//            string dateStr = $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}";
//            FileLog.Log($"date: {dateStr}");

//            string header = "FileType=text/acmi/tacview\nFileVersion=2.2";
//            string config = $"0,ReferenceTime={dateStr}T00:00:00Z,ReferenceLongitude={mapLongOffset},ReferenceLatitude=0.5";
//            Output = $"{header}\n{config}";
//            //Output = "FileType=text/acmi/tacview\nFileVersion=2.2\n0,ReferenceTime=2000-01-01T00:00:00Z,ReferenceLongitude=0,ReferenceLatitude=0";
//            Output += $"\n#0\n1,T=0|0|0,Name=goio-enviro-{mapId},Color=Orange";
//            initialized = true;
//        }
        

//        public string GetShipACMIId(Ship ship)
//        {
//            return $"1000{ship.Side:X2}{ship.CrewIndex:X2}";
//        }
//        //public string GetProjectileACMIId()
//        //{
//        //    return "";
//        //}

//        public void StartTimer()
//        {
//            var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
//            GameStartTimestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond;
//        }

//        public static string vectorToTransformString(Vector3 vector, Vector3? heading = null)
//        {
//            const double earthCircumference = 6378137 * 2 * Math.PI; //40030173;
//            const double mToDeg = 360 / earthCircumference;
//            // Approximation for equator.
//            double longitude = vector.x * mToDeg;
//            double latitude = vector.z * mToDeg;
//            double altitude = vector.y;
//            string t = $"{longitude}|{latitude}|{altitude}";

//            if (heading.HasValue)
//            {
//                double roll = 0;
//                //double pitch = Math.Asin(heading.Value.y) * 360 / (2 * Math.PI);
//                double pitch = 0;
//                double yaw = -Math.Atan2(heading.Value.z, heading.Value.x) * 360 / (2 * Math.PI) + 90;
//                t = $"{t}|{roll}|{pitch}|{yaw}";
//            }
//            return t;
//        }

//        public float GetTimestampSeconds()
//        {
//            var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
//            long timestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond;
//            timestamp -= GameStartTimestamp;
//            return (float)timestamp/1000.0f;
//        }

//        private int projectileCount = 0;
//        private Dictionary<BaseShell, ShellInfo> activeShells = new Dictionary<BaseShell, ShellInfo>();

//        struct ShellInfo
//        {
//            public string Id;
//            public Vector3 LaunchPosition;
//            public float LaunchTimestamp;
//        }

//        public void ShellFired(BaseShell shell)
//        {
//            FileLog.Log($"SHELL LAUNCHED: {shell.GetInstanceID()} {shell.position}");
//            string id = $"1001{projectileCount++:D4}";
//            float timestamp = GetTimestampSeconds();
//            activeShells.Add(shell, new ShellInfo() {
//                Id = id,
//                LaunchPosition = shell.position,
//                LaunchTimestamp = timestamp
//            });
//            WriteEvent(timestamp, ACMISerializer.ShellFire(shell, projectileCount++));
//        }

//        public void ShellDetonated(BaseShell shell)
//        {
//            FileLog.Log($"SHELL DETONATED: {shell.GetInstanceID()} {shell.position}");
//            string id = activeShells[shell].Id;
//            float timestamp = GetTimestampSeconds();

//            // Interpolate shell arc
//            Vector3 LaunchPosition = activeShells[shell].LaunchPosition;
//            Vector3 HitPosition = shell.position;

//            float t = timestamp - activeShells[shell].LaunchTimestamp;
//            float gravity = shell.Network.MyView.GetAppFixed(4);
//            float d_y = HitPosition.y - LaunchPosition.y;
//            float d_x = HitPosition.x - LaunchPosition.x;
//            float d_z = HitPosition.z - LaunchPosition.z;

//            float v_x = d_x / t;
//            float v_z = d_z / t;
//            float v_y = d_y / t + gravity * t / 2;

//            const int interpolationSteps = 10;
//            for (int i = 1; i < interpolationSteps; i++)
//            {
//                float t_i = t * i / interpolationSteps;
//                Vector3 pos = new Vector3(
//                    LaunchPosition.x + v_x * t_i,
//                    LaunchPosition.y + v_y * t_i - gravity * t_i * t_i / 2,
//                    LaunchPosition.z + v_z * t_i
//                );
//                WriteEvent(activeShells[shell].LaunchTimestamp + t_i, $"{id},T={vectorToTransformString(pos)}");
//            }

//            // Write hit position and remove object.
//            WriteEvent(timestamp, $"{id},T={vectorToTransformString(shell.position)}");
//            WriteEvent(timestamp, $"-{id}");

//            activeShells.Remove(shell);
//        }

//        public void AddShipPosition(Ship ship)
//        {
//            string id = GetShipACMIId(ship);
//            float time = GetTimestampSeconds();

//            // Only add data if time has passed or ship died.
//            if (!ShipLastTimestamp.ContainsKey(id))
//            {
//                ShipLastTimestamp.Add(id, time);
//                ShipLastDead.Add(id, ship.IsDead);
//            }
//            else if (time - ShipLastTimestamp[id] < PositionSnapshotInterval && ShipLastDead[id] == ship.IsDead)
//            {
//                return;
//            }

//            ShipLastTimestamp[id] = time;
//            ShipLastDead[id] = ship.IsDead;

//            WriteEvent(time, ACMISerializer.ShipPosition(ship));
//        }



        
//        private Dictionary<int, RepairableStatus> previousRepairableStatus = new Dictionary<int, RepairableStatus>();
//        public void AddComponentChange(Repairable repairable)
//        {
//            //FileLog.Log($"{ repairable.Network.MyView.Fields.FieldsToString()}");
//            //float hp = repairable.Health;
//            //bool broken = repairable.NoHealth;
//            //int fireStacks = repairable.FireCharges;
//            //float buffDuration = repairable.BuffDuration;
//            //float buffProgress = repairable.BuffProgress;
//            if (repairable.Ship == null) return;

//            RepairableStatus status = new RepairableStatus(repairable) ;

//            int networkId = repairable.NetworkId;
//            if (!previousRepairableStatus.ContainsKey(networkId))
//            {
//                previousRepairableStatus.Add(networkId, status);
//            }
//            else if (status.Equals(previousRepairableStatus[networkId])) return;

//            Ship ship = repairable.Ship;
//            string id = GetShipACMIId(ship);

//            string evt = $"{id}";
//            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
//            string componentName = rgx.Replace(repairable.name, "");
//            RepairableStatus prevStatus = previousRepairableStatus[networkId];

//            if (prevStatus.Health != status.Health)
//            {
//                evt += $",{componentName}Health={status.Health}";
//            }
//            if (prevStatus.Health != status.Health)
//            {
//                evt += $",{componentName}MaxHealth={status.MaxHealth}";
//            }
//            if (prevStatus.Broken != status.Broken)
//            {
//                evt += $",{componentName}Broken={status.Broken}";
//            }
//            if (prevStatus.RepairProgress != status.RepairProgress)
//            {
//                evt += $",{componentName}RepairProgress={status.RepairProgress}";
//            }

//            previousRepairableStatus[networkId] = status;

//            FileLog.Log(evt);

//            WriteEvent(GetTimestampSeconds(), evt);
//        }
//    }
//}
