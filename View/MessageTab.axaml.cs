using DofusHuntHelper.ViewModel;

namespace DofusHuntHelper.View;

public partial class MessageTab : Avalonia.Controls.UserControl
{
    public MessageTab()
    {
        InitializeComponent();
        // On instancie le ViewModel et on le d�finit comme DataContext
        DataContext = new MainViewModel();
    }
}