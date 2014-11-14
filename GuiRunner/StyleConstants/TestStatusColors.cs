using System.Windows.Media;
using FeldSpar.ClrInterop;

namespace FeldSparGuiCSharp.StyleConstants
{
    public static class TestStatusColors
    {
        public static Brush FailureBrush { get { return Brushes.Red; } }
        public static Brush IgnoredBrush { get { return Brushes.Goldenrod; } }
        public static Brush SuccessBrush { get { return Brushes.ForestGreen; } }
        public static Brush RunningBrush { get { return Brushes.DodgerBlue; } }
        public static Brush NoneBrush { get { return Brushes.Gray; } }

        public static System.Windows.Media.Brush GetStatusBrush(TestStatus status)
        {
            switch (status)
            {
                case TestStatus.Success:
                    return SuccessBrush;
                case TestStatus.Running:
                    return RunningBrush;
                case TestStatus.Ignored:
                    return IgnoredBrush;
                case TestStatus.Failure:
                    return FailureBrush;
                default:
                    return NoneBrush;
            }
        }
    }
}