using System.Windows;
using System.Windows.Controls;

namespace NetTrafficSilencer
{
    public static class TreeViewHelper
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItem",
                typeof(object),
                typeof(TreeViewHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        public static object GetSelectedItem(DependencyObject obj)
        {
            return (object)obj.GetValue(SelectedItemProperty);
        }

        public static void SetSelectedItem(DependencyObject obj, object value)
        {
            obj.SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeView treeView)
            {
                treeView.SelectedItemChanged -= TreeView_SelectedItemChanged;
                treeView.SelectedItemChanged += TreeView_SelectedItemChanged;

                if (e.NewValue is TreeViewItem treeViewItem)
                {
                    treeViewItem.IsSelected = true;
                }
                else
                {
                    // Try to find and select the new value in the TreeView
                    SelectTreeViewItem(treeView, e.NewValue);
                }
            }
        }

        private static void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is TreeView treeView)
            {
                SetSelectedItem(treeView, e.NewValue);
            }
        }

        private static void SelectTreeViewItem(TreeView treeView, object selectedItem)
        {
            if (selectedItem == null) return;

            // Expand and select the corresponding TreeViewItem
            var container = GetTreeViewItem(treeView, selectedItem);
            if (container != null)
            {
                container.IsSelected = true;
                container.BringIntoView();
            }
        }

        private static TreeViewItem GetTreeViewItem(ItemsControl container, object item)
        {
            if (container == null) return null;

            if (container.DataContext == item)
                return container as TreeViewItem;

            container.ApplyTemplate();
            var itemsPresenter = container.Template.FindName("ItemsHost", container) as ItemsPresenter;
            if (itemsPresenter != null)
                itemsPresenter.ApplyTemplate();

            for (int i = 0; i < container.Items.Count; i++)
            {
                var subContainer = (TreeViewItem)container.ItemContainerGenerator.ContainerFromIndex(i);
                if (subContainer == null)
                {
                    subContainer = (TreeViewItem)container.ItemContainerGenerator.ContainerFromItem(container.Items[i]);
                }

                var resultContainer = GetTreeViewItem(subContainer, item);
                if (resultContainer != null)
                    return resultContainer;
            }

            return null;
        }
    }
}