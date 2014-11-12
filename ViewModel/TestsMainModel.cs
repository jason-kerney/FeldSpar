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
        private string description;
        private TestDetailModel selected;

        public TestsMainModel()
        {
            var itemsRemovedActions = new[] { NotifyCollectionChangedAction.Remove, NotifyCollectionChangedAction.Replace };

            assemblies.CollectionChanged += (sender, args) =>
            {
                foreach (TestAssemblyModel newItem in args.NewItems)
                {
                    newItem.PropertyChanged += ItemOnPropertyChanged;
                }

                if (itemsRemovedActions.All(x => args.Action != x))
                {
                    return;
                }

                foreach (TestAssemblyModel oldItem in args.OldItems)
                {
                    oldItem.PropertyChanged -= ItemOnPropertyChanged;
                }
            };

            assemblies.Add(new TestAssemblyModel(path1));
            assemblies.Add(new TestAssemblyModel(path2));
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

        public TestDetailModel Selected
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

        public ObservableCollection<TestAssemblyModel> Assemblies
        {
            get { return assemblies; }
            set { assemblies = value; }
        }

        private T[] GetTestItems<T>(Func<TestAssemblyModel, IEnumerable<T>> selector) { return assemblies.SelectMany(selector).ToArray(); }

        public TestResult[] Results { get { return GetTestItems(assembly => assembly.Results); } }

        public TestDetailModel[] Tests { get { return GetTestItems(assembyly => assembyly.Tests); } }
        public ICommand RunCommand { get { return new DelegateCommand(Run, _ => !IsRunning); } }
    }
}