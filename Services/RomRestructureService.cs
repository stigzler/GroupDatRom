using GroupDatRom.Models;
using GroupDatRom.Models.Presentation;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GroupDatRom.Services
{
    internal class RomRestructureService
    {
        public async Task ExecuteRestructureAsync(FileObjectNode previewRoot, string romDirPath, IProgress<ProgressReport> progress = null, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(romDirPath) || !Directory.Exists(romDirPath))
                    throw new DirectoryNotFoundException("ROM directory does not exist.");

                // 1. Count total items to process for accurate percentage calculation
                int totalItems = 0;
                foreach (var child in previewRoot.Children)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (child.Type == NodeType.FolderNew && child.Selected)
                    {
                        totalItems += child.Children.Count;
                    }
                    else if (child.Type == NodeType.FileM3u && child.Selected)
                    {
                        totalItems++;
                    }
                }

                if (totalItems == 0)
                {
                    progress?.Report(new ProgressReport { Message = "No items selected for restructure.", Percentage = 100 });
                    return;
                }

                int processedItems = 0;

                // 2. Process items and report progress live
                foreach (var child in previewRoot.Children)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (child.Type == NodeType.FolderNew && child.Selected)
                    {
                        // Create the new game folder directly in the main ROM directory
                        string targetGameFolder = Path.Combine(romDirPath, child.Name);
                        Directory.CreateDirectory(targetGameFolder);

                        foreach (var item in child.Children)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            processedItems++;

                            int percent = (int)((double)processedItems / totalItems * 100);
                            progress?.Report(new ProgressReport
                            {
                                Message = $"Moving File: {item.Name}",
                                Percentage = Math.Clamp(percent, 1, 100)
                            });

                            ProcessNode(item, targetGameFolder);
                        }
                    }
                    else if (child.Type == NodeType.FileM3u && child.Selected)
                    {
                        processedItems++;
                        int percent = (int)((double)processedItems / totalItems * 100);

                        progress?.Report(new ProgressReport
                        {
                            Message = $"Creating playlist: {child.Name}",
                            Percentage = Math.Clamp(percent, 1, 100)
                        });

                        string targetFilePath = Path.Combine(romDirPath, child.Name);
                        if (child.M3uLines != null)
                        {
                            File.WriteAllLines(targetFilePath, child.M3uLines);
                        }
                    }
                }
            }, cancellationToken);
        }

        private void ProcessNode(FileObjectNode node, string targetDirectory)
        {
            if (node.Type == NodeType.FileM3u && node.M3uLines != null)
            {
                string m3uPath = Path.Combine(targetDirectory, node.Name);
                File.WriteAllLines(m3uPath, node.M3uLines);
                return;
            }

            if (!string.IsNullOrEmpty(node.SourceFullPath) && File.Exists(node.SourceFullPath))
            {
                string destPath = Path.Combine(targetDirectory, node.Name);

                // Move file into the new game folder (skip if source and destination are identical)
                if (!string.Equals(node.SourceFullPath, destPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Move(node.SourceFullPath, destPath, true);
                }
            }
        }
    }
}