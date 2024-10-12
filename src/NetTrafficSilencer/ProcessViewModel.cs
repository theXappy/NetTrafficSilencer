using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace NetTrafficSilencer
{
    public class ProcessViewModel : BaseViewModel
    {
        private string _filterText;
        private object _selectedProcessGroup;

        public ObservableCollection<ProcessGroupItem> ProcessGroups { get; set; }
        public ICollectionView FilteredProcessGroups { get; set; }
        private Timer _refreshTimer;

        // Command to handle the "Remove All Rules" button click
        public ICommand RemoveAllRulesCommand { get; }

        // Property to store the filter text
        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                OnPropertyChanged();
                FilteredProcessGroups.Refresh(); // Refresh the filtered view whenever the filter text changes
            }
        }

        // Property to track the selected item in the TreeView
        public object SelectedProcessGroup
        {
            get => _selectedProcessGroup;
            set
            {
                _selectedProcessGroup = value;
                OnPropertyChanged();
            }
        }

        public ProcessViewModel()
        {
            ProcessGroups = new ObservableCollection<ProcessGroupItem>();
            FilteredProcessGroups = CollectionViewSource.GetDefaultView(ProcessGroups);
            FilteredProcessGroups.Filter = FilterProcessGroups;

            LoadProcessesAsync(true);

            // Set up the timer to refresh the process list every 5 seconds
            _refreshTimer = new Timer(state => LoadProcessesAsync(false), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            // Initialize the Remove All Rules command
            RemoveAllRulesCommand = new RelayCommand(RemoveAllFirewallRules);
        }

        // Unified method to load or refresh processes
        private async void LoadProcessesAsync(bool isInitialLoad)
        {
            FirewallHelper.LoadFirewallRules(); // Load firewall rules once at the beginning if necessary

            var processGroups = new ConcurrentDictionary<string, ProcessGroupItem>();

            await Task.Run(() =>
            {
                var processes = Process.GetProcesses();
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                Parallel.ForEach(processes, parallelOptions, process =>
                {
                    HandleSingleProcess(process, processGroups);
                });
            });

            // Preserve the currently selected item
            var previouslySelectedItem = SelectedProcessGroup as ProcessGroupItem;

            // Update the UI based on whether it's the initial load or a refresh
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isInitialLoad)
                {
                    // Set the ProcessGroups collection on initial load
                    ProcessGroups = new ObservableCollection<ProcessGroupItem>(processGroups.Values.OrderBy(g => g.ExecutableName));
                    FilteredProcessGroups = CollectionViewSource.GetDefaultView(ProcessGroups);
                    FilteredProcessGroups.Filter = FilterProcessGroups;
                }
                else
                {
                    // Merge the changes into the existing ProcessGroups collection during refresh
                    UpdateProcessGroups(processGroups);
                }

                FilteredProcessGroups.Refresh(); // Refresh the filtered view after updating the collection

                // Attempt to reselect the previously selected item if it still exists
                if (previouslySelectedItem != null)
                {
                    var reselectedItem = ProcessGroups.FirstOrDefault(group => group.ExecutableName == previouslySelectedItem.ExecutableName);
                    if (reselectedItem != null)
                    {
                        SelectedProcessGroup = reselectedItem;
                    }
                }

                OnPropertyChanged(nameof(FilteredProcessGroups));
            });
        }

        // Method to handle a single process and update the process groups
        private void HandleSingleProcess(Process process, ConcurrentDictionary<string, ProcessGroupItem> processGroups)
        {
            try
            {
                if (ProcessAccessHelper.CanAccessProcess(process))
                {
                    var processItem = new ProcessItem(process);
                    string executableName = process.ProcessName;

                    // Retrieve the executable path for this process
                    string executablePath = null;
                    ImageSource groupIcon = null;

                    try
                    {
                        executablePath = ProcessAccessHelper.GetExecutablePath(process);
                    }
                    catch (System.ComponentModel.Win32Exception) // Catch "Access Denied" errors
                    {
                        executablePath = null;
                    }
                    catch (InvalidOperationException) // Catch if process exited
                    {
                        executablePath = null;
                    }

                    // Check firewall rule status using the preloaded cache
                    bool hasFirewallRule = !string.IsNullOrEmpty(executablePath) && FirewallHelper.RuleExists(executablePath);

                    // Get the icon for the group if it's the first process of that executable
                    if (!processGroups.ContainsKey(executableName))
                    {
                        groupIcon = Application.Current.Dispatcher.Invoke(() =>
                        {
                            return !string.IsNullOrEmpty(executablePath) ? IconHelper.GetLargeIcon(executablePath) : IconHelper.GetDefaultExeIcon();
                        });
                    }

                    // Add process to its group with the correct executable path and firewall status
                    var group = processGroups.GetOrAdd(executableName, new ProcessGroupItem(executableName, groupIcon, executablePath ?? string.Empty, hasFirewallRule));
                    group.ChildProcesses.Add(processItem);
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Handle specific Win32 exceptions such as access denied or partial reads
                Debug.WriteLine($"Win32Exception for process {process.ProcessName}: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                // Handle cases where the process has exited
                Debug.WriteLine($"InvalidOperationException for process {process.ProcessName}: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Log any other unexpected exception
                Debug.WriteLine($"Exception for process {process.ProcessName}: {ex.Message}");
            }
        }

        // Method to update the existing ProcessGroups collection based on new data
        private void UpdateProcessGroups(ConcurrentDictionary<string, ProcessGroupItem> updatedGroups)
        {
            // Remove groups that no longer exist in the updated list
            var groupsToRemove = ProcessGroups.Where(g => !updatedGroups.ContainsKey(g.ExecutableName)).ToList();
            foreach (var group in groupsToRemove)
            {
                ProcessGroups.Remove(group);
            }

            // Add or update groups that are new or modified
            foreach (var updatedGroup in updatedGroups)
            {
                var existingGroup = ProcessGroups.FirstOrDefault(g => g.ExecutableName == updatedGroup.Key);

                if (existingGroup == null)
                {
                    // Add new group
                    ProcessGroups.Add(updatedGroup.Value);
                }
                else
                {
                    // Update existing group with new processes
                    existingGroup.ChildProcesses.Clear();
                    foreach (var child in updatedGroup.Value.ChildProcesses)
                    {
                        existingGroup.ChildProcesses.Add(child);
                    }
                }
            }
        }

        // Filter method to filter process groups based on the FilterText property
        private bool FilterProcessGroups(object item)
        {
            if (item is ProcessGroupItem groupItem)
            {
                // Check if the group's executable name contains the filter text (case-insensitive)
                return string.IsNullOrEmpty(FilterText) || groupItem.ExecutableName.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return true;
        }

        // Method to remove all firewall rules created by the application
        private void RemoveAllFirewallRules()
        {
            // Update the IsChecked state of all ProcessGroupItems. This triggers a removal of previously set rules.
            foreach (var group in ProcessGroups)
            {
                if(group.IsChecked)
                    group.IsChecked = false;
            }
        }
    }
}