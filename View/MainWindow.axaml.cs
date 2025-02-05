using DofusHuntHelper.ViewModel;
using SukiUI.Controls;

namespace DofusHuntHelper.View;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();
        // On instancie le ViewModel et on le définit comme DataContext
        DataContext = new MainViewModel();
    }
}