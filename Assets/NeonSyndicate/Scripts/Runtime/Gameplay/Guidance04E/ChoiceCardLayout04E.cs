using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How the pausing choice card (MenuSystem.ShowChoice) lays out one body text.</summary>
    public struct ChoiceCardFit04E
    {
        /// <summary>The body font size.</summary>
        public int TextSize;
        /// <summary>The card's height (the card is centred on the screen).</summary>
        public float CardHeight;
        /// <summary>The height the body text needs at TextSize.</summary>
        public float TextHeight;
        /// <summary>The height of the body area between the title and the buttons.</summary>
        public float AreaHeight;
        /// <summary>True when even the smallest size on the tallest card does not fit: the body scrolls.</summary>
        public bool Scroll;
    }

    /// <summary>
    /// Fits the choice card's body text (offers, update and next-step cards, contract boards) between its title and its
    /// buttons. A short text keeps the classic card (half the screen high, size 24). A longer one grows the card up to 90%
    /// of the screen, then steps the font down to 16, and only then scrolls, so the text never runs under the buttons.
    /// Pure: the caller measures the wrapped text height at each size.
    /// </summary>
    public static class ChoiceCardLayout04E
    {
        public const float Width = 1200f;
        /// <summary>Card padding left and right of the body text.</summary>
        public const float SidePad = 60f;
        /// <summary>From the card's top edge to the body text (the title row).</summary>
        public const float TitleBlock = 110f;
        /// <summary>From the body area's bottom to the card's bottom edge (a 20 px gap, the 90 px buttons, 50 px margin).</summary>
        public const float ButtonBlock = 160f;
        public static readonly int[] Sizes = { 24, 22, 20, 18, 16 };

        /// <summary>The body text's wrap width.</summary>
        public static float TextWidth { get { return Width - SidePad * 2f; } }

        /// <summary>Chooses the font size and card height for a body text whose wrapped height at a size is textHeightAt(size).</summary>
        public static ChoiceCardFit04E Fit(float screenH, Func<int, float> textHeightAt)
        {
            float minCard = screenH * 0.5f, maxCard = Mathf.Max(minCard, screenH * 0.9f);
            ChoiceCardFit04E fit = new ChoiceCardFit04E();
            for (int i = 0; i < Sizes.Length; i++)
            {
                float th = textHeightAt != null ? Mathf.Max(0f, textHeightAt(Sizes[i])) : 0f;
                float need = TitleBlock + th + ButtonBlock;
                if (need > maxCard) continue;
                fit.TextSize = Sizes[i];
                fit.TextHeight = th;
                fit.CardHeight = Mathf.Max(minCard, need);
                fit.AreaHeight = fit.CardHeight - TitleBlock - ButtonBlock;
                fit.Scroll = false;
                return fit;
            }
            int smallest = Sizes[Sizes.Length - 1];
            fit.TextSize = smallest;
            fit.TextHeight = textHeightAt != null ? Mathf.Max(0f, textHeightAt(smallest)) : 0f;
            fit.CardHeight = maxCard;
            fit.AreaHeight = maxCard - TitleBlock - ButtonBlock;
            fit.Scroll = true;
            return fit;
        }
    }
}
