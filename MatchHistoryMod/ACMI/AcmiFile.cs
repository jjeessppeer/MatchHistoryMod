using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace MatchHistoryMod.ACMI
{
    class AcmiFile
    {
        string buffer = "";

        public void Save(string path)
        {

        }

        public void WriteHeader(int mapId, string mapName, DateTime date)
        {
            float mapLongOffset = ACMIConstants.GetMapOffset(mapName) + 0.5f;
            string dateStr = $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}";

            string header = "FileType=text/acmi/tacview\nFileVersion=2.2";
            string config = $"0,ReferenceTime={dateStr}T00:00:00Z,ReferenceLongitude={mapLongOffset},ReferenceLatitude=0.5";
            string mapItem = $"\n#0\n1,T=0|0|0,Name=goio-enviro-{mapId},Color=Orange";
            buffer += $"{header}\n{config}\n{mapItem}";
        }

        public void RegisterShip()
        {

        }

        public void WriteShipPosition(Ship ship, float timestamp)
        {
            string id = GetShipACMIId(ship);
            string transform = ACMIRecorder.vectorToTransformString(ship.position, ship.Forward);
            string evt = $"{id},T={transform},Name=goio-ship-{ship.ShipModelId},CallSign={ship.name},Color={ACMIConstants.GetColor(ship.Side)}";
            buffer += $"\n#{timestamp}\n{evt}";
        }

        public void AddShell(BaseShell shell, float timestamp, int index)
        {
            string id = $"1001{index:D4}";

            string shipId = "1000";
            string shooter = "Unknown";
            int turretType = 0;
            int turretSlot = -1;
            int side = -1;
            try
            {
                PropertyInfo property = typeof(BaseShell).GetProperty("TurretLaunchedFrom", BindingFlags.NonPublic | BindingFlags.Instance);
                var value = property.GetValue(shell, null);
                Turret turret = (Turret)value;
                Ship ship = turret.Ship;
                NetworkedPlayer player = turret.UsingPlayer;
                side = ship.Side;
                if (player != null)
                {
                    if (player.UserId == 0) shooter = "AI";
                    else shooter = player.name;
                }
                shipId = GetShipACMIId(ship);
                turretType = turret.ItemId;
            }
            catch (Exception e) { }
            string transform = VectorToTransform(shell.position);
            string evt = $"{id},T={transform},Name=goio-projectile-{turretType},Parent={shipId},Color={ACMIConstants.GetColor(side)}";
            buffer += $"\n#{timestamp}\n{evt}";
        }

        public void DetonateShell(string shellId, Vector3 launchPosition, Vector3 hitPosition, float launchTimestamp, float hitTimestamp, float gravity)
        {
            // Interpolate shell arc
            float t = hitTimestamp - launchTimestamp;
            float v_x = (hitPosition.x - launchPosition.x) / t;
            float v_z = (hitPosition.z - launchPosition.z) / t;
            float v_y = (hitPosition.y - launchPosition.y) / t + gravity * t / 2;

            const int interpolationSteps = 10;
            for (int i = 1; i <= interpolationSteps; i++)
            {
                float t_i = t * i / interpolationSteps;
                Vector3 pos = new Vector3(
                    launchPosition.x + v_x * t_i,
                    launchPosition.y + v_y * t_i - gravity * t_i * t_i / 2,
                    launchPosition.z + v_z * t_i
                );
                float timestamp = launchTimestamp + t_i;

                buffer += $"\n#{timestamp}\n{shellId},T={VectorToTransform(pos)}";
            }
            buffer += $"\n#{hitTimestamp}\n-{shellId}";
        }


        private static string GetShipACMIId(Ship ship)
        {
            return $"1000{ship.Side:X2}{ship.CrewIndex:X2}";
        }

        private static string VectorToTransform(Vector3 vector, Vector3? heading = null)
        {
            const double earthCircumference = 6378137 * 2 * Math.PI; //40030173;
            const double mToDeg = 360 / earthCircumference;
            // Approximation for equator.
            double longitude = vector.x * mToDeg;
            double latitude = vector.z * mToDeg;
            double altitude = vector.y;
            string t = $"{longitude}|{latitude}|{altitude}";

            if (heading.HasValue)
            {
                double roll = 0;
                //double pitch = Math.Asin(heading.Value.y) * 360 / (2 * Math.PI);
                double pitch = 0;
                double yaw = -Math.Atan2(heading.Value.z, heading.Value.x) * 360 / (2 * Math.PI) + 90;
                t = $"{t}|{roll}|{pitch}|{yaw}";
            }
            return t;
        }
    }
}
