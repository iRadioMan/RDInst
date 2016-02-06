using System;
using System.Collections.Generic;
using System.Text;
using System.Management;

namespace RDInst
{
    class Program
    {
        static private void Prt(string _txt, bool _line = false)
        {
            if (_line) Console.WriteLine(_txt);
            else Console.Write(_txt);
        }
        static private void Wait()
        {
            Console.ReadLine();
        }
        static private ManagementObjectCollection GetUnDevicesList()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * from Win32_PnPEntity where Availability = 12 or Availability = 11"); //11 - not installed, 12 - error
            return searcher.Get();
        }
        static private void PrintDevNames(ManagementObjectCollection _c)
        {
            foreach (ManagementObject obj in _c)
            {
                string t = obj.GetPropertyValue("DeviceName").ToString();
                if (t != null && t != "")
                {
                    t += obj.GetPropertyValue("DeviceID").ToString();
                    Prt(t, true);
                }
            }
        }

        static void Main(string[] args)
        {
            Prt("-= RadioMan's Driver Installer =-\n\nПолучение списка неустановленных устройств...\n", true);
            
            ManagementObjectCollection devs = GetUnDevicesList();
            if (devs.Count != 0) PrintDevNames(devs);
            else Prt("Все устройства успешно установлены!", true);
           
            Wait();
        }
    }
}
