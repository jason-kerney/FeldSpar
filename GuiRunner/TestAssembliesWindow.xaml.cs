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
            TestAssemblies = new ObservableCollection<TestsMainModel>();
            TestAssemblies.Add(new TestsMainModel());
        }

        public ObservableCollection<TestsMainModel> TestAssemblies
        {
            get { return (ObservableCollection<TestsMainModel>) DataContext; }
            set { DataContext = value; }
        }
    }
}
