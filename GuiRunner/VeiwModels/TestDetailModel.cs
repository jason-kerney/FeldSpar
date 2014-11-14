using FeldSpar.ClrInterop;

namespace FeldSparGuiCSharp.VeiwModels
{
    public interface ITestDetailModel
    {
        string Name { get; set; }
        TestStatus Status { get; set; }
        string FailDetail { get; set; }
        string AssemblyName { get; set; }
        ITestAssemblyModel Parent { get; set; }
        void OnPropertyChanged(string propertyName);
        void OnPropertyChanged();
    }

    public class TestDetailModel : PropertyNotifyBase, ITestDetailModel
    {
        private string name;
        private TestStatus status;
        private string failDetail;
        private string assemblyName;
        private ITestAssemblyModel parent;

        public string Name
        {
            get { return name; }
            set
            {
                if (name == value)
                {
                    return;
                }
                name = value;
                OnPropertyChanged();
            }
        }

        public TestStatus Status
        {
            get { return status; }
            set
            {
                if (status == value)
                {
                    return;
                }
                
                status = value;
                OnPropertyChanged();
            }
        }

        public string FailDetail
        {
            get { return failDetail; }
            set
            {
                if (failDetail == value)
                {
                    return;
                }

                failDetail = value;
                OnPropertyChanged();
            }
        }

        public string AssemblyName
        {
            get { return assemblyName; }
            set
            {
                if (value == assemblyName)
                {
                    return;
                }

                assemblyName = value;
                OnPropertyChanged();
            }
        }

        public ITestAssemblyModel Parent
        {
            get { return parent; }
            set
            {
                if (parent == value)
                {
                    return;
                }

                parent = value;
                OnPropertyChanged();
            }
        }
    }
}