using System.Windows;

namespace CourseWork_CSaN
{
    /// <summary>
    /// Логика взаимодействия для DialogWithTextBox.xaml
    /// </summary>
    public partial class DialogWithTextBox : Window
    {
        public string NewName { get => tbNewName.Text.Trim(); }

        public DialogWithTextBox()
        {
            InitializeComponent();
        }

        private void ButtonAcceptClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
