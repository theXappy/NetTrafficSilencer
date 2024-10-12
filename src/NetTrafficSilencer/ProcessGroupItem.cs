using System.Windows.Media;
using System.Collections.ObjectModel;

namespace NetTrafficSilencer
{
    public class ProcessGroupItem : BaseViewModel
    {
        public string ExecutableName { get; set; }
        public string ExecutablePath { get; set; }
        public ImageSource Icon { get; set; }
        public ObservableCollection<ProcessItem> ChildProcesses { get; set; }

        private bool _isChecked;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();

                // Manage the firewall rule based on the IsChecked state
                if (_isChecked)
                {
                    // Create a firewall rule to block outgoing traffic
                    FirewallHelper.AddFirewallRule(ExecutablePath);
                }
                else
                {
                    // Remove the firewall rule
                    FirewallHelper.RemoveFirewallRule(ExecutablePath);
                }
            }
        }

        public ProcessGroupItem(string executableName, ImageSource icon, string executablePath, bool hasFirewallRule)
        {
            ExecutableName = executableName;
            Icon = icon;
            ExecutablePath = executablePath;
            ChildProcesses = new ObservableCollection<ProcessItem>();

            // Set initial state based on existing firewall rules
            _isChecked = hasFirewallRule;
        }
    }
}
