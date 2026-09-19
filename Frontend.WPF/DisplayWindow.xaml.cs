using Backend.Core.Models.Bible;
using Backend.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfScreenHelper;
using WpfScreenHelper.Enum;

namespace Frontend.WPF
{

    /// <summary>
    /// Interaction logic for DisplayWindow.xaml
    /// </summary>
    public partial class DisplayWindow : Window
    {

        //! Singleton Insatance
        public static DisplayWindow Instance { get; private set; }
        static DisplayWindow() => Instance = new DisplayWindow();

        private ProjectionService projectionService;
        private string selectedScreen = "";

        public DisplayWindow()
        {
            InitializeComponent();
     
            projectionService = App.Services.GetRequiredService<ProjectionService>();
            projectionService.LyricChanged += LyricChanged;
            projectionService.VerseTextChanged += VerseTextChanged;
            projectionService.VersePortionChanged += VersePortionChanged;

            projectionService.MonitorChanged += (monitor) =>
            {
                SetWindow(monitor.Name);
                selectedScreen = monitor.Name;
            };

            var screenlist = Screen.AllScreens.ToList();
            foreach(var screen in screenlist)
            {
                projectionService.AddMonitor(new Backend.Core.Models.Beacon.Screen
                {
                    Id = screen.DeviceName.GetHashCode(),
                    Name = screen.DeviceName,
                    IsPrimary = screen.Primary
                });
            }
        }


        public void LyricChanged(string songTitle, string lyricLine, string lyricText)
        {
            Instance.SetWindow(selectedScreen);
            Instance.Show();
            
            Header1.Text = songTitle;
            Header2.Text = lyricLine;
            
            Content.Text = lyricText;
            Content.FontSize = 100;
            Content.TextWrapping = TextWrapping.NoWrap;
            Content.Width = Double.NaN;
            Content.HighlightCount = 0;
        }

        public void VerseTextChanged(string verseReference, string translation, string text)
        {
            Instance.SetWindow(selectedScreen);
            Instance.Show();

            Header1.Text = verseReference;
            Header2.Text = translation;

            Content.Text = text;
            Content.TextWrapping = TextWrapping.Wrap;
            Content.Width = this.Width;

            var greaters400 = text.Count() > 400;
            var greater200 = text.Count() > 200; 
            
            Content.FontSize = greaters400 ? 70 : greater200 ? 85 : 100;
        }

        public void VersePortionChanged(int id)
        {
            Content.HighlightCount = id;
        }

        //! ====================================================
        //! [+] WINDOW CLOSING: cancel the closing and set visibility to collapsed
        //! ====================================================
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            e.Cancel = true;
        }

        //! ====================================================
        //! [+] SET WINDOW POSITION: using WPFScreenHelper so I don't have to use WinForms
        //! ====================================================
        private static void SetWindowPosition(Window wnd, WindowPositions pos, Rect bounds)
        {
            var x = 0.0d;
            var y = 0.0d;

            switch (pos)
            {
                case WindowPositions.Center:
                    x = bounds.X + (bounds.Width - wnd.Width) / 2.0;
                    y = bounds.Y + (bounds.Height - wnd.Height) / 2.0;
                    break;
                case WindowPositions.Left:
                    x = bounds.X;
                    y = bounds.Y + (bounds.Height - wnd.Height) / 2.0;
                    break;
                case WindowPositions.Top:
                    x = bounds.X + (bounds.Width - wnd.Width) / 2.0;
                    y = bounds.Y;
                    break;
                case WindowPositions.Right:
                    x = bounds.X + (bounds.Width - wnd.Width);
                    y = bounds.Y + (bounds.Height - wnd.Height) / 2.0;
                    break;
                case WindowPositions.Bottom:
                    x = bounds.X + (bounds.Width - wnd.Width) / 2.0;
                    y = bounds.Y + (bounds.Height - wnd.Height);
                    break;
                case WindowPositions.TopLeft:
                    x = bounds.X - 4;
                    y = bounds.Y - 1;
                    break;
                case WindowPositions.TopRight:
                    x = bounds.X + (bounds.Width - wnd.Width);
                    y = bounds.Y;
                    break;
                case WindowPositions.BottomRight:
                    x = bounds.X + (bounds.Width - wnd.Width);
                    y = bounds.Y + (bounds.Height - wnd.Height);
                    break;
                case WindowPositions.BottomLeft:
                    x = bounds.X;
                    y = bounds.Y + (bounds.Height - wnd.Height);
                    break;
            }

            wnd.Left = x;
            wnd.Top = y;
        }

        //! ====================================================
        //! [+] VISIBLE CHANGED
        //! ====================================================
        //private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => SetWindow();
        public void SetWindow(string monitor)
        {
            var screenlist = Screen.AllScreens.ToList();
            var chosenScreen = screenlist.FirstOrDefault(s => s.DeviceName == monitor);

            if (chosenScreen != null)
            {
                SetWindowPosition(this, WindowPositions.TopLeft, chosenScreen.Bounds);
                Width = chosenScreen.Bounds.Width + 8;
                Height = chosenScreen.Bounds.Height + 5;
            }
            else
            {
                SetWindowPosition(this, WindowPositions.TopLeft, screenlist.FirstOrDefault()!.Bounds);
                Width = screenlist[0].Bounds.Width + 8;
                Height = screenlist[0].Bounds.Height + 5;
            }
        }

        //? =============================[LOADED & UNLOADED]==============================

        //private void Window_Loaded(object sender, RoutedEventArgs e) => MainWindow.CloseDisplayEvent += CloseDisplayMethod;

        //private void Window_Unloaded(object sender, RoutedEventArgs e) => MainWindow.CloseDisplayEvent -= CloseDisplayMethod;

        //!? ====================================================
        //!? DISABLES FOCUS ON THIS WINDOW
        //!? ====================================================
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            if (source is not null)
                source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(MA_NOACTIVATE);
            }
            else return IntPtr.Zero;
        }
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 0x0003;


    }
}
