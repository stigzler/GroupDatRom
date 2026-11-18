using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GroupDatRom.Mappers;
using GroupDatRom.Models;
using GroupDatRom.Services;
using iNKORE.UI.WPF.Modern;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using GroupDatRom.Models.Presentation;

namespace GroupDatRom.ViewModels
{
    internal partial class MainVM: ObservableObject
    {
        [ObservableProperty]
        private bool isDarkMode = true;

        [ObservableProperty]
        private string romDirPath = string.Empty;

        [ObservableProperty]
        private FileObjectNode? scanResults;

        [ObservableProperty]
        private FileObjectNode? previewResults;

        [ObservableProperty]
        private int operationProgressPercentage;

        [ObservableProperty]
        private string operationPrimaryText;

        [ObservableProperty]
        private string operationSecondaryText;

        [ObservableProperty]
        private bool produceM3u = true;

        [ObservableProperty]
        private string filePreviewText;

        [ObservableProperty]
        private string playlistFilename;

        private readonly RomDirectoryScanner _romDirectoryScanner = new();
        private CancellationTokenSource _cts = new();

        public MainVM()
        {
            IsDarkMode = true;

            // 1. Initialize JSON mapping files (writes defaults if missing)
            FileTypeMapper.Initialize();
            MediaTypeMapper.Initialize();

            // 2. Restore last used directory from settings on startup
            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastRomDirPath))
            {
                RomDirPath = Properties.Settings.Default.LastRomDirPath;
            }
        }

        [RelayCommand]
        private void OnSelectedNodeChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            if (e is null || e.NewValue is null) return;
            //if (e.NewValue is FileObjectNode selectedNode == false) return;
            FileObjectNode selectedNode = (FileObjectNode)e.NewValue;
            if (selectedNode.Type == NodeType.FileM3u && selectedNode.M3uLines != null)
            {
                FilePreviewText = String.Join(Environment.NewLine, selectedNode.M3uLines);
                PlaylistFilename = PriorityCuesheetFilename(selectedNode);
            }
            else if (selectedNode.Type == NodeType.FileCue && selectedNode.CueLines != null)
            {
                FilePreviewText = String.Join(Environment.NewLine, selectedNode.CueLines);
                PlaylistFilename = PriorityCuesheetFilename(selectedNode);
            }
            else
            {
                FilePreviewText = string.Empty;
                PlaylistFilename = string.Empty;
            }    
        }

        private string PriorityCuesheetFilename(FileObjectNode selectedNode)
        {
            if (!string.IsNullOrEmpty(selectedNode.SourceFullPath)) return Path.GetFileName(selectedNode.SourceFullPath);
            else if (!string.IsNullOrEmpty(selectedNode.DestinationFullPath)) return Path.GetFileName(selectedNode.DestinationFullPath);
            else return "unknown"; // this should never fire
        }

        [RelayCommand]
        private void OpenRomFolder()
        {
            if (!Directory.Exists(RomDirPath)) return;

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = RomDirPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void CollapseTreeRecursive(System.Windows.Controls.ItemContainerGenerator generator)
        {
            if (generator == null) return;

            for (int i = 0; i < generator.Items.Count; i++)
            {
                if (generator.ContainerFromIndex(i) is System.Windows.Controls.TreeViewItem container)
                {
                    container.IsExpanded = false;
                    if (container.HasItems)
                    {
                        CollapseTreeRecursive(container.ItemContainerGenerator);
                    }
                }
            }
        }

        [RelayCommand]
        private void CollapseScanTree(object parameter)
        {
            if (parameter is System.Windows.Controls.TreeView treeView)
            {
                CollapseTreeRecursive(treeView.ItemContainerGenerator);
            }
        }

        [RelayCommand]
        private void CollapsePreviewTree(object parameter)
        {
            if (parameter is System.Windows.Controls.TreeView treeView)
            {
                CollapseTreeRecursive(treeView.ItemContainerGenerator);
            }
        }

        private void ClearResults()
        {
            ScanResults?.Children.Clear();
            PreviewResults?.Children.Clear();
        }

        internal void OnViewClosing()
        {
            Properties.Settings.Default.LastRomDirPath = RomDirPath;
        }

        [RelayCommand]
        public void SetRomDirViaFolderBrowser()
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog()
            {  Title = "Please select the directory containing a Platform's roms"};

            var result = openFolderDialog.ShowDialog();

            if (result != true) return;

            RomDirPath = openFolderDialog.FolderName;

            ClearResults();
        }

        partial void OnIsDarkModeChanged(bool value)
        {
            if (value == true)
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            else
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
        }

        [RelayCommand]
        private async Task ScanFolderAsync()
        {
            if (string.IsNullOrWhiteSpace(RomDirPath) || !Directory.Exists(RomDirPath))
            {
                return;
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            try
            {
                var progress = new Progress<ProgressReport>(report =>
                {
                    if (report.Percentage > 0)
                    {
                        OperationProgressPercentage = report.Percentage;
                    }

                    if (!string.IsNullOrEmpty(report.Message))
                    {
                        OperationSecondaryText = report.Message;
                    }
                });

                OperationPrimaryText = "Scanning Rom Folder: ";
                OperationProgressPercentage = 0;
                OperationSecondaryText = "Initializing scan...";

                ScanResults = await _romDirectoryScanner.BuildScanResultsTreeAsync(RomDirPath, progress, _cts.Token);

                OperationPrimaryText = "Building Preview: ";
                OperationSecondaryText = "Initialising Build...";

                PreviewResults = await _romDirectoryScanner.BuildPreviewResultsTreeAsync(ScanResults, true, progress, _cts.Token);

                OperationPrimaryText = "Scan Successful";
                OperationSecondaryText = string.Empty;

            }
            catch (OperationCanceledException)
            {
                OperationPrimaryText = "Cancelled";
                OperationSecondaryText = "Scan cancelled.";
                OperationProgressPercentage = 0;
            }
            catch (Exception ex)
            {
                OperationPrimaryText = "Error";
                OperationSecondaryText = ex.Message;
                OperationProgressPercentage = 0;
            }
        }
    }
}
