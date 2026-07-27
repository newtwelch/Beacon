using AsyncAwaitBestPractices;
using Backend.Core.Models.Songs;
using Backend.Core.Services;
using Frontend.WPF.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Velopack;
using Velopack.Sources;

namespace Frontend.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            SourceInitialized += MainWindow_SourceInitialized;                                              // Initialize the complicated window maximize problem thingy

            InitializeComponent();
            blazorWebView.Services = App.Services; // ✅ set services here
            InitializeComponent();

            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

            // APP TITLE WITH VERSION
            var updateManager = new UpdateManager("http://beaconapi.runasp.net/updates");
            AppTitle.Text = $"Beacon {(updateManager.IsInstalled ? updateManager.CurrentVersion : "(DEV MODE)")}";

            //!? ====================================================
            //!? INIT: Window Buttons
            //!? ====================================================
            HideButton.Click += (s, e) => WindowState = WindowState.Minimized;                              // EVENT: Hide App
            MinMaxButton.Click += (s, e) => WindowState = WindowState ==                                    // EVENT: Minimize if Max.
                            WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;             // Maximize if Min.
            CloseButton.Click += (s, e) => Close();                                                         // EVENT: Close App
            DisplayWindow.Instance.SetWindow("");
            DisplayWindow.Instance.Show();
            DisplayWindow.Instance.Close();
        }

        #region Window Maximize Fix
        //! ====================================================
        //! [+] SOURCE INITIALIZE: should fix the maximizing issue
        //! ====================================================
        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
        }

        //! ====================================================
        //! [+] INTPTR WINDOWPROC: I have no idea :(
        //! ====================================================
        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WindowMaximizeHelper.WmGetMinMaxInfo(hwnd, lParam, (int)MinWidth, (int)MinHeight);
                    handled = true;
                    break;
            }

            return (IntPtr)0;
        }
        #endregion

    }
}