using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Management;

namespace RDInst
{
    class Program
    {
        private struct Installator {
            public string DevName { get; set; }
            public string DevID { get; set; }
            public string DevDrv { get; set; }
        }

        static int MaxDevices = 50; //
        static Installator[] Devices = new Installator[MaxDevices];
        static int DevicesCount = 0;
        static int ExitCode = 0;

        static private void Prt(string _txt, bool _line = false) {
            if (_line) Console.WriteLine(_txt);
            else Console.Write(_txt);
        }

        static private void Wait() {
            Prt("To continue press <Enter>...", true);
            Console.ReadLine();
        }

        static private void PrtLog(string _txt, bool _line = false) {
            try {
                using (StreamWriter sr = new StreamWriter(@"RDInst.log", true)) {
                    if (_line) sr.WriteLine(_txt);
                    else sr.Write(_txt);
                }
            }
            catch {
                Prt("ERROR writing to the log file.\nIs the program has sufficient rights?", true);
                return;
            }
        }

        static private void Init() {
            PrtLog(DateTime.Now + " Program started", true);
            Prt("-= RadioMan's Driver Installer =-\n\n", true);
            PrtLog(DateTime.Now + " Checking base", true);
            //check drivers base
        }

        static private void PrintDevNames(Installator[] _dvs) {
            for (int i = 0; i < DevicesCount; ++i) {
                Prt(_dvs[i].DevName + " (" + _dvs[i].DevID + ")", true);
            }
        }

        static private void GetUnDevicesList() {
            PrtLog(DateTime.Now + " Get devices list", true);
            Prt("Getting a list of uninstalled devices...\n", true);
            try {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * from Win32_PnPEntity where Availability = 1 or Availability = 12 or Availability = 11"); //11 - not installed, 12 - error
                foreach (ManagementObject obj in searcher.Get())
                {
                    string t = obj.GetPropertyValue("DeviceName").ToString();
                    if (t != null && t != "")
                    {
                        Devices[DevicesCount].DevName = t;
                        Devices[DevicesCount].DevID = obj.GetPropertyValue("DeviceID").ToString();
                        Devices[DevicesCount].DevDrv = null;
                        DevicesCount++;
                    }
                }
            }
            catch (Exception e) {
                ExitCode = 10;
                PrtLog(DateTime.Now + " Get devices list error: " + e.ToString(), true);
                Prt("ERROR while trying to get a list of devices.\nSee the log file for details.", true);
                return;
            }
        }

        static private bool CheckDevices() {
            if (DevicesCount != 0) {
                PrintDevNames(Devices);
                //write a code here
                return true; //there are uninstalled devices
            }
            else {
                PrtLog(DateTime.Now + " All devices installed", true);
                Prt("All devices are already installed!", true);
                Wait();
                return false;
            }
        }

        static void InstallDrivers() {
            PrtLog(DateTime.Now + " Install drivers", true);
            Prt("Installing needed drivers...\nPlease wait!", true);
            //load drivers base
        }

        static int OnExit() {
            //write a code here
            PrtLog(DateTime.Now + " Program exit with exitcode " + ExitCode.ToString(), true);
            return ExitCode;
        }

        static int Main(string[] args) {
            //the main function is simplified to maximum
            Init();
            GetUnDevicesList();
            if (CheckDevices()) InstallDrivers();
            return OnExit();
        }
    }
}
