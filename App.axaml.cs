using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DofusHuntHelper.View;
using Application = Avalonia.Application;

namespace DofusHuntHelper;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}