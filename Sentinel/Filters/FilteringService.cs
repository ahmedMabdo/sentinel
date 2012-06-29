﻿#region License
//
// © Copyright Ray Hayes
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.
//
#endregion

namespace Sentinel.Filters
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows.Input;

    using ProtoBuf;

    using Sentinel.Filters.Gui;
    using Sentinel.Filters.Interfaces;
    using Sentinel.Interfaces;
    using Sentinel.Support.Mvvm;

    [ProtoContract]
    [DataContract]
    public class FilteringService<T>
        : ViewModelBase
        , IFilteringService<T>
        , IDefaultInitialisation
        where T : class, IFilter
    {
        private readonly CollectionChangeHelper<T> collectionHelper = new CollectionChangeHelper<T>();

        private IAddFilterService addFilterService = new AddFilter();

        private IEditFilterService editFilterService = new EditFilter();

        private IRemoveFilterService removeFilterService = new RemoveFilter();

        private string displayName = "FilteringService";

        private int selectedIndex = -1;

        public FilteringService()
        {
            Add = new DelegateCommand(AddFilter);
            Edit = new DelegateCommand(EditFilter, e => selectedIndex != -1);
            Remove = new DelegateCommand(RemoveFilter, e => selectedIndex != -1);

            Filters = new ObservableCollection<T>();

            // Register self as an observer of the collection.
            collectionHelper.OnPropertyChanged += CustomFilterPropertyChanged;
            collectionHelper.ManagerName = "FilteringService";
            collectionHelper.NameLookup += e => e.Name;
            Filters.CollectionChanged += collectionHelper.AttachDetach;
        }

        public override string DisplayName
        {
            get
            {
                return displayName;
            }
            set
            {
                displayName = value;
            }
        }

        public ICommand Add { get; private set; }

        public ICommand Edit { get; private set; }

        public ICommand Remove { get; private set; }

        public int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }

            set
            {
                if (value != selectedIndex)
                {
                    selectedIndex = value;
                    OnPropertyChanged("SelectedIndex");
                }
            }
        }

        [ProtoAfterDeserialization]
        public void PostLoad()
        {
        }

        [ProtoBeforeSerialization]
        public void PreSave()
        {
        }


        #region IFilteringService Members
        [DataMember]
        [ProtoMember(1)]
        public ObservableCollection<T> Filters { get; set; }

        public bool IsFiltered(LogEntry entry)
        {
            return (Filters.Any(filter => filter.Enabled && filter.IsMatch(entry)));
        }

        #endregion

        private void AddFilter(object obj)
        {
            addFilterService.Add();
        }

        private void CustomFilterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Filter)
            {
                Filter filter = sender as Filter;
                Trace.WriteLine(
                    string.Format(
                        "FilterServer saw some activity on {0} (IsEnabled = {1})",
                        filter.Name,
                        filter.Enabled));
            }

            OnPropertyChanged(string.Empty);
        }

        private void EditFilter(object obj)
        {
            var filter = Filters.ElementAt(SelectedIndex);
            if (filter != null)
            {
                editFilterService.Edit(filter);
            }
        }

        private void RemoveFilter(object obj)
        {
            var filter = Filters.ElementAt(SelectedIndex);
            removeFilterService.Remove(filter);
        }

        public void Initialise()
        {
            // Add the defaulted filters
            Filters.Add(new Filter("Trace", LogEntryField.Type, "TRACE") as T);
            Filters.Add(new Filter("Debug", LogEntryField.Type, "DEBUG") as T);
            Filters.Add(new Filter("Information", LogEntryField.Type, "INFO") as T);
            Filters.Add(new Filter("Warning", LogEntryField.Type, "WARN") as T);
            Filters.Add(new Filter("Error", LogEntryField.Type, "ERROR") as T);
            Filters.Add(new Filter("Fatal", LogEntryField.Type, "FATAL") as T);
        }
    }
}