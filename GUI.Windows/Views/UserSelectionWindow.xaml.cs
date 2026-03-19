using System.Windows;
using Core.Models;

namespace GUI.Windows.Views;

/// <summary>
/// Finestra di selezione utente all'avvio dell'applicazione.
/// </summary>
public partial class UserSelectionWindow : Window
{
    /// <summary>
    /// Utente selezionato dall'utente.
    /// </summary>
    public User? SelectedUser { get; private set; }

    public UserSelectionWindow(IReadOnlyList<User> users)
    {
        InitializeComponent();
        UserComboBox.ItemsSource = users;
    }

    private void UserComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ConfirmButton.IsEnabled = UserComboBox.SelectedItem is not null;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedUser = UserComboBox.SelectedItem as User;
        DialogResult = SelectedUser is not null;
    }
}
