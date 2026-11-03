using Backend.Core.Models.Bible;
using Backend.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfScreenHelper;
using WpfScreenHelper.Enum;

namespace Frontend.WPF
{
    /// <summary>Everything the projector needs to show one song slide.</summary>
    public record LyricSlide(string Title, string Section, string Text, int VerseCount = 0, string? Key = null);

    /// <summary>
    /// Interaction logic for DisplayWindow.xaml
    /// </summary>
    public partial class DisplayWindow : Window
    {//! Singleton Instance
        public static DisplayWindow Instance { get; private set; }
        static DisplayWindow() => Instance = new DisplayWindow();

        private ProjectionService projectionService;
        private string selectedScreen = "";

        private bool showKey;
        private string? currentKey;

        private string? KeyLabel => showKey && !string.IsNullOrWhiteSpace(currentKey) ? $"Key: {currentKey!.Trim()}" : null;

        /// <summary>Turn this on/off from your settings to show "Key: G" next to the section badge.</summary>
        public bool ShowKey
        {
            get => showKey;
            set
            {
                showKey = value;
                string? label = KeyLabel;
                shownValues[KeyBadge] = label;               // keep the fade tracking in sync
                SetBadge(KeyBadge, KeyText, label);          // settings toggle applies instantly
            }
        }

        public DisplayWindow()
        {
            InitializeComponent();

            SizeChanged += (_, _) => UpdateHeaderScale();
            Loaded += (_, _) => UpdateHeaderScale();
            ProgressTrack.SizeChanged += (_, _) => UpdateProgress(currentLyricId, totalLyrics);

            UpdateHeaderScale();

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
            foreach (var screen in screenlist)
            {
                projectionService.AddMonitor(new Backend.Core.Models.Beacon.Screen
                {
                    Id = screen.DeviceName.GetHashCode(),
                    Name = screen.DeviceName,
                    IsPrimary = screen.Primary
                });
            }
        }
        #region Songs
        private int currentLyricId;
        private int totalLyrics;

        public void LyricChanged(string songTitle, string lyricLine, string lyricText, int stanzaCount, string? key, int lyricId, int totalLyrics)
        {
            BeginSlide();

            FadeSwap(RefChapter, null, () => SetPart(RefChapter, null));
            FadeSwap(RefColon, null, () => SetPart(RefColon, null));
            FadeSwap(RefVerse, null, () => SetPart(RefVerse, null));

            string section = FormatSection(lyricLine, stanzaCount);
            currentKey = key;
            string? keyLabel = KeyLabel;

            FadeSwap(Header1, songTitle, () => Header1.Text = songTitle);
            FadeSwap(Header2Badge, section, () => SetBadge(Header2Badge, Header2, section));
            FadeSwap(KeyBadge, keyLabel, () => SetBadge(KeyBadge, KeyText, keyLabel));
            FadeSwap(Content, "NoWrap|" + lyricText, () =>
            {
                Content.TextWrapping = TextWrapping.NoWrap;
                Content.Text = lyricText;
                Content.HighlightCount = 0;
            });

            UpdateProgress(lyricId, totalLyrics);            // no fade
        }

        private static readonly Regex VerseRegex =
            new(@"^\s*(?:v(?:erse)?|s(?:tanza)?)\s*(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>"VERSE 2" → "Verse 2 of 3", "CHORUS 2" → "Chorus", "PRE-CHORUS 1" → "Pre-Chorus", ...</summary>
        private static string FormatSection(string? raw, int verseCount)
        {
            raw = raw?.Trim() ?? "";
            if (raw.Length == 0) return "";

            var verse = VerseRegex.Match(raw);
            if (verse.Success)
            {
                string n = verse.Groups[1].Value;
                return verseCount > 1 ? $"Stanza {n} of {verseCount}" : $"Stanza {n}";
            }

            string squashed = raw.Replace(" ", "").Replace("-", "").ToLowerInvariant();
            if (squashed.StartsWith("prechorus")) return "Pre-Chorus";
            if (squashed.StartsWith("chorus")) return "Chorus";
            if (squashed.StartsWith("bridge")) return "Bridge";

            return raw;   // Intro, Outro, Tag... shown as-is
        }
        private void UpdateProgress(int currentId, int total)
        {
            currentLyricId = currentId;
            totalLyrics = total;

            double progress = total > 0
                ? Math.Clamp((double)currentId / total, 0, 1)
                : 0;

            ProgressAccent.Width = ProgressTrack.ActualWidth * progress;
        }

        #endregion


        #region Bible

        private static readonly Regex RefRegex =
            new(@"^\s*(?<book>.+?)\s+(?<chapter>\d+)\s*:\s*(?<verse>\d+(?:\s*[-–]\s*\d+)?)\s*$", RegexOptions.Compiled);

        /// <summary>"1 John 3:16" → ("1 John", "3", "16"). Anything that doesn't look like that → (whole text, null, null).</summary>
        private static (string Book, string? Chapter, string? Verse) ParseReference(string? reference)
        {
            var m = RefRegex.Match(reference ?? "");
            return m.Success
                ? (m.Groups["book"].Value, m.Groups["chapter"].Value, m.Groups["verse"].Value)
                : (reference?.Trim() ?? "", null, null);
        }

        private static void SetPart(TextBlock part, string? text)
        {
            bool has = !string.IsNullOrEmpty(text);
            part.Visibility = has ? Visibility.Visible : Visibility.Collapsed;
            if (has) part.Text = text!;
        }

        public void VerseTextChanged(string verseReference, string translation, string text, int verseNumber, int totalVerses)
        {
            BeginSlide();

            currentKey = null;

            var (book, chapter, verse) = ParseReference(verseReference);
            string? chapterText = chapter == null ? null : " " + chapter;   // leading space separates it from the book
            string? colon = chapter == null ? null : ":";

            FadeSwap(Header1, book, () => Header1.Text = book);
            FadeSwap(RefChapter, chapterText, () => SetPart(RefChapter, chapterText));
            FadeSwap(RefColon, colon, () => SetPart(RefColon, colon));
            FadeSwap(RefVerse, verse, () => SetPart(RefVerse, verse));
            FadeSwap(Header2Badge, translation, () => SetBadge(Header2Badge, Header2, translation));
            FadeSwap(KeyBadge, null, () => SetBadge(KeyBadge, KeyText, null));
            FadeSwap(Content, "Wrap|" + text, () =>
            {
                Content.TextWrapping = TextWrapping.Wrap;
                Content.Text = text;
            });

            UpdateProgress(verseNumber, totalVerses);   // verse 16 of 36 → bar at 44%, no fade
        }

        public void VersePortionChanged(int id)
        {
            Content.HighlightCount = id;
        }

        #endregion

        #region Helpers

        private void Present()
        {
            SetWindow(selectedScreen);
            Show();
        }

        //! ====================================================
        //! [+] FADE: each element fades out/in only when its own value changed
        //! ====================================================
        private static readonly TimeSpan FadeOutTime = TimeSpan.FromMilliseconds(120);
        private static readonly TimeSpan FadeInTime = TimeSpan.FromMilliseconds(220);

        private readonly Dictionary<UIElement, string?> shownValues = new();   // last value each element was told to show
        private readonly Dictionary<UIElement, Action> pendingSwaps = new();   // swap waiting for that element's fade-out
        private bool firstShow;

        private void BeginSlide()
        {
            firstShow = !IsVisible;      // window was hidden: nothing to fade out, just fade in
            Present();
        }

        private void FadeSwap(UIElement element, string? newValue, Action apply)
        {
            if (!firstShow && shownValues.TryGetValue(element, out var oldValue) && oldValue == newValue)
                return;                  // unchanged: no fade at all

            shownValues[element] = newValue;

            if (firstShow)
            {
                apply();
                element.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, FadeInTime));
                return;
            }

            pendingSwaps[element] = apply;   // if values change quickly, only the latest one is applied
            var fadeOut = new DoubleAnimation(0, FadeOutTime);
            fadeOut.Completed += (_, _) =>
            {
                if (pendingSwaps.Remove(element, out var swap))
                    swap();
                element.BeginAnimation(OpacityProperty, new DoubleAnimation(1, FadeInTime));
            };
            element.BeginAnimation(OpacityProperty, fadeOut);
        }

        private static void SetBadge(Border badge, TextBlock label, string? text)
        {
            bool has = !string.IsNullOrWhiteSpace(text);
            badge.Visibility = has ? Visibility.Visible : Visibility.Collapsed;
            if (has) label.Text = text!.Trim();
        }
        private void UpdateHeaderScale()
        {
            if (ActualHeight <= 0)
                return;

            double unit = Math.Max(16, ActualHeight / 22);

            Header1.FontSize = unit;
            RefChapter.FontSize = RefColon.FontSize = RefVerse.FontSize = unit;
            Header2.FontSize = unit * 0.48;
            KeyText.FontSize = unit * 0.42;

            UpdateProgress(currentLyricId, totalLyrics);
        }


        #endregion

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