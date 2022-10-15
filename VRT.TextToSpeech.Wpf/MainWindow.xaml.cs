using Microsoft.Win32;
using System.Windows;

namespace VRT.TextToSpeech.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel();
            DataContext = ViewModel;
        }

        internal MainWindowViewModel ViewModel { get; }

        private void OnBrowseOutputFileButton(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Title = "Select output wav file",
                Filter = "Wave file|.wav",
                AddExtension = true,
                OverwritePrompt = true,
                CheckPathExists = true
            };
            if(dialog.ShowDialog().GetValueOrDefault())
            {
                ViewModel.Options.OutputFileName = dialog.FileName;
            }
        }
    }
}
