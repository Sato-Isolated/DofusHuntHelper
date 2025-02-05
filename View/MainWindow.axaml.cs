using DofusHuntHelper.ViewModel;
using SukiUI.Controls;

namespace DofusHuntHelper.View;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();
        // On instancie le ViewModel et on le d�finit comme DataContext
        DataContext = new MainViewModel();
    }
}