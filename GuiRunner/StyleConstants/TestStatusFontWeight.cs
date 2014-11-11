using System.Windows;

namespace GuiRunner.StyleConstants
{
    public static class TestStatusFontWeight
    {
        public static FontWeight NoneFontWeight { get { return FontWeights.Normal; } }
        public static FontWeight RunningFontWeight { get { return FontWeights.Bold; } }
        public static FontWeight PassedFontWeight { get { return FontWeights.Normal; } }
        public static FontWeight FailedFontWeight { get { return FontWeights.Bold; } }
        public static FontWeight IgnoredFontWeight { get { return FontWeights.Bold; } }
        public static FontWeight SuccessFontWeight { get { return FontWeights.Normal; } }
    }
}