using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using FeldSpar.ClrInterop;
using FeldSpar.Framework;

namespace FeldSparGuiCSharp.VeiwModels
{
    public class TestAssemblyModel : PropertyNotifyBase
    {
        readonly Engine engine;
        private readonly ObservableCollection<TestDetailModel> tests = new ObservableCollection<TestDetailModel>();
        private readonly ObservableCollection<TestResult> results = new ObservableCollection<TestResult>();

        private readonly Dictionary<string, TestDetailModel> knownTests = new Dictionary<string, TestDetailModel>();

        private readonly string assemblyPath;
        private bool isRunning;
        private bool isVisible;

        public TestAssemblyModel(string assemblyPath)
        {
            isVisible = true;
            this.assemblyPath = assemblyPath;
            Name = Path.GetFileName(AssemblyPath);

            engine = new Engine();
            engine.TestFound += (sender, args) =>
            {
                if (knownTests.ContainsKey(args.Name))
                {
                    return;
                }

                var testDetail = new TestDetailModel{Name = args.Name, Status = TestStatus.None, AssemblyName = Name, Parent = this};
                Tests.Add(testDetail);
                knownTests.Add(testDetail.Name, testDetail);
                OnPropertyChanged("Tests");
            };

            engine.TestFinished += (sender, args) =>
            {
                results.Add(args.TestResult);

                var info = ConvertResultIntoTestInfo(args.TestResult);

                var testDetail = knownTests[args.Name];
                testDetail.Status = info.Item1;
                testDetail.FailDetail = info.Item2;
                OnPropertyChanged("Results");
                OnPropertyChanged("Tests");
            };

            engine.TestRunning += (sender, args) =>
            {
                knownTests[args.Name].Status = TestStatus.Running;
            };

            engine.FindTests(assemblyPath);

        }

        private static Tuple<TestStatus, string> ConvertResultIntoTestInfo(TestResult testResult)
        {
            TestStatus status;
            string msg = string.Empty;
            Tuple<TestStatus, string> info;

            if (testResult.IsFailure)
            {
                status = TestStatus.Failure;
                var failure = ((TestResult.Failure) testResult).Item;
                if (failure.IsExceptionFailure)
                {
                    msg = ((FailureType.ExceptionFailure) failure).Item.ToString();
                }
                else if (failure.IsExpectationFailure)
                {
                    msg = ((FailureType.ExpectationFailure) failure).Item;
                }
                else if (failure.IsGeneralFailure)
                {
                    msg = "General Failure" + Environment.NewLine + ((FailureType.GeneralFailure) failure).Item;
                }
                else if (failure.IsIgnored)
                {
                    status = TestStatus.Ignored;
                    msg = "Ignored:" + Environment.NewLine + ((FailureType.Ignored) failure).Item;
                }
                else if (failure.IsStandardNotMet)
                {
                    msg = "Standard not met, check the comparison";
                }
            }
            else
            {
                status = TestStatus.Success;
            }
            info = Tuple.Create(status, msg);
            return info;
        }

        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (isVisible == value)
                {
                    return;
                }
                isVisible = value;
                OnPropertyChanged();
            }
        }

        public async void Run(object ignored)
        {
            IsRunning = true;
            results.Clear();

            foreach (var test in Tests)
            {
                test.Status = TestStatus.None;
            }

            await Task.Run(() => engine.RunTests(assemblyPath));

            IsRunning = false;
        }

        public void ToggleVisible(object ignored)
        {
            IsVisible = !IsVisible;
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

        public string AssemblyPath { get { return assemblyPath; } }

        public ObservableCollection<TestDetailModel> Tests { get { return tests; } }

        public ObservableCollection<TestResult> Results { get { return results; } }

        public ICommand RunCommand { get { return new DelegateCommand(Run, _ => !IsRunning); } }

        public ICommand ToggleVisibilityCommand { get { return new DelegateCommand(ToggleVisible);} }
    }
}
