using DofusHuntHelper.ViewModel;

namespace DofusHuntHelper.View;

public partial class SettingsTab : Avalonia.Controls.UserControl
{
    public SettingsTab()
    {
        InitializeComponent();
        // On instancie le ViewModel et on le définit comme DataContext
        DataContext = new MainViewModel();
    }
}