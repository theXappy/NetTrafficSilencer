using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NetTrafficSilencer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the timer to reset the search string after a short pause in typing
            _resetTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            _resetTimer.Tick += (s, e) => _searchBuilder.Clear(); // Reset search string after 1.5 seconds

        }


        private readonly StringBuilder _searchBuilder = new StringBuilder();
        private readonly DispatcherTimer _resetTimer;
        // Handles the PreviewKeyDown event for the TreeView to perform search
        private void TreeView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Check if the input is a printable character
            if (e.Key >= Key.A && e.Key <= Key.Z || e.Key >= Key.D0 && e.Key <= Key.D9 || e.Key == Key.Space)
            {
                // Convert the Key to a character and append to the search string
                string key = e.Key.ToString().Replace("D", ""); // Remove "D" prefix for numbers
                if (e.Key == Key.Space)
                {
                    key = " ";
                }
                _searchBuilder.Append(key.ToLower());

                // Restart the timer to clear the search string after a short pause
                _resetTimer.Stop();
                _resetTimer.Start();

                // Perform the search in the TreeView
                SearchAndSelectInTreeView(sender as TreeView, _searchBuilder.ToString());
            }
        }

        // Method to search for and select the next matching element in the TreeView
        private void SearchAndSelectInTreeView(TreeView treeView, string searchText)
        {
            if (treeView == null || string.IsNullOrEmpty(searchText)) return;

            // Flatten the tree view items to find matches
            var allItems = treeView.Items.Cast<object>().SelectMany(GetAllItems);

            // Find the first item whose ExecutableName starts with the search text (case-insensitive)
            var matchingItem = allItems
                .OfType<ProcessGroupItem>()
                .FirstOrDefault(item => item.ExecutableName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase));

            if (matchingItem != null)
            {
                // Find the corresponding TreeViewItem and select it
                var itemContainer = (TreeViewItem)treeView.ItemContainerGenerator.ContainerFromItem(matchingItem);
                if (itemContainer != null)
                {
                    itemContainer.IsSelected = true;
                    itemContainer.BringIntoView();
                    itemContainer.Focus();
                }
            }
        }

        // Recursively gets all items in the tree (including nested child items)
        private static IEnumerable<object> GetAllItems(object item)
        {
            if (item is TreeViewItem treeViewItem && treeViewItem.HasItems)
            {
                foreach (var childItem in treeViewItem.Items)
                {
                    yield return childItem;
                    foreach (var nestedChild in GetAllItems(childItem))
                    {
                        yield return nestedChild;
                    }
                }
            }
            else if (item is ProcessGroupItem groupItem)
            {
                yield return groupItem;
                foreach (var child in groupItem.ChildProcesses)
                {
                    yield return child;
                }
            }
        }

        // Event handler to update the ViewModel's SelectedProcessGroup property
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is ProcessViewModel viewModel)
            {
                // Update the SelectedProcessGroup property in the ViewModel
                viewModel.SelectedProcessGroup = e.NewValue;
            }
        }
    }
}