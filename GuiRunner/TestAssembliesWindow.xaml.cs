using System.Collections.ObjectModel;
using System.Windows;
using ViewModel;

namespace GuiRunner
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

        public TestsMainModel TestAssemblies
        {
            get { return (TestsMainModel) DataContext; }
            set { DataContext = value; }
        }
    }
}
