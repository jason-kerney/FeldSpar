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

    [Serializable]
    public class MainViewModel : PropertyNotifyBase
    {
        Engine engine;
        private ObservableCollection<TestDetail> tests = new ObservableCollection<TestDetail>();
        private ObservableCollection<TestResult> results = new ObservableCollection<TestResult>();

        private Dictionary<string, TestDetail> knownTests = new Dictionary<string, TestDetail>();

        private const string path = @"C:\Users\Jason\Documents\GitHub\FeldSpar\GuiRunner\bin\Debug\FeldSpar.Tests.dll";

        private bool _isRunning;

        public MainViewModel()
        {
            engine = new Engine();
            engine.TestFound += (sender, args) =>
            {
                if (!knownTests.ContainsKey(args.Name))
                {
                    var testDetail = new TestDetail{Name = args.Name, Status = TestStatus.None};
                    Tests.Add(testDetail);
                    knownTests.Add(testDetail.Name, testDetail);
                }
            };

            engine.TestFinished += (sender, args) =>
            {
                results.Add(args.TestResult);
                TestStatus status;
                string msg = string.Empty;

                if (args.TestResult.IsFailure && ((TestResult.Failure)args.TestResult).Item.IsIgnored)
                {
                    var failure = ((TestResult.Failure) args.TestResult).Item;
                    status = TestStatus.Ignored;
                    msg = failure.ToString();

                }
                else if (args.TestResult.IsFailure)
                {
                    var failure = ((TestResult.Failure)args.TestResult).Item;
                    status = TestStatus.Failure;
                    msg = failure.ToString();
                }
                else
                {
                    status = TestStatus.Success;
                }

                var testDetail = knownTests[args.Name];
                testDetail.Status = status;
                testDetail.FailDetail = msg;
            };

            engine.TestRunning += (sender, args) =>
            {
                knownTests[args.Name].Status = TestStatus.Running;
            };

            engine.FindTests(path);
        }

        public async void Run(object ignored)
        {
            IsRunning = true;
            var random = new Random();
            results.Clear();

            foreach (var test in Tests)
            {
                test.Status = TestStatus.None;
            }

            await Task.Run(() => engine.RunTests(path));
            engine.RunTests(path);

            results.Clear();
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
