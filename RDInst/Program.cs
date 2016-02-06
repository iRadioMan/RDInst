using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;

namespace RDInst
{
    class Program
    {
        private struct Installator {
            public string DevName { get; set; }
            public string DevID { get; set; }
            public string DevDrv { get; set; }
        }

        static string OSVer = "none";
        static List<Installator> Devices = new List<Installator>();
        static int ExitCode = 0;

        static private void MakeError(string _log, string _txt, int _exitcode) {
            PrtLog(DateTime.Now + " " + _log, true);
            Prt(_txt, true);
            ExitCode = _exitcode;
            Wait();
            OnExit();
            Environment.Exit(_exitcode);
        }

        static private void Prt(string _txt, bool _line = false) {
            if (_line) Console.WriteLine(_txt);
            else Console.Write(_txt);
        }

        static private void Wait() {
            Prt("To continue press <Enter>...", false);
            Console.ReadLine();
        }

        static private void PrtLog(string _txt, bool _line = false) {
            try {
                using (StreamWriter sw = new StreamWriter(@"RDInst.log", true)) {
                    if (_line) sw.WriteLine(_txt);
                    else sw.Write(_txt);
                }
            }
            catch {
                Prt("ERROR writing to the log file.\nIs the program has sufficient rights?", true);
                return;
            }
        }

        static private void Init() {
            PrtLog(DateTime.Now + " Program started", true);
            Prt("-= RadioMan's Driver Installer =-", true);
            PrtLog(DateTime.Now + " Checking base", true);
            
            if (!Directory.Exists(@"Drivers\7x86") || !Directory.Exists(@"Drivers\7x64") || !Directory.Exists(@"Drivers\XP") || !File.Exists(@"base7x86.ini") || !File.Exists(@"base7x64.ini") || !File.Exists(@"baseXP.ini"))
                MakeError("Base error #0", "\nError: the drivers base is corrupted.\nPlease, reinstall the program!\n", 5);
            
            FileInfo fi = new FileInfo(@"base7x86.ini");
            FileInfo fi2 = new FileInfo(@"base7x64.ini");
            FileInfo fi3 = new FileInfo(@"baseXP.ini");
            if (fi.Length == 0 || fi2.Length == 0 || fi3.Length == 0)
                MakeError("Base error #1", "\nError: one of main base files is empty.\nPlease, reinstall the program!\n", 7);
            
            PrtLog(DateTime.Now + " Get OS version", true);
            switch (Environment.OSVersion.Version.ToString().Substring(0, 3)) { //get only 3 digits of version
                case "5.1":
                    OSVer = "XP";
                    Prt("Your OS: Windows XP", true);
                    break;
                case "6.1":
                    if (Marshal.SizeOf(typeof(IntPtr)) == 8) {
                        OSVer = "7x64";
                        Prt("Your OS: Windows 7 64-bit", true);
                    }
                    else {
                        OSVer = "7x86";
                        Prt("Your OS: Windows 7 32-bit", true);
                    }
                    break;
                default:
                    MakeError("Unsupported OS", "Warning: your OS is not supported. Sorry!\nRDInst will now exit...", 9);
                    break;
            }
            Prt("---------------------------------", true);
        }

        static private void PrintDevNames(int _drv)
        {
            Prt("==============================================================", true);
            foreach (Installator i in Devices) {
                switch (_drv) {
                    case 0:
                        Prt(i.DevName + " (" + i.DevID + ")", true);
                        break;
                    case 1:
                        if (i.DevDrv != "none") Prt(i.DevName + " (" + i.DevID + ")", true);
                        break;
                    case 2:
                        if (i.DevDrv == "none") Prt(i.DevName + " (" + i.DevID + ")", true);
                        break;
                }
            }
            Prt("==============================================================", true);
        }

        static private void GetUnDevicesList() {
            PrtLog(DateTime.Now + " Get devices list", true);
            Prt("\n\nGetting a list of uninstalled devices...\n", true);
            try {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Status = 'Error'"); //11 - not installed, 12 - error
                foreach (ManagementObject obj in searcher.Get())
                {
                    string t = obj.GetPropertyValue("Name").ToString();
                    if (t != null && t != ""&& t != "ttnfd")
                    {
                        Installator tempInst = new Installator();
                        tempInst.DevName = t;
                        tempInst.DevID = obj.GetPropertyValue("DeviceID").ToString().Substring(0, 21);
                        tempInst.DevDrv = "none";
                        Devices.Add(tempInst);
                    }
                }
            }
            catch (Exception e) {
                MakeError("Get devices list error: " + e.ToString(), "ERROR while trying to get a list of devices.\nSee the log file for details.", 10);
            }
        }

        static private bool CheckDevices() {
            if (Devices.Count != 0) {
                PrtLog(DateTime.Now + " Print devices", true);
                PrintDevNames(0);
                return true; //there are uninstalled devices
            }
            else {
                PrtLog(DateTime.Now + " All devices installed", true);
                Prt("All devices are already installed!\n", true);
                return false;
            }
        }

        static private List<string> GetIDs(string _s, char token) {
            List<string> IDs = new List<string>();
            if (!_s.StartsWith("PCI") && !_s.StartsWith("USB")) { //add more types!
                return IDs; //this is not dev id, ignoring
            }
            string ID = "";
            for (int i = 0; i != _s.Length; ++i)
            {
                if (_s[i] == token)
                {
                    IDs.Add(ID);
                    ID = "";
                }
                else ID += _s[i];
            }
            return IDs;
        }

        static void InstallDrivers() {
            Prt("\nRDInst will start searching for needed drivers in base.", true);
            Wait();
            PrtLog(DateTime.Now + " Search drivers", true);
            Prt("\nSearching for needed drivers...\nPlease wait!\n", true);

            //searching for drivers
            int DriversFound = 0;
            using (StreamReader sr = new StreamReader(@"base" + OSVer + ".ini")) {
                string str;
                while ((str = sr.ReadLine()) != null) {
                    List<string> ids = GetIDs(str, ',');
                    foreach (string s in ids) {
                        for (int i = 0; i < Devices.Count; ++i) {
                            if (s == Devices[i].DevID) { //driver found!
                                Installator tmp = new Installator();
                                tmp.DevName = Devices[i].DevName;
                                tmp.DevID = Devices[i].DevID;
                                tmp.DevDrv = ids[ids.Count - 1]; //last id is main driver file
                                Devices[i] = tmp;
                                DriversFound++;
                            }
                        }
                    }
                }

                //printing result
                if (DriversFound == 0) {
                    PrtLog(DateTime.Now + " Search done. No drivers found", true);
                    Prt("Sorry, there are no drivers found for your devices. :(", true);
                    return;
                }
                PrtLog(DateTime.Now + " Search done. Printing", true);
                Prt("Devices, for those drivers WERE found:", true);
                PrintDevNames(1);
                if ((Devices.Count - DriversFound) != 0) {
                    Prt("\n\nDevices, for those drivers WERE NOT found:", true);
                    PrintDevNames(2);
                }
                Prt("\n\nRDInst will now start installing drivers. ;)", true);
                Wait();

                //installing drivers
                foreach (Installator i in Devices) {
                    if (i.DevDrv != "none") {
                        //install driver
                    }
                }
            }
        }

        static int OnExit() {
            //write a code here
            Prt("Thank you for using program!\n", true);
            PrtLog(DateTime.Now + " Program exit with exitcode " + ExitCode.ToString(), true);
            Wait();
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
