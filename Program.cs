using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ASRT_BoostRaceEnabler
{
    public class Program
    {
        [DllImport("kernel32")]
        private static extern int OpenProcess(int dwDesiredAccess, int bInheritHandle, int dwProcessId);

        [DllImport("kernel32")]
        private static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpbuffer, int nSize, int lpNumberOfBytesWritten);

        [DllImport("kernel32")]
        private static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int nSize, int lpNumberOfBytesRead);

        [DllImport("kernel32")]
        private static extern int VirtualAllocEx(int hProcess, IntPtr lpAddress, int nSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32")]
        private static extern int VirtualFreeEx(int hProcess, int lpAddress, int nSize, uint dwFreeType);

        private static int processHandle;

        public static void Main()
        {
            Application.EnableVisualStyles();

            Process[] processList = Process.GetProcessesByName("ASN_App_PcDx9_Final");

            if (processList.Length == 0)
            {
                MessageBox.Show("Please start the game first!", "ASRT Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            foreach (Process process in processList)
            {
                processHandle = OpenProcess(0x38, 0, process.Id);

                if (processHandle == 0)
                {
                    MessageBox.Show("Could not access the game, please run as administrator!", "ASRT Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(2);
                }

                if (process.MainModule.ModuleMemorySize != 0xC7C000 && process.MainModule.ModuleMemorySize != 0xD06000)
                {
                    MessageBox.Show("Cannot apply patch. Please ensure you are\n" +
                                    "running the correct version of the game!", "ASRT Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(2);
                }

                byte[] status = new byte[4];
                int injectionpoint = 0x6A5DE8;
                ReadProcessMemory(processHandle, injectionpoint, status, 1, 0);

                if (status[0] != 0xE8)
                {
                    byte[] injectedcode = new byte[] {0xEB, 0x08, 0x89, 0x06, 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3, 0x83, 0x3D, 0x90, 0x68, 0xC5, 0x00, 0x03, 0x75, 0xEF,
                    0x53, 0x8B, 0x1D, 0x88, 0x1A, 0xEC, 0x00, 0x83, 0xBB, 0x8C, 0x03, 0x00, 0x00, 0x00, 0x5B, 0x75, 0xDE, 0x53, 0x8B, 0x1D, 0x70, 0x02, 0xBD, 0x00, 0x81,
                    0xC3, 0xD0, 0x02, 0x00, 0x00, 0x39, 0xD8, 0x5B, 0x75, 0xCC, 0x05, 0xA8, 0x0C, 0x00, 0x00, 0x75, 0xC5};
                    int injectionaddress = VirtualAllocEx(processHandle, IntPtr.Zero, injectedcode.Length, 0x00001000, 0x40);
                    WriteProcessMemory(processHandle, injectionaddress, injectedcode, injectedcode.Length, 0);
                    byte[] injectedcodecall = new byte[] { 0xE8, 0x00, 0x00, 0x00, 0x00, 0x90, 0x90 };
                    BitConverter.GetBytes(injectionaddress - (injectionpoint + 5)).CopyTo(injectedcodecall, 1);
                    WriteProcessMemory(processHandle, injectionpoint, injectedcodecall, injectedcodecall.Length, 0);
                    MessageBox.Show("Single Race mode will now load Boost Races!\n\n" +
                    "To restore the default behaviour, either restart the game\nor launch this tool again!\n\n",
                    "ASRT Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    if (MessageBox.Show("It appears your game is already patched\n" +
                                       "Do you want to remove the patch and restore stock settings?", "ASRT Boost Race Enabler", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        byte[] offset = new byte[4];
                        ReadProcessMemory(processHandle, injectionpoint + 1, offset, offset.Length, 0);
                        WriteProcessMemory(processHandle, injectionpoint, new byte[] { 0x89, 0x06, 0xB8, 0x01, 0x00, 0x00, 0x00 }, 7, 0);
                        VirtualFreeEx(processHandle, injectionpoint + 5 + BitConverter.ToInt32(offset, 0), 10, 0x4000);
                        MessageBox.Show(
                        "Boost races disabled!\n" +
                        "Enjoy your stock experience!", "ASRT Boost Race Enabler", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    Environment.Exit(0);
                }
            }
        }
    }
}