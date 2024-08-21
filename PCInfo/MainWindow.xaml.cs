using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management;
using System.Net.Sockets;
using System.Net;
using Microsoft.Win32;
using System.IO;
using System.Windows.Threading;

namespace PCInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            init();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            dispatcherTimer.Start();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                DragMove();
            }
        }
        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MaximizeRestoreWindow(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;

        }

        private void GroupPolicyUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            GPtext.Visibility = Visibility.Collapsed;
            LoadingIcon.Visibility = Visibility.Visible;
            GPupdate();
        }
        public void GPupdate()
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c gpupdate /force";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                //Console.WriteLine(output);
                process.WaitForExit();
                GPtext.Visibility = Visibility.Visible;
                LoadingIcon.Visibility = Visibility.Collapsed;
                //Console.WriteLine("Group Policy update completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
        private void init()
        {
            Hostname.Content = System.Net.Dns.GetHostName();
            IPArdess.Content = GetLocalIPAddress();
            Winvers.Content = Winversion();
            DiskSpace.Content = GetTotalFreeSpace("Disk0").ToString();
            ComputerModel.Content = Compmodel();
            LastBooted.Content = Boottime();
            LastGPUpdate.Content = LastGPupdate();

        }
        private void Reset(object sender, RoutedEventArgs e)
        {
            init();
        }
        public string LastGPupdate()
        {
            EventLog systemLog = new EventLog("System");
            StringBuilder sb = new StringBuilder();
            for (int i = systemLog.Entries.Count - 1; i >= 0; i--)
            {
                EventLogEntry entry = systemLog.Entries[i];

                if (entry.Source == "Microsoft-Windows-GroupPolicy" && entry.InstanceId == 1502)
                {
                    sb.Append(entry.TimeGenerated);
                    break;
                }
            }
            return sb.ToString();
        }
        public string Boottime()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            StringBuilder sb = new StringBuilder();
            // Iterate through the results
            foreach (ManagementObject obj in searcher.Get())
            {
                DateTime lastBootUpTime = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString());
                sb.Append(lastBootUpTime);
            }
            return sb.ToString();
        }
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        public static DateTime GetWindowsInstallationDateTime(string computerName)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.RegistryKey.OpenRemoteBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, computerName);
            key = key.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
            if (key != null)
            {
                DateTime installDate =
                    DateTime.FromFileTimeUtc(
                        Convert.ToInt64(
                            key.GetValue("InstallDate").ToString()));

                return installDate;
            }

            return DateTime.MinValue;
        }
        public string Compmodel()
        {

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            StringBuilder sb = new StringBuilder();
            foreach (ManagementObject obj in searcher.Get())
            {
                sb.Append(obj["Manufacturer"]);
                sb.Append(" " + obj["Model"]);
            }

            return sb.ToString();
        }
        public static string Winversion()
        {

            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (registryKey != null)
            {
                return registryKey.GetValue("UBR").ToString();
            }
            return "Coudent retrive the Windows version";
        }
        private string GetTotalFreeSpace(string driveName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                sb.Append(drive.Name);
                sb.Append(' ');
                sb.Append(drive.TotalFreeSpace / Math.Pow(1024, 3));
                sb.Append(" GB ");
                sb.Append(' ');
                sb.Append($"{drive.TotalFreeSpace / (drive.TotalSize / 100)} %");
            }
            return sb.ToString();
        }

        private void ResetComputer(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0");

        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today;
            var daysUntilWednesday = ((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7;
            var nextFriday = today.AddDays(daysUntilWednesday);
            TimeSpan ts = new TimeSpan(16, 0, 0);
            nextFriday = nextFriday.Date + ts;
            // Updating the Label which displays the current second
            RestartCountdown.Content = $"{nextFriday.Day} nap {nextFriday.Hour}:{nextFriday.Second}";
            

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }
    }
}