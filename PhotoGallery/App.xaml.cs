using System;
using System.Reflection;
using System.Windows;
using DryIoc;
using FileAndFolderDialog.Abstractions;
using FileAndFolderDialog.Wpf;
using PhotoGallery.Views;
using Prism.Ioc;
using Prism.Mvvm;

namespace PhotoGallery;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    protected override Window CreateShell()
    {
        var applicationWindow = Container.Resolve<ShellWindow>();
        return applicationWindow;
    }

    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();

        ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver((viewType) =>
        {
            var viewName = viewType.FullName;
            var viewAssemblyName = viewType.GetTypeInfo().Assembly.FullName;
            var viewModelName = $"{viewName.Replace(".Views.", ".ViewModels.")}Model, {viewAssemblyName}";
            return Type.GetType(viewModelName);
        });
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterScoped<IFolderDialogService, FolderDialogService>();
    }
}