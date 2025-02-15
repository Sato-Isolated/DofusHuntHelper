using DofusHuntHelper.ViewModel;

namespace DofusHuntHelper.View;

public partial class MessageTab : Avalonia.Controls.UserControl
{
    public MessageTab()
    {
        InitializeComponent();
        // On instancie le ViewModel et on le définit comme DataContext
        DataContext = new MainViewModel();
    }
}