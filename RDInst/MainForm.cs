using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Management;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace RDInst
{
    public partial class MainForm : Form
    {
        [DllImport("advpack.dll", EntryPoint = "LaunchINFSection", CallingConvention = CallingConvention.StdCall)] //inf installer import library
        public static extern void LaunchINFSection(
            [In] IntPtr hwnd,
            [In] IntPtr hInstance,
            [In, MarshalAs(UnmanagedType.LPStr)] string pszParams,
            int nShow);

        private struct Installator
        {
            public string DevName { get; set; }
            public string DevID { get; set; }
            public string DevDrv { get; set; }
        }

        static List<Installator> Devices = new List<Installator>();
        static private bool DriversChecked = false;

        public MainForm()
        {
            InitializeComponent();
            switch (Program.OSVer) {
                case "XP":
                    OSVerLabel.Text += "Windows XP.";
                    break;
                case "7x86":
                    OSVerLabel.Text += "Windows 7 32-бита.";
                    break;
                case "7x64":
                    OSVerLabel.Text += "Windows 7 64-бита.";
                    break;
            }

            GetUnDevicesList();
            CheckDevices();
        }

        static private void GetUnDevicesList()
        {
            Program.PrtLog(DateTime.Now + " Get devices list", true);
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Status = 'Error'"); //11 - not installed, 12 - error
                foreach (ManagementObject obj in searcher.Get())
                {
                    string t = obj.GetPropertyValue("Name").ToString();
                    if (t != null && t != "" && t != "ttnfd")
                    {
                        Installator tempInst = new Installator();
                        tempInst.DevName = t;
                        tempInst.DevID = obj.GetPropertyValue("DeviceID").ToString().Substring(0, 21);
                        tempInst.DevDrv = "none";
                        Devices.Add(tempInst);
                    }
                }
            }
            catch (Exception e)
            {
                Program.MakeError("Get devices list error: " + e.ToString(), "Ошибка получения списка устройств.\nСм. лог для подробностей.", 10);
            }
        }

        private void RefreshDevicesList(int _list, int _drv) {
            if (_list == 0) listBox1.Items.Clear();
            else listBox2.Items.Clear();

            foreach (Installator i in Devices)
            {
                switch (_drv)
                {
                    case 0:
                        if (_list == 0) listBox1.Items.Add(i.DevName + " (" + i.DevID + ")");
                        else listBox2.Items.Add(i.DevName + " (" + i.DevID + ")");
                        break;
                    case 1:
                        if (i.DevDrv != "none") {
                            if (_list == 0) listBox1.Items.Add(i.DevName + " (" + i.DevID + ")");
                            else listBox2.Items.Add(i.DevName + " (" + i.DevID + ")");  
                        }
                        break;
                    case 2:
                        if (i.DevDrv == "none") {
                            if (_list == 0) listBox1.Items.Add(i.DevName + " (" + i.DevID + ")");
                            else listBox2.Items.Add(i.DevName + " (" + i.DevID + ")");
                        }
                        break;
                }
            }
        }

        private bool CheckDevices()
        {
            if (Devices.Count != 0)
            {
                Program.PrtLog(DateTime.Now + " Print devices", true);
                ConsoleBox.Text += "Найдены неустановленные устройства.\n";
                RefreshDevicesList(0, 0);
                button1.Enabled = true;
                return true; //there are uninstalled devices
            }
            else
            {
                Program.PrtLog(DateTime.Now + " All devices installed", true);
                ConsoleBox.Text += "Неустановленных устройств не найдено.\n";
                MessageBox.Show("Поздравляем! Все устройства уже установлены.", "Успех.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
        }

        static private List<string> GetIDs(string _s, char token)
        {
            List<string> IDs = new List<string>();
            if (!_s.StartsWith("PCI") && !_s.StartsWith("USB")) { //add more types!
                return IDs; //this is not dev id, ignoring
            }
            string ID = "";
            for (int i = 0; i < _s.Length; ++i) {
                if (i == _s.Length - 1) {
                    ID += _s[i];
                    IDs.Add(ID);
                    break;
                }
                if (_s[i] == token) {
                    IDs.Add(ID);
                    ID = "";
                }
                else ID += _s[i];
            }
            return IDs;
        }

        void CheckDrivers()
        {
            progressBar1.Enabled = true;
            button2.Enabled = false;
            ConsoleBox.Text += "Программа начинает поиск по базе драйверов...\n";
            Program.PrtLog(DateTime.Now + " Search drivers", true);
            ConsoleBox.Text += "\nПоиск необходимых драйверов...\nПожалуйста, подождите!\n";

            //searching for drivers
            int DriversFound = 0;
            using (StreamReader sr = new StreamReader(@"base" + Program.OSVer + ".ini"))
            {
                string str;
                while ((str = sr.ReadLine()) != null)
                {
                    List<string> ids = GetIDs(str, ',');
                    foreach (string s in ids)
                    {
                        for (int i = 0; i < Devices.Count; ++i)
                        {
                            if (s == Devices[i].DevID)
                            { //driver found!
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
                if (DriversFound == 0)
                {
                    progressBar1.Enabled = false;
                    button2.Enabled = true;
                    Program.PrtLog(DateTime.Now + " Search done. No drivers found", true);
                    MessageBox.Show("К сожалению, программа не смогла найти ни одного драйвера для ваших устройств. :(", "Извините.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                Program.PrtLog(DateTime.Now + " Search done. Printing", true);
                label1.Text = "Для этих устройств найдены драйверы:";
                RefreshDevicesList(0, 1);
                if ((Devices.Count - DriversFound) != 0)
                {
                    label2.Text = "Для этих устройств найдены драйверы:";
                    label2.Visible = true;
                    RefreshDevicesList(1,2);
                }
                progressBar1.Enabled = false;
                button2.Enabled = true;
                ConsoleBox.Text += "\nПрограмма готова к установке драйверов. ;)\n";
                button1.Text = "Установить драйверы";
                button1.Enabled = true;
                DriversChecked = true;
            }
        }

        void InstallDrivers() {
            //installing drivers
            progressBar1.Enabled = true;
            button2.Enabled = false;
            foreach (Installator i in Devices)
            {
                if (i.DevDrv != "none")
                {
                    try {
                        string DrvPath = Application.StartupPath + @"\Drivers\" + Program.OSVer + @"\" + i.DevDrv;
                        LaunchINFSection(IntPtr.Zero, IntPtr.Zero, DrvPath + ",,,4,N", 0);
                        //InstallHinfSection(IntPtr.Zero, IntPtr.Zero, DrvPath, 0); //FIX THIS
                    }
                    catch (Exception e) {
                        Program.PrtLog(DateTime.Now + " Error install " + i.DevID + ": " + e.ToString(), true);
                        ConsoleBox.Text += "[!] Ошибка установки драйвера для " + i.DevName + ". См. лог для подробностей.\n";
                    }
                }
            }
            Program.PrtLog(DateTime.Now + " Install end", true);
            ConsoleBox.Text += "Установка драйверов завершена.\n";
            progressBar1.Enabled = false;
            button1.Enabled = true;
            button2.Enabled = true;
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            if (DriversChecked)
                InstallDrivers();
            else 
                CheckDrivers();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://vk.com/iradioman");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("mailto:ilyadud@mail.ru");
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("skype:ilya.tech.rad?chat");
        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.java.com/ru/download/");
        }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://get.adobe.com/ru/flashplayer");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            ConsoleBox.Text += "\nУстановка дополнительных утилит. Пожалуйста, подождите...\n";

            Process p = new Process();
            if (checkBox1.Checked) { 
                p.StartInfo.FileName = @"Utils\DirectX.exe";
                try { 
                    p.Start();
                    p.WaitForExit();
                }
                catch (Exception ex) {
                    Program.PrtLog(DateTime.Now + " Error install DirectX: " + ex.ToString(), true);
                    ConsoleBox.Text += "[!] Ошибка установки DirectX. См. лог для подробностей.\n";
                }
                checkBox1.Checked = false;
                checkBox1.Enabled = false;
            }
            if (checkBox2.Checked)
            {
                p.StartInfo.FileName = @"Utils\dotNetfz.exe";
                try {
                    p.Start();
                    p.WaitForExit();
                }
                catch (Exception ex) {
                    Program.PrtLog(DateTime.Now + " Error install .NET: " + ex.ToString(), true);
                    ConsoleBox.Text += "[!] Ошибка установки Microsoft .NET Framework. См. лог для подробностей.\n";
                }
                checkBox2.Checked = false;
                checkBox2.Enabled = false;
            }
            if (checkBox3.Checked)
            {
                p.StartInfo.FileName = @"Utils\RuntimePack.exe";
                try {
                    p.Start();
                    p.WaitForExit();
                }
                catch (Exception ex) {
                    Program.PrtLog(DateTime.Now + " Error install Runtime Pack: " + ex.ToString(), true);
                    ConsoleBox.Text += "[!] Ошибка установки Runtime Pack. См. лог для подробностей.\n";
                }
                checkBox3.Checked = false;
                checkBox3.Enabled = false;
            }
            if (checkBox4.Checked)
            {
                p.StartInfo.FileName = @"Utils\Visual_C.exe";
                try {
                    p.Start();
                    p.WaitForExit();
                }
                catch (Exception ex) {
                    Program.PrtLog(DateTime.Now + " Error install VC++ Redist: " + ex.ToString(), true);
                    ConsoleBox.Text += "[!] Ошибка установки Microsoft Visual C++ Redist. См. лог для подробностей.\n";
                }
                checkBox4.Checked = false;
                checkBox4.Enabled = false;
            }

            ConsoleBox.Text += "Установка завершена.\n";
            button1.Enabled = true;
            button2.Enabled = true;
        }
    }
}
