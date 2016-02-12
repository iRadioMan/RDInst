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
        [DllImport("DIFXApi.dll", CharSet = CharSet.Unicode)]
        public static extern Int32 DriverPackageInstall(string _path, Int32 _flags);
        //public static extern Int32 DriverPackagePreinstall(string _path, Int32 _flags);

        [DllImport("CfgMgr32.dll", SetLastError = true)]
        public static extern int CM_Reenumerate_DevNode(int dnDevInst, int ulFlags);

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
                    winLogo.Image = RDInst.Properties.Resources.wXPsmall;
                    break;
                case "7x86":
                    OSVerLabel.Text += "Windows 7 32-бита.";
                    winLogo.Image = RDInst.Properties.Resources.w7small;
                    break;
                case "7x64":
                    OSVerLabel.Text += "Windows 7 64-бита.";
                    winLogo.Image = RDInst.Properties.Resources.w7small;
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
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Status = 'Error' OR Service = 'vga'"); //11 - not installed, 12 - error
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj == null) return;
                    Program.PrtLog(DateTime.Now + " Obj != null", true);
                    string name = obj.GetPropertyValue("Name").ToString();
                    Program.PrtLog(DateTime.Now + " Name get", true);

                    string service;
                    try { service = obj.GetPropertyValue("Service").ToString(); }
                    catch { service = "none"; }
                    
                    Program.PrtLog(DateTime.Now + " Service get", true);
                    if (name != null && name != "" && name != "ttnfd")
                    {
                        //не добавляем видеокарту в список устройств, если на ней нестандартный драйвер
                        if (service == "vga" && name != "Стандартный VGA графический адаптер" && name != "Standart VGA Graphics Adapter")
                            return;

                        Program.PrtLog(DateTime.Now + " Create installator", true);
                        Installator tempInst = new Installator();
                        tempInst.DevName = name;
                        tempInst.DevID = obj.GetPropertyValue("DeviceID").ToString().Substring(0, 21);
                        tempInst.DevDrv = "none";
                        Program.PrtLog(DateTime.Now + " Add installator to devices", true);
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
                return true;
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
            if (!_s.StartsWith("PCI") && !_s.StartsWith("USB")) { //добавить больше типов устройств
                return IDs;
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
            button2.Enabled = false;
            ConsoleBox.Text += "Программа начинает поиск по базе драйверов...\n";
            Program.PrtLog(DateTime.Now + " Search drivers", true);
            ConsoleBox.Text += "\nПоиск необходимых драйверов...\nПожалуйста, подождите!\n";

            //поиск драйверов по базе
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
                            {
                                Installator tmp = new Installator();
                                tmp.DevName = Devices[i].DevName;
                                tmp.DevID = Devices[i].DevID;
                                tmp.DevDrv = ids[ids.Count - 1]; //последний ID - установочный файл драйвера
                                Devices[i] = tmp;
                                DriversFound++;
                            }
                        }
                    }
                }
            }

            if (DriversFound == 0)
            {
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
                label2.Text = "Для этих устройств НЕ найдены драйверы:";
                label2.Visible = true;
                RefreshDevicesList(1,2);
                listBox2.Visible = true;
            }
            button2.Enabled = true;
            ConsoleBox.Text += "\nПрограмма готова к установке драйверов. ;)\n";
            button1.Text = "Установить драйверы";
            button1.Enabled = true;
            DriversChecked = true;
        }

        void InstallDrivers() {
            int InstalledDrivers = 0;
            ConsoleBox.Text += "Установка драйверов. Пожалуйста, подождите...\n";
            button2.Enabled = false;
            foreach (Installator i in Devices)
            {
                if (i.DevDrv != "none")
                {
                    Program.PrtLog(DateTime.Now + " Install " + i.DevID, true);
                    ConsoleBox.Text += "Устанавливается драйвер для " + i.DevName + "...\n";
                    string arch = "";
                    for (int c = 0; i.DevDrv[c] != '\\'; ++c) { //получаем имя папки (и архива)
                        arch += i.DevDrv[c];
                    }
                    if (Directory.Exists(arch)) Directory.Delete(arch, true); //удалим распакованную папку, если она есть
                    Process z = new Process();
                    //распакуем архив
                    z.StartInfo.FileName = @"Utils\7z.exe";
                    z.StartInfo.Arguments = @"x -y Drivers\" + arch + ".7z";
                    z.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    z.Start();
                    z.WaitForExit();
                    //

                    int result = -1;
                    if (Directory.Exists(arch))
                    {
                        string DrvPath = Application.StartupPath + @"\" + i.DevDrv; //получаем полный путь до inf-файла

                        /* устанавливаем драйвер
                         * TODO: процедура долгая, нужно запускать ее в отдельном потоке
                         */
                        result = DriverPackageInstall(DrvPath, 0);

                        Directory.Delete(arch, true); //удаляем распакованную папку
                    }
                    else {
                        result = 10;
                        Program.PrtLog(DateTime.Now + " 7z unpack: directory not found", true);
                    }

                    if (result != 0) {
                        Program.PrtLog(DateTime.Now + " Error install " + i.DevID + ": " + result, true);
                        ConsoleBox.Text += "[!] Ошибка установки драйвера для " + i.DevName + ". См. лог для подробностей.\n";
                    }
                    else {
                        InstalledDrivers++;
                        Program.PrtLog(DateTime.Now + " Successful install " + i.DevID, true);
                        ConsoleBox.Text += "Драйвер для " + i.DevName + "установлен успешно.\n";
                    }
                }
            }
            Program.PrtLog(DateTime.Now + " Install end", true);
            ConsoleBox.Text += "Установка драйверов завершена.";
            button2.Enabled = true;
            if (InstalledDrivers != 0) {
                CM_Reenumerate_DevNode(0, 0);
                if (DialogResult.Yes == MessageBox.Show("Для вступления изменений в силу может потребоваться перезагрузка. Перезагрузить компьютер сейчас?", "Внимание!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) {
                    Process.Start("shutdown.exe", "-r -t 0");
                    Environment.Exit(0);
                }
            }
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
            if (!checkBox1.Checked && !checkBox2.Checked && !checkBox3.Checked && !checkBox4.Checked) return;
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

        private void ConsoleBox_TextChanged(object sender, EventArgs e)
        {
            ConsoleBox.SelectionStart = ConsoleBox.Text.Length;
            ConsoleBox.ScrollToCaret();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            richTextBox1.Enabled = false;
            if (comboBox1.SelectedIndex != -1) {
                try { richTextBox1.LoadFile(@"base" + comboBox1.Items[comboBox1.SelectedIndex] + ".ini", RichTextBoxStreamType.PlainText); }
                catch (Exception ex) {
                    Program.PrtLog(DateTime.Now + " Error read base file: " + ex.ToString(), true);
                    ConsoleBox.Text += "Ошибка открытия файла " + comboBox1.Items[comboBox1.SelectedIndex] + ".\nСм. лог для подробностей.\n";
                    return; 
                }
                ConsoleBox.Text += "Открыт файл базы " + comboBox1.Items[comboBox1.SelectedIndex] + ".\n";
                richTextBox1.Enabled = true;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (richTextBox1.Text == "" || richTextBox1.Enabled == false) return;
            try { richTextBox1.SaveFile(@"base" + comboBox1.Items[comboBox1.SelectedIndex] + ".ini", RichTextBoxStreamType.PlainText); }
            catch (Exception ex) {
                Program.PrtLog(DateTime.Now + " Error write base file: " + ex.ToString(), true);
                ConsoleBox.Text += "Ошибка сохранения файла " + comboBox1.Items[comboBox1.SelectedIndex] + ".\nСм. лог для подробностей.\n";
                return;
            }
            ConsoleBox.Text += "Сохранен файл базы " + comboBox1.Items[comboBox1.SelectedIndex] + ".\n";
            richTextBox1.Enabled = false;
        }
    }
}
