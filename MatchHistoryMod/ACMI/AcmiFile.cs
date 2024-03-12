using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

namespace MatchHistoryMod.ACMI
{
    class AcmiFile
    {
        static readonly Regex rgx = new Regex("[^a-zA-Z0-9]");

        private string buffer = "";

        private readonly string FilePath;

        public AcmiFile(int mapId, string mapName, DateTime date)
        {
            Directory.CreateDirectory("Replays");
            string dateStr = $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}-{date.Hour:D2}{date.Minute:D2}";
            string mapStr = mapName.Replace(' ', '_');

            int idx = 0;
            do
            {
                FilePath = $"Replays/{dateStr}_{mapStr}" + (idx++ == 0 ? "" : $"{idx}") + ".acmi";
            } while (File.Exists(FilePath));
        }

        public void Flush()
        {
            using (var fs = File.Open(FilePath, FileMode.Append, FileAccess.Write))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(buffer);
                fs.Write(info, 0, info.Length);
                buffer = "";
            }
        }

        private void Write(string str, bool flush = true)
        {
            buffer += str + "\n";
            if (flush) Flush();
        }

        public void AddHeader(int mapId, string mapName, DateTime date)
        {
            float mapLongOffset = ACMIConstants.GetMapOffset(mapName) + 0.5f;
            string dateStr = $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}";

            string header = "FileType=text/acmi/tacview\nFileVersion=2.2";
            string config = $"0,ReferenceTime={dateStr}T00:00:00Z,ReferenceLongitude={mapLongOffset},ReferenceLatitude=0.5";
            string mapItem = $"#0\n1,T=0|0|0,Name=goio-enviro-{mapId},Color=Orange";
            Write($"{header}\n{config}\n{mapItem}");
        }

        public void RegisterShip()
        {
            // TODO: move unchanging ship date here from AddShipPosition.
        }

        public void AddShipPosition(Ship ship, float timestamp)
        {
            string id = GetShipACMIId(ship);
            string transform = VectorToTransform(ship.position, ship.Forward);
            string evt = $"{id},T={transform},Name=goio-ship-{ship.ShipModelId},CallSign={ship.name},Color={ACMIConstants.GetColor(ship.Side)}";
            Write($"#{timestamp}\n{evt}");
        }

        public void AddShipDeath(Ship ship, float timestamp)
        {
            string shipId = GetShipACMIId(ship);
            Write($"#{timestamp}\n-{shipId}");
        }

        public void AddShell(BaseShell shell, float timestamp)
        {
            string shipId = "01";
            int shooterUserId = -1;
            string shooterName = "Unknown";
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
                    shooterUserId = player.UserId;
                    if (player.UserId == 0) shooterName = "AI";
                    else shooterName = player.name;
                }
                shipId = GetShipACMIId(ship);
                turretType = turret.ItemId;
            }
            catch (Exception) { }
            string transform = VectorToTransform(shell.position);
            string id = GetShellAcmiId(shell);
            string evt = $"{id},T={transform},Name=goio-projectile-{turretType},Parent={shipId},ShooterName={shooterName},ShooterId={shooterUserId},Color={ACMIConstants.GetColor(side)}";
            Write($"#{timestamp}\n{evt}");
        }

        public void AddShellDetonation(BaseShell shell, float hitTimestamp, ShellInfo launch)
        {
            string shellId = GetShellAcmiId(shell);
            
            // Interpolate shell arc
            float gravity = shell.Network.MyView.GetAppFixed(4);
            float t = hitTimestamp - launch.LaunchTimestamp;
            float v_x = (shell.position.x - launch.LaunchPosition.x) / t;
            float v_z = (shell.position.z - launch.LaunchPosition.z) / t;
            float v_y = (shell.position.y - launch.LaunchPosition.y) / t + gravity * t / 2;

            const int interpolationSteps = 8;
            for (int i = 1; i <= interpolationSteps; i++)
            {
                float t_i = t * i / interpolationSteps;
                Vector3 pos = new Vector3(
                    launch.LaunchPosition.x + v_x * t_i,
                    launch.LaunchPosition.y + v_y * t_i - gravity * t_i * t_i / 2,
                    launch.LaunchPosition.z + v_z * t_i
                );
                float timestamp = launch.LaunchTimestamp + t_i;

                Write($"#{timestamp}\n{shellId},T={VectorToTransform(pos)}", false);
            }
            Write($"#{hitTimestamp}\n-{shellId}");
        }

        public void AddMineDetonation()
        {
            // TODO: different behavior for mines and shells.
            // * Projectile until arming time.
            // * Stationary until detonation.
        }

        public void AddRepairableUpdate(Repairable repairable, float timestamp, RepairableState state)
        {
            Ship ship = repairable.Ship;
            string id = GetShipACMIId(ship);
            string componentName = rgx.Replace(repairable.SlotName, "");
            string evt = $"{id}," +
                $"{componentName}Health={state.Health}," +
                $"{componentName}MaxHealth={state.MaxHealth}," +
                $"{componentName}Broken={state.Broken}," +
                $"{componentName}RebuildProgress={state.RebuildProgress}," +
                $"{componentName}OnCooldown={state.OnCooldown}";
            Write($"#{timestamp}\n{evt}");
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

        public static string GetShipACMIId(Ship ship)
        {
            return $"01{ship.Side:X2}{ship.CrewIndex:X2}";
        }

        public static string GetShellAcmiId(BaseShell shell)
        {
            uint id = (uint) shell.GetInstanceID();
            return $"02{id:X8}";
        }
    }
}
