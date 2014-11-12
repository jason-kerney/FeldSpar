using System.Windows.Media;

namespace FeldSparGuiCSharp.StyleConstants
{
    public static class TestStatusColors
    {
        public static Brush FailureBrush { get { return Brushes.Red; } }
        public static Brush IgnoredBrush { get { return Brushes.Goldenrod; } }
        public static Brush SuccessBrush { get { return Brushes.ForestGreen; } }
        public static Brush RunningBrush { get { return Brushes.DodgerBlue; } }
        public static Brush NoneBrush { get { return Brushes.Gray; } }
    }
}