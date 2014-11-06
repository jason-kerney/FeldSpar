using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using FeldSpar.ClrInterop;
using FeldSpar.Framework;
using ViewModel.Annotations;

namespace ViewModel
{
    public class PropertyNotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TestDetail : PropertyNotifyBase
    {
        private string _name;
        private TestStatus _status;
        private string _failDetail;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value)
                {
                    return;
                }
                _name = value;
                OnPropertyChanged();
            }
        }

        public TestStatus Status
        {
            get { return _status; }
            set
            {
                if (_status == value)
                {
                    return;
                }
                
                _status = value;
                OnPropertyChanged();
            }
        }

        public string FailDetail
        {
            get { return _failDetail; }
            set
            {
                if (_failDetail == value)
                {
                    return;
                }

                _failDetail = value;
                OnPropertyChanged();
            }
        }
    }

    public enum TestStatus
    {
        None,
        Running,
        Success,
        Failure,
        Ignored
    }

    public class MainViewModel : PropertyNotifyBase
    {
        Engine engine;
        private ObservableCollection<TestDetail> tests = new ObservableCollection<TestDetail>();
        private ObservableCollection<TestResult> results = new ObservableCollection<TestResult>();

        private Assembly _testFeldSparAssembly;
        private bool _isRunning;

        public MainViewModel()
        {
            engine = new FeldSpar.ClrInterop.Engine();
            engine.TestFound += (sender, args) =>
            {
                if(Tests.All(x => x.Name != args.Name))
                    Tests.Add(new TestDetail{Name = args.Name, Status = TestStatus.None});
            };

            engine.TestFinished += (sender, args) =>
            {
                results.Add(args.TestResult);
            };

            engine.TestRunning += (sender, args) =>
            {

            };

            _testFeldSparAssembly = FeldSpar.Console.Helpers.Data.testFeldSparAssembly;
            engine.FindTests(_testFeldSparAssembly);
        }

        public async void Run(object ignored)
        {
            var random = new Random();
            IsRunning = true;
            //engine.RunTests(_testFeldSparAssembly);
            foreach (var test in Tests)
            {
                test.Status = TestStatus.Running;
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                if (random.Next(10) == 1)
                {
                    test.Status = TestStatus.Failure;
                    results.Add(TestResult.NewFailure(FailureType.NewGeneralFailure("Failed")));
                }
                else
                {
                    test.Status = TestStatus.Success;
                    results.Add(TestResult.Success);
                }
            }

            IsRunning = false;
        }

        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                if (_isRunning == value)
                {
                    return;
                }

                _isRunning = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TestDetail> Tests { get { return tests; } }

        public ObservableCollection<TestResult> Results { get { return results; } }

        public ICommand RunCommand { get { return new DelegateCommand(Run, _ => !IsRunning); } }
    }
}
