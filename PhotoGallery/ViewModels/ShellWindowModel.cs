using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using FileAndFolderDialog.Abstractions;
using Prism.Commands;
using Prism.Mvvm;

namespace PhotoGallery.ViewModels;

public class ShellWindowModel : BindableBase
{
    private readonly IFolderDialogService folderDialogService;

    private FileSystemWatcher watcher;
    private string selectedItem;
    private ObservableCollection<string> fileList = new ObservableCollection<string>();
    private ListView fileListView;

    public string SelectedItem
    {
        get => selectedItem; set
        {
            SetProperty(ref selectedItem, value);
        }
    }

    public ObservableCollection<string> FileList { get => fileList; set => SetProperty(ref fileList, value); }

    public DelegateCommand<Window> LoadedCommand { get; private set; }

    public DelegateCommand<CancelEventArgs> ClosingWindowCommand { get; private set; }
    public DelegateCommand DeleteCommand { get; private set; }
    public DelegateCommand OpenFolderCommand { get; private set; }
    public DelegateCommand<KeyEventArgs> KeyDownCommand { get; private set; }

    public ShellWindowModel(IFolderDialogService folderDialogService)
    {
        this.folderDialogService = folderDialogService;

        LoadedCommand = new DelegateCommand<Window>((window) =>
        {
            var folderPath = Environment.CurrentDirectory;

            watcher = new FileSystemWatcher();
            watcher.Path = folderPath;
            watcher.Created += FileCreated;
            watcher.Deleted += FileDeleted;
            watcher.EnableRaisingEvents = true;

            OpenFolderImages(folderPath);

            fileListView = window.FindChild<ListView>("FileListView");
        });

        ClosingWindowCommand = new DelegateCommand<CancelEventArgs>((args) =>
        {
            args.Cancel = true;

            Application.Current.Shutdown();
        });

        OpenFolderCommand = new DelegateCommand(() =>
        {
            var options = new SelectFolderOptions()
            {
                ShowNewFolderButton = false,
                //SelectedPath = Environment.CurrentDirectory
                RootFolder = Environment.SpecialFolder.Recent
            };

            var result = folderDialogService.ShowSelectFolderDialog(options);
            if (!string.IsNullOrEmpty(result))
            {
                OpenFolderImages(result);

                fileListView.Focus();
            }
        });

        DeleteCommand = new DelegateCommand(RemoveItem);

        KeyDownCommand = new DelegateCommand<KeyEventArgs>((e) =>
        {
            e.Handled = true;
            if (e.Key == Key.Delete)
            {
                RemoveItem();
            }
            else if (e.Key == Key.Down)
            {
                SetSelectedItem(SelectedItem, Position.Next);
            }
            else if (e.Key == Key.Up)
            {
                SetSelectedItem(SelectedItem, Position.Prev);
            }
        });
    }

    private void OpenFolderImages(string folderPath)
    {
        var directoryInfo = new DirectoryInfo(folderPath);

        var files = directoryInfo.GetFiles()
            .Where(f => f.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) || f.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.CreationTime)
            .Select(f => f.FullName)
            .ToArray();
        FileList = new ObservableCollection<string>(files);

        SetSelectedItem(string.Empty, Position.First);
    }

    private void FileCreated(object sender, FileSystemEventArgs e)
    {
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(async () =>
        {
            await Task.Delay(1000);
            FileList.Add(e.FullPath.Replace(".tmp", ".png"));

            if (string.IsNullOrWhiteSpace(SelectedItem))
            {
                SetSelectedItem(FileList.FirstOrDefault());
            }

            Debug.WriteLine("File created: " + e.FullPath);
        }));
    }

    private void FileDeleted(object sender, FileSystemEventArgs e)
    {
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            _ = FileList.Remove(e.FullPath);

            Debug.WriteLine("File deleted: " + e.FullPath);
        }));
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        RemoveItem();
    }

    private void RemoveItem()
    {
        try
        {
            var isLast = false;
            var removeItem = SelectedItem;

            if (SelectedItem == FileList.LastOrDefault())
            {
                Debug.WriteLine("islast");
                isLast = true;
            }

            if (isLast)
            {
                SetSelectedItem(SelectedItem, Position.Prev);
            }
            else
            {
                SetSelectedItem(SelectedItem, Position.Next);
            }

            File.Delete(removeItem);

            fileListView.Focus();
        }
        catch
        {
            Debug.WriteLine("catch");
        }
    }

    private void SetSelectedItem(string item, Position position = Position.Current)
    {
        var index = FileList.IndexOf(item);

        switch (position)
        {
            case Position.Current:
                SelectedItem = item;
                break;

            case Position.Next:
                if (index + 1 < FileList.Count)
                {
                    SelectedItem = FileList.ElementAt(index + 1);
                }
                else
                {
                    SelectedItem = FileList.LastOrDefault();
                }
                break;

            case Position.Prev:
                if (index - 1 > 0)
                {
                    SelectedItem = FileList.ElementAt(index - 1);
                }
                else
                {
                    SelectedItem = FileList.FirstOrDefault();
                }
                break;

            case Position.First:
                SelectedItem = FileList.FirstOrDefault();
                break;

            default:
                SelectedItem = string.Empty;
                break;
        }

        if (!string.IsNullOrEmpty(SelectedItem))
        {
            fileListView.ScrollIntoView(SelectedItem);
        }
    }
}

public enum Position
{
    First,
    Current,
    Next,
    Prev
}