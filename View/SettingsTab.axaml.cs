using DofusHuntHelper.ViewModel;

namespace DofusHuntHelper.View;

public partial class SettingsTab : Avalonia.Controls.UserControl
{
    public SettingsTab()
    {
        InitializeComponent();
        // On instancie le ViewModel et on le d�finit comme DataContext
        DataContext = new MainViewModel();
    }
}