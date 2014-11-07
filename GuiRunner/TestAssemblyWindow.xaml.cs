using System.Windows;
using ViewModel;

namespace GuiRunner
{
    /// <summary>
    /// Interaction logic for TestAssemblyWindow.xaml
    /// </summary>
    public partial class TestAssemblyWindow : Window
    {
        public TestAssemblyWindow()
        {
            InitializeComponent();
            ViewModel = new TestAssemblyModel();
        }

        public ViewModel.TestAssemblyModel ViewModel
        {
            get { return (TestAssemblyModel)DataContext; }
            set { DataContext = value; }
        }
    }
}
