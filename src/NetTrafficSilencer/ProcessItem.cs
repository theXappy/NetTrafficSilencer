using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NetTrafficSilencer
{
    public class ProcessItem : BaseViewModel
    {
        public string ProcessName { get; set; }
        public int ProcessId { get; set; }
        public string ExecutableName { get; set; }
        public ImageSource Icon { get; set; }
        public ICommand CheckboxClickedCommand { get; set; }

        private bool _isChecked;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
                CheckboxClickedCommand?.Execute(null);
            }
        }

        public ProcessItem(Process process)
        {
            ProcessName = $"{process.ProcessName} (PID: {process.Id})";
            ProcessId = process.Id;
            ExecutableName = process.ProcessName;
            CheckboxClickedCommand = new RelayCommand(OnCheckboxClicked);
        }

        private void OnCheckboxClicked()
        {
            Debug.WriteLine($"Checkbox clicked for process: {ProcessName}, IsChecked: {IsChecked}");
        }

        //private void LoadIcon(Process process)
        //{
        //    Task.Run(() =>
        //    {
        //        try
        //        {
        //            string filePath = process.MainModule?.FileName;
        //            if (!string.IsNullOrEmpty(filePath))
        //            {
        //                Application.Current.Dispatcher.Invoke(() =>
        //                {
        //                    // Use Dispatcher to set the Icon property on the UI thread
        //                    var icon = IconHelper.GetLargeIcon(filePath);
        //                    Icon = icon; 
        //                });
        //            }
        //        }
        //        catch
        //        {
        //            // Ignore access issues
        //        }
        //    });
        //}
    }
}
