using System.Windows;

namespace GraphEditor
{
    public partial class WeightInputWindow : Window
    {
        public double? Weight { get; private set; }

        public WeightInputWindow()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(WeightTextBox.Text, out double result))
            {
                Weight = result;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Введите корректное число.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
