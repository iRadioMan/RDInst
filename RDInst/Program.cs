using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;

namespace RDInst
{
    class Program
    {
        public static string OSVer = "none";
        static int ExitCode = 0;

        static public void MakeError(string _log, string _txt, int _exitcode) {
            PrtLog(DateTime.Now + " " + _log, true);
            ExitCode = _exitcode;
            MessageBox.Show(_txt, "Ошибка в программе.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            OnExit();
            Environment.Exit(_exitcode);
        }

        static public void PrtLog(string _txt, bool _line = false) {
            try {
                using (StreamWriter sw = new StreamWriter(@"RDInst.log", true)) {
                    if (_line) sw.WriteLine(_txt);
                    else sw.Write(_txt);
                }
            }
            catch {}
        }

        static private void Init() {
            PrtLog(DateTime.Now + " Program started", true);
            PrtLog(DateTime.Now + " Checking base", true);
            
            if (!Directory.Exists(@"Drivers") || !File.Exists(@"base7x86.ini") || !File.Exists(@"base7x64.ini") || !File.Exists(@"baseXP.ini"))
                MakeError("Base error #0", "\nОшибка: база драйверов повреждена.\nПожалуйста, переустановите программу.", 5);
            
            FileInfo fi = new FileInfo(@"base7x86.ini");
            FileInfo fi2 = new FileInfo(@"base7x64.ini");
            FileInfo fi3 = new FileInfo(@"baseXP.ini");
            if (fi.Length == 0 || fi2.Length == 0 || fi3.Length == 0)
                MakeError("Base error #1", "\nОшибка: один или несколько основных файлов базы пусты.\nПожалуйста, переустановите программу.", 7);
            
            PrtLog(DateTime.Now + " Get OS version", true);
            switch (Environment.OSVersion.Version.ToString().Substring(0, 3)) { //получаем только 3 символа из строки версии
                case "5.1":
                    OSVer = "XP";
                    break;
                case "6.1":
                    if (Marshal.SizeOf(typeof(IntPtr)) == 8) {
                        OSVer = "7x64";
                    }
                    else {
                        OSVer = "7x86";
                    }
                    break;
                default:
                    MakeError("Unsupported OS", "Внимание, ваша версия ОС не поддерживается. Извините!\nПрограмма завершит свою работу...", 9);
                    break;
            }
        }

        static int OnExit()
        {
            PrtLog(DateTime.Now + " Program exit with exitcode " + ExitCode.ToString(), true);
            MessageBox.Show("Спасибо за использование программы!", "Благодарность.", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return ExitCode;
        }

        [STAThread]
        static int Main(string[] args) {
            Init();
            Application.Run(new MainForm());
            return OnExit();
        }
    }
}
