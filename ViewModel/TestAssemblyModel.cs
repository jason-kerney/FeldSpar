using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using FeldSpar.ClrInterop;
using FeldSpar.Framework;

namespace ViewModel
{
    public class TestAssemblyModel : PropertyNotifyBase
    {
        readonly Engine engine;
        private readonly ObservableCollection<TestDetailModel> tests = new ObservableCollection<TestDetailModel>();
        private readonly ObservableCollection<TestResult> results = new ObservableCollection<TestResult>();

        private readonly Dictionary<string, TestDetailModel> knownTests = new Dictionary<string, TestDetailModel>();

        private readonly string path;
        private bool isRunning;

        public TestAssemblyModel()
            : this(@"C:\Users\Jason\Documents\GitHub\FeldSpar\GuiRunner\bin\Debug\FeldSpar.Tests.dll")
        {
            
        }


        public TestAssemblyModel(string path)
        {
            this.path = path;

            engine = new Engine();
            engine.TestFound += (sender, args) =>
            {
                if (!knownTests.ContainsKey(args.Name))
                {
                    var testDetail = new TestDetailModel{Name = args.Name, Status = TestStatus.None};
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

            Name = Path.GetFileName(path);
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
            get { return isRunning; }
            set
            {
                if (isRunning == value)
                {
                    return;
                }

                isRunning = value;
                OnPropertyChanged();
            }
        }

        public string Name { get; private set; }

        public ObservableCollection<TestDetailModel> Tests { get { return tests; } }

        public ObservableCollection<TestResult> Results { get { return results; } }

        public ICommand RunCommand { get { return new DelegateCommand(Run, _ => !IsRunning); } }
    }
}
