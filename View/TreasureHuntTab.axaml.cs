using DofusHuntHelper.ViewModel;

namespace DofusHuntHelper.View;

public partial class TreasureHuntTab : Avalonia.Controls.UserControl
{
    public TreasureHuntTab()
    {
        InitializeComponent();
        // On instancie le ViewModel et on le définit comme DataContext
        DataContext = new MainViewModel();
    }
}