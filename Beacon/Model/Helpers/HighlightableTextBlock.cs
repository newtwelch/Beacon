using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Beacon.Model.Helpers
{
    public class HighlightableTextBlock : TextBlock
    {
        #region Properties

        public new string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static readonly DependencyPropertyKey MatchCountPropertyKey
            = DependencyProperty.RegisterReadOnly("MatchCount", typeof(int), typeof(HighlightableTextBlock),
                new PropertyMetadata(0));

        public static readonly DependencyProperty MatchCountProperty
            = MatchCountPropertyKey.DependencyProperty;

        public static readonly DependencyProperty HighlightTextProperty =
            DependencyProperty.Register("HighlightText", typeof(string), typeof(HighlightableTextBlock),
                new PropertyMetadata(string.Empty, UpdateHighlighting));

        public new static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string),
            typeof(HighlightableTextBlock), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(UpdateHighlighting)));

        public string HighlightPhrase
        {
            get => (string)GetValue(HighlightPhraseProperty);
            set => SetValue(HighlightPhraseProperty, value);
        }

        public static readonly DependencyProperty HighlightPhraseProperty =
            DependencyProperty.Register("HighlightPhrase", typeof(string),
            typeof(HighlightableTextBlock), new FrameworkPropertyMetadata(String.Empty, FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(UpdateHighlighting)));

        public Brush HighlightBrush
        {
            get => (Brush)GetValue(HighlightBrushProperty);
            set => SetValue(HighlightBrushProperty, value);
        }

        public int HighlightCount
        {
            get => (int)GetValue(HighlightCountProperty);
            set => SetValue(HighlightCountProperty, value);
        }

        public static readonly DependencyProperty HighlightCountProperty =
            DependencyProperty.Register("HighlightCount", typeof(int),
            typeof(HighlightableTextBlock), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(UpdateHighlighting)));

        public static readonly DependencyProperty HighlightBrushProperty =
            DependencyProperty.Register("HighlightBrush", typeof(Brush),
            typeof(HighlightableTextBlock), new FrameworkPropertyMetadata(Brushes.Orange, FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(UpdateHighlighting)));

        public bool IsCaseSensitive
        {
            get => (bool)GetValue(IsCaseSensitiveProperty);
            set => SetValue(IsCaseSensitiveProperty, value);
        }

        public bool IgnoreDot
        {
            get => (bool)GetValue(IsCaseSensitiveProperty);
            set => SetValue(IsCaseSensitiveProperty, value);
        }

        public int MatchCount
        {
            get => (int)GetValue(MatchCountProperty);
            protected set => SetValue(MatchCountPropertyKey, value);
        }

        public static readonly DependencyProperty IsCaseSensitiveProperty =
            DependencyProperty.Register("IsCaseSensitive", typeof(bool),
            typeof(HighlightableTextBlock), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(UpdateHighlighting)));

        private static void UpdateHighlighting(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ApplyHighlight(d as HighlightableTextBlock);


        #endregion

        #region Members

        private static void ApplyHighlight(HighlightableTextBlock? tb)
        {
            if (tb is null) return;
         
            string highlightPhrase = tb.HighlightPhrase;
            string text = tb.Text;

            if (tb.IgnoreDot) highlightPhrase = highlightPhrase.Replace(".", "");

            tb.Inlines.Clear();
            tb.SetValue(MatchCountPropertyKey, 0);

            if ((string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(highlightPhrase)) && tb.HighlightCount == 0)
            {
                tb.Inlines.Add(new Run(text));
                return;
            }

            var portions = Regex.Split(text, @"(?<=[\.,;:!\?])\s+");
            for (int i = 1; i <= portions.Length; i++)
            {
                var portion = portions[i - 1];
                if(i < portions.Length)
                {
                    portion += " ";
                }

                if(tb.HighlightCount == i)
                {
                    tb.Inlines.Add(tb.GetRunForText(portion));
                }
                else
                {
                    tb.Inlines.Add(portion);
                }

            }

            //for (int i = 0; i < text.Length; i++)
            //{
            //    if (i + highlightPhrase.Length > text.Length)
            //    {
            //        tb.Inlines.Add(new Run(text.Substring(i)));
            //        break;
            //    }

            //    int nextHighlightPhrase = text.IndexOf(highlightPhrase, i, StringComparison.InvariantCultureIgnoreCase);

            //    if (nextHighlightPhrase == -1)
            //    {
            //        tb.Inlines.Add(new Run(text.Substring(i)));
            //        break;
            //    }

            //    tb.Inlines.Add(new Run(text.Substring(i, nextHighlightPhrase - i)));
            //    tb.Inlines.Add(tb.GetRunForText(text.Substring(nextHighlightPhrase, highlightPhrase.Length)));

            //    i = nextHighlightPhrase + highlightPhrase.Length - 1;
            //}

            //=====================================
            //Ancient Code I Made Previously 
            //=====================================
            //var phrases = highlightPhrase.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            //int firstHighlight = 0;
            //int nextHighlight = 0;
            //int previousHighlight = 0;
            //for (int i = 0; i < phrases.Length; i++)
            //{
            //    if(i == 0)
            //    {
            //        firstHighlight = text.IndexOf(phrases[0], previousHighlight, StringComparison.InvariantCultureIgnoreCase);

            //        if(firstHighlight == -1)
            //        {
            //            tb.Inlines.Add(new Run(text));
            //            break;
            //        }

            //        tb.Inlines.Add(new Run(text.Substring(0, firstHighlight))); // BEFORE THE PHRASE
            //        tb.Inlines.Add(tb.GetRunForText(text.Substring(firstHighlight, phrases[0].Length))); // PHRASE
            //        previousHighlight = firstHighlight;
            //        if (phrases.Length > 1) continue;
            //        else
            //        {
            //            tb.Inlines.Add(new Run(text.Substring(firstHighlight + phrases[0].Length)));
            //            break;
            //        }
            //    }

            //    if (phrases.Length > 1)
            //    {
            //        nextHighlight = text.IndexOf(phrases[i], previousHighlight + phrases[i - 1].Length, StringComparison.InvariantCultureIgnoreCase);
            //        if (nextHighlight == -1 || nextHighlight - (previousHighlight + phrases[i - 1].Length) > 2)
            //        {
            //            tb.Inlines.Add(new Run(text.Substring(previousHighlight + phrases[i - 1].Length, text.Length - (previousHighlight + phrases[i - 1].Length))));
            //            break;
            //        }
            //        tb.Inlines.Add(new Run(text.Substring(previousHighlight + phrases[i - 1].Length, nextHighlight - (previousHighlight + phrases[i - 1].Length))));
            //        tb.Inlines.Add(tb.GetRunForText(text.Substring(nextHighlight, phrases[i].Length)));
            //        previousHighlight = nextHighlight;

            //        if (i == phrases.Length - 1)
            //        {
            //            i = 0;
            //            previousHighlight += phrases[i].Length - 2;

            //            //tb.Inlines.Add(new Run(text.Substring(nextHighlight + phrases[i].Length, text.Length - (nextHighlight + phrases[i].Length))));
            //        }
            //    }
            //}

        }

        private Run GetRunForText(string text)
        {
            var textRun = new Run(text) { Foreground = HighlightBrush };
            return textRun;
        }
        #endregion
    }
}
