using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace TelemetryAudit.Core
{
    public enum ItemState { On, Off, NotFound }

    public class TelemetryItem
    {
        public string Category;
        public string Name;
        public string Detail;
        public ItemState State;
        public string DisableHint; // registry path / service name / schtasks path
    }

    internal static class TelemetryScanner
    {
        public static List<TelemetryItem> Scan()
        {
            var items = new List<TelemetryItem>();
            ScanRegistry(items);
            ScanServices(items);
            ScanScheduledTasks(items);
            return items;
        }

        private static void ScanRegistry(List<TelemetryItem> items)
        {
            // AllowTelemetry policy
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                {
                    object v = k == null ? null : k.GetValue("AllowTelemetry");
                    int level = v == null ? -1 : Convert.ToInt32(v);
                    string desc = level < 0 ? "not set (system default, usually Full on Pro)" : ("level " + level + " (0=Security, 1=Basic, 3=Full)");
                    items.Add(new TelemetryItem
                    {
                        Category = "Registry",
                        Name = "AllowTelemetry policy",
                        Detail = desc,
                        State = level == 0 ? ItemState.Off : ItemState.On,
                        DisableHint = @"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 0 /f"
                    });
                }
            }
            catch (Exception ex)
            {
                items.Add(new TelemetryItem { Category = "Registry", Name = "AllowTelemetry", Detail = ex.Message, State = ItemState.NotFound });
            }

            // Advertising ID
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo"))
                {
                    object v = k == null ? null : k.GetValue("Enabled");
                    bool enabled = v != null && Convert.ToInt32(v) == 1;
                    items.Add(new TelemetryItem
                    {
                        Category = "Registry",
                        Name = "Advertising ID",
                        Detail = enabled ? "enabled (apps can use your ad ID)" : "disabled",
                        State = enabled ? ItemState.On : ItemState.Off,
                        DisableHint = @"reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo"" /v Enabled /t REG_DWORD /d 0 /f"
                    });
                }
            }
            catch (Exception ex)
            {
                items.Add(new TelemetryItem { Category = "Registry", Name = "Advertising ID", Detail = ex.Message, State = ItemState.NotFound });
            }
        }

        private static void ScanServices(List<TelemetryItem> items)
        {
            string[] svcNames = { "DiagTrack", "dmwappushservice" };
            string[] friendly = { "Connected User Experiences and Telemetry (DiagTrack)", "Device Management WAP Push (dmwappushservice)" };
            for (int i = 0; i < svcNames.Length; i++)
            {
                try
                {
                    ServiceController sc = new ServiceController(svcNames[i]);
                    ServiceControllerStatus status = sc.Status;
                    items.Add(new TelemetryItem
                    {
                        Category = "Service",
                        Name = friendly[i],
                        Detail = "status: " + status + ", start type: " + GetStartType(svcNames[i]),
                        State = status == ServiceControllerStatus.Running ? ItemState.On : ItemState.Off,
                        DisableHint = @"sc config " + svcNames[i] + " start= disabled && net stop " + svcNames[i]
                    });
                }
                catch
                {
                    items.Add(new TelemetryItem { Category = "Service", Name = friendly[i], Detail = "not found", State = ItemState.NotFound });
                }
            }
        }

        private static string GetStartType(string svc)
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + svc))
                {
                    object v = k == null ? null : k.GetValue("Start");
                    if (v == null) return "unknown";
                    int n = Convert.ToInt32(v);
                    switch (n)
                    {
                        case 2: return "Automatic";
                        case 3: return "Manual";
                        case 4: return "Disabled";
                        default: return "code " + n;
                    }
                }
            }
            catch { return "unknown"; }
        }

        private static void ScanScheduledTasks(List<TelemetryItem> items)
        {
            string[][] tasks = {
                new[] { "Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser", "Compatibility telemetry (CRC)" },
                new[] { "Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator", "CEIP telemetry" },
                new[] { "Microsoft\\Windows\\Customer Experience Improvement Program\\UsbCeip", "USB CEIP" },
                new[] { "Microsoft\\Windows\\Application Experience\\ProgramDataUpdater", "Program Data Updater (telemetry)" }
            };
            foreach (string[] t in tasks)
            {
                string path = t[0];
                string name = t[1];
                bool exists = false;
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("schtasks", "/query /tn \"" + path + "\"");
                    psi.UseShellExecute = false;
                    psi.RedirectStandardOutput = true;
                    psi.CreateNoWindow = true;
                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit(5000);
                        exists = p.ExitCode == 0;
                    }
                }
                catch { }
                items.Add(new TelemetryItem
                {
                    Category = "Scheduled task",
                    Name = name,
                    Detail = exists ? "exists (scheduled to run)" : "not found",
                    State = exists ? ItemState.On : ItemState.Off,
                    DisableHint = @"schtasks /change /tn """ + path + @""" /disable"
                });
            }
        }
    }
}
