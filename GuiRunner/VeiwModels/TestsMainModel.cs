using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using FeldSpar.ClrInterop;
using FeldSpar.Framework;

namespace FeldSparGuiCSharp.VeiwModels
{
    public interface ITestsMainModel
    {
        bool IsRunning { get; set; }
        string Description { get; }
        ITestDetailModel Selected { get; set; }
        ObservableCollection<ITestAssemblyModel> Assemblies { get; set; }
        TestResult[] Results { get; }
        ITestDetailModel[] Tests { get; }
        ICommand RunCommand { get; }
        ICommand AddCommand { get; }
        void Run(object ignored);
        void Add(object ignored);
    }

    public class TestsMainModel : PropertyNotifyBase, ITestsMainModel
    {
        private string description;
        private ITestDetailModel selected;

        public TestsMainModel()
        {
            var itemsRemovedActions = new[] { NotifyCollectionChangedAction.Remove, NotifyCollectionChangedAction.Replace };

            Assemblies = new ObservableCollection<ITestAssemblyModel>();
            Assemblies.CollectionChanged += (sender, args) =>
            {
                var testChanged = false;
                foreach (INotifyPropertyChanged newItem in args.NewItems)
                {
                    newItem.PropertyChanged += ItemOnPropertyChanged;
                    testChanged = true;
                }

                if (testChanged)
                {
                    OnPropertyChanged("Tests");
                }

                if (itemsRemovedActions.All(x => args.Action != x))
                {
                    return;
                }

                testChanged = false;
                foreach (INotifyPropertyChanged oldItem in args.OldItems)
                {
                    oldItem.PropertyChanged -= ItemOnPropertyChanged;
                    testChanged = true;
                }

                if (testChanged)
                {
                    OnPropertyChanged("Tests");
                }
            };
        }

        private void ItemOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "Results")
            {
                OnPropertyChanged("Results");
            }
            if (propertyChangedEventArgs.PropertyName == "Tests")
            {
                OnPropertyChanged("Tests");
            }
        }

        public bool IsRunning { get; set; }

        public void Run(object ignored)
        {
            IsRunning = true;
            foreach (var testAssemblyModel in Assemblies)
            {
                var model = testAssemblyModel;
                model.Run(ignored);
            }

            IsRunning = false;
        }

        public void Add(object ignored)
        {
            var fileOpen = new Microsoft.Win32.OpenFileDialog {Filter = "test suites|*.dll;*.exe", Multiselect = false};

            var result = fileOpen.ShowDialog();
            if (result != true)
            {
                return;
            }

            if (Assemblies.All(assembly => assembly.AssemblyPath != fileOpen.FileName))
            {
                Assemblies.Add(new TestAssemblyModel(fileOpen.FileName));
            }
        }

        public string Description
        {
            get { return description; }
            private set
            {
                if (value == description)
                {
                    return;
                }

                description = value;
                OnPropertyChanged();
            }
        }

        public ITestDetailModel Selected
        {
            get { return selected; }
            set
            {
                if (selected == value)
                {
                    return;
                }

                selected = value;
                OnPropertyChanged();

                Description = selected.FailDetail + "";
            }
        }

        public ObservableCollection<ITestAssemblyModel> Assemblies { get; set; }

        private T[] GetTestItems<T>(Func<ITestAssemblyModel, IEnumerable<T>> selector) { return Assemblies.SelectMany(selector).ToArray(); }

        public TestResult[] Results { get { return GetTestItems(assembly => assembly.Results); } }

        public ITestDetailModel[] Tests { get { return GetTestItems(assembyly => assembyly.Tests); } }

        public ICommand RunCommand { get { return new DelegateCommand(Run, _ => !IsRunning); } }

        public ICommand AddCommand { get { return new DelegateCommand(Add); } }
    }
}