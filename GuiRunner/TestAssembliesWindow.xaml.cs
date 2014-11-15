using System.Windows;
using FeldSpar.ClrInterop;
using FeldSparGuiCSharp.VeiwModels;

namespace FeldSparGuiCSharp
{
    /// <summary>
    /// Interaction logic for TestAssembliesWindow.xaml
    /// </summary>
    public partial class TestAssembliesWindow : Window
    {
        public TestAssembliesWindow()
        {
            InitializeComponent();
            TestAssemblies = new TestsMainModel();
        }

        public ITestsMainModel TestAssemblies
        {
            get { return (ITestsMainModel)DataContext; }
            set { DataContext = value; }
        }
    }
}
