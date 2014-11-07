using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using FeldSpar.Framework;

namespace ViewModel
{
    public class TestsMainModel : PropertyNotifyBase
    {
        private readonly string path1 = @"C:\Users\Jason\Documents\GitHub\FeldSpar\GuiRunner\bin\Debug\FeldSpar.Tests.dll";
        private readonly string path2 = @"C:\Users\Jason\Documents\GitHub\FeldSpar\GuiRunner\bin\Debug\PathFindindTests.dll";
        private ObservableCollection<TestAssemblyModel> assemblies = new ObservableCollection<TestAssemblyModel>();

        public TestsMainModel()
        {
            var itemsRemovedActions = new[] { NotifyCollectionChangedAction.Remove, NotifyCollectionChangedAction.Replace };

            assemblies.Add(new TestAssemblyModel(path1));
            assemblies.Add(new TestAssemblyModel(path2));

            assemblies.CollectionChanged += (sender, args) =>
            {
                foreach (TestDetailModel newItem in args.NewItems)
                {
                    newItem.PropertyChanged += ItemOnPropertyChanged;
                }

                if (itemsRemovedActions.All(x => args.Action != x))
                {
                    return;
                }

                foreach (TestDetailModel oldItem in args.OldItems)
                {
                    oldItem.PropertyChanged -= ItemOnPropertyChanged;
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
            foreach (var testAssemblyModel in assemblies)
            {
                var model = testAssemblyModel;
                model.Run(ignored);
            }

            IsRunning = false;
        }


        public ObservableCollection<TestAssemblyModel> Assemblies
        {
            get { return assemblies; }
            set { assemblies = value; }
        }

        private IEnumerable<T> GetTestItems<T>(Func<TestAssemblyModel, IEnumerable<T>> selector) { return assemblies.SelectMany(selector); }

        public IEnumerable<TestResult> Results { get { return GetTestItems(assembly => assembly.Results); } }

        public IEnumerable<TestDetailModel> Tests { get { return GetTestItems(assembyly => assembyly.Tests); } }
        public ICommand RunCommand { get { return new DelegateCommand(Run, _ => !IsRunning); } }
    }
}