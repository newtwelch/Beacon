using Backend.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Backend.Core.Models.Bible
{
    //CHATGPT GENERATED with some fixes
    public class VerseNavigator
    {
        public List<BeaconVerse> Verses => verses;

        public BeaconVerse? SelectedVerse { get; private set; }

        public VersePortion? SelectedPortion { get; private set; }

        public IReadOnlyList<VersePortion> Portions => portions;

        public bool IsExpanded => SelectedPortion != null;

        public int VerseIndex
            => SelectedVerse == null
                ? -1
                : verses.IndexOf(SelectedVerse);

        public int PortionIndex
            => SelectedPortion == null
                ? -1
                : portions.IndexOf(SelectedPortion);

        public event Action? SelectionChanged;

        private readonly List<BeaconVerse> verses = new();

        private readonly List<VersePortion> portions = new();

        //----------------------------------------------------------
        // Loading
        //----------------------------------------------------------
        public void SetVerses(IEnumerable<BeaconVerse> source)
        {
            verses.Clear();

            verses.AddRange(source);

            SelectedVerse = null;
            SelectedPortion = null;

            portions.Clear();

            SelectionChanged?.Invoke();
        }

        //----------------------------------------------------------
        // Verse Selection
        //----------------------------------------------------------
        public void SelectVerse(BeaconVerse verse)
        {
            if (SelectedVerse == verse)
                return;

            SelectedVerse = verse;

            SelectedPortion = null;

            portions.Clear();

            SelectionChanged?.Invoke();
        }

        public void Expand()
        {
            if (SelectedVerse == null)
                return;

            portions.Clear();

            portions.AddRange(
                CreatePortions(SelectedVerse));

            if (portions.Count > 0)
                SelectedPortion = portions[0];

            SelectionChanged?.Invoke();
        }

        public void Collapse()
        {
            SelectedPortion = null;

            portions.Clear();

            SelectionChanged?.Invoke();
        }

        public void SelectPortion(VersePortion portion)
        {
            SelectedPortion = portion;

            SelectionChanged?.Invoke();
        }

        //----------------------------------------------------------
        // Portion Generation
        //----------------------------------------------------------

        private static List<VersePortion> CreatePortions(
            BeaconVerse verse)
        {
            return VersePortioner.Split(verse.Text)
                .Select((x, i) => new VersePortion
                {
                    Verse = verse,
                    Index = i + 1,
                    Text = x,
                    OriginalText = x
                })
                .ToList();
        }

    //----------------------------------------------------------
// Navigation
//----------------------------------------------------------

public bool Next()
        {
            //
            // Nothing selected yet
            //

            if (SelectedVerse == null)
            {
                if (verses.Count == 0)
                    return false;

                SelectVerse(verses[0]);
                return true;
            }

            //
            // Verse navigation only
            //

            if (!IsExpanded)
            {
                return NextVerse();
            }

            //
            // Portion navigation
            //

            var portionIndex = portions.IndexOf(SelectedPortion!);

            if (portionIndex < portions.Count - 1)
            {
                SelectedPortion = portions[portionIndex + 1];
                SelectionChanged?.Invoke();
                return true;
            }

            //
            // Last portion
            // Go to next verse and expand automatically
            //

            if (!NextVerse())
                return false;

            Expand();

            return true;
        }

        public bool Previous()
        {
            //
            // Nothing selected
            //

            if (SelectedVerse == null)
                return false;

            //
            // Verse navigation only
            //

            if (!IsExpanded)
            {
                return PreviousVerse();
            }

            //
            // Portion navigation
            //

            var portionIndex = portions.IndexOf(SelectedPortion!);

            if (portionIndex > 0)
            {
                SelectedPortion = portions[portionIndex - 1];
                SelectionChanged?.Invoke();
                return true;
            }

            //
            // First portion
            // Jump to previous verse
            //

            if (!PreviousVerse())
                return false;

            Expand();

            if (portions.Count > 0)
                SelectedPortion = portions[^1];

            SelectionChanged?.Invoke();

            return true;
        }

        public bool NextVerse()
        {
            if (SelectedVerse == null)
                return false;

            var index = verses.IndexOf(SelectedVerse);

            if (index >= verses.Count - 1)
                return false;

            SelectVerse(verses[index + 1]);

            return true;
        }

        public bool PreviousVerse()
        {
            if (SelectedVerse == null)
                return false;

            var index = verses.IndexOf(SelectedVerse);

            if (index <= 0)
                return false;

            SelectVerse(verses[index - 1]);

            return true;
        }

        public bool ExpandSelectedVerse()
        {
            if (SelectedVerse == null)
                return false;

            if (IsExpanded)
                return false;

            Expand();

            return true;
        }

        public bool CollapseSelectedVerse()
        {
            if (!IsExpanded)
                return false;

            Collapse();

            return true;
        }

        public bool HasNextVerse()
        {
            if (SelectedVerse == null)
                return verses.Count > 0;

            return VerseIndex < verses.Count - 1;
        }

        public bool HasPreviousVerse()
        {
            if (SelectedVerse == null)
                return false;

            return VerseIndex > 0;
        }

        public bool HasNextPortion()
        {
            if (!IsExpanded)
                return false;

            return PortionIndex < portions.Count - 1;
        }

        public bool HasPreviousPortion()
        {
            if (!IsExpanded)
                return false;

            return PortionIndex > 0;
        }

        public void SelectFirstVerse()
        {
            if (verses.Count == 0)
                return;

            SelectVerse(verses[0]);
        }

        public void SelectLastVerse()
        {
            if (verses.Count == 0)
                return;

            SelectVerse(verses[^1]);
        }

        public void ExpandFirstPortion()
        {
            Expand();

            if (portions.Count > 0)
            {
                SelectedPortion = portions[0];
                SelectionChanged?.Invoke();
            }
        }

        public void ExpandLastPortion()
        {
            Expand();

            if (portions.Count > 0)
            {
                SelectedPortion = portions[^1];
                SelectionChanged?.Invoke();
            }
        }
    }

}