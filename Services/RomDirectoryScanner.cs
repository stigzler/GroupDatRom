using GroupDatRom.Mappers;
using GroupDatRom.Models;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpSevenZip;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProgressReport = GroupDatRom.Models.ProgressReport;
using GroupDatRom.Extensions;
using GroupDatRom.Models.Presentation;


namespace GroupDatRom.Services
{
    internal class RomDirectoryScanner
    {
        private static bool _isSevenZipInitialized = false;

        public async Task<FileObjectNode> BuildScanResultsTreeAsync(string rootDirectoryPath, IProgress<ProgressReport> progress, CancellationToken cancellationToken)
        {
            EnsureSevenZipDllInitialized();

            return await Task.Run(() =>
            {
                var rootDirInfo = new DirectoryInfo(rootDirectoryPath);
                if (!rootDirInfo.Exists)
                {
                    throw new DirectoryNotFoundException($"Directory not found: {rootDirectoryPath}");
                }

                var rootNode = new FileObjectNode
                {
                    Name = rootDirInfo.Name,
                    Type = NodeType.Root,
                    SourceFullPath = rootDirInfo.FullName,
                    DestinationFullPath = rootDirInfo.FullName
                };

                var archiveQueue = new ConcurrentBag<(string FilePath, FileObjectNode Node)>();
                int discoveredCount = 0;

                progress?.Report(new ProgressReport { Message = "Scanning directory structure...", Percentage = 0 });
                ScanSinglePass(rootDirInfo, rootNode, archiveQueue, ref discoveredCount, progress, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                var archivesList = archiveQueue.ToList();
                int totalArchives = archivesList.Count;

                if (totalArchives > 0)
                {
                    int inspectedCount = 0;
                    object lockObj = new object();

                    Parallel.ForEach(archivesList, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken }, item =>
                    {
                        InspectArchive(item.FilePath, item.Node);

                        int currentCount;
                        lock (lockObj)
                        {
                            inspectedCount++;
                            currentCount = inspectedCount;
                        }

                        int percent = (int)((double)currentCount / totalArchives * 100);
                        progress?.Report(new ProgressReport
                        {
                            Message = $"Inspecting archive ({currentCount}/{totalArchives}): {Path.GetFileName(item.FilePath)}",
                            Percentage = Math.Clamp(percent, 1, 100)
                        });
                    });
                }

                progress?.Report(new ProgressReport { Message = "Scan complete.", Percentage = 100 });
                SortNodeRecursive(rootNode);
                return rootNode;
            }, cancellationToken);
        }

        private static void EnsureSevenZipDllInitialized()
        {
            if (_isSevenZipInitialized) return;

            try
            {
                string archFolder = Environment.Is64BitProcess ? "x64" : "x86";
                string dllPath = Path.Combine(AppContext.BaseDirectory, archFolder, "7z.dll");

                if (!File.Exists(dllPath))
                {
                    dllPath = Path.Combine(AppContext.BaseDirectory, "7z.dll");
                }

                if (File.Exists(dllPath))
                {
                    SharpSevenZipBase.SetLibraryPath(dllPath);
                }
                _isSevenZipInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize 7z.dll path: {ex.Message}");
            }
        }

        private void ScanSinglePass(DirectoryInfo currentDir, FileObjectNode parentNode, ConcurrentBag<(string, FileObjectNode)> archiveQueue, ref int fileCount, IProgress<ProgressReport> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                foreach (var fileInfo in currentDir.EnumerateFiles())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    fileCount++;
                    NodeType fileNodeType = FileTypeMapper.GetNodeType(fileInfo.Extension);

                    var fileNode = new FileObjectNode
                    {
                        Name = fileInfo.Name,
                        Type = fileNodeType,
                        SourceFullPath = fileInfo.FullName,
                        DestinationFullPath = fileInfo.FullName,
                        Parent = parentNode // <--- Assign parent link
                    };

                    // If it's an existing .m3u file found on disk, read its lines into the model
                    if (fileNodeType == NodeType.FileM3u)
                    {
                        try
                        {
                            if (File.Exists(fileInfo.FullName))
                            {
                                fileNode.M3uLines = File.ReadAllLines(fileInfo.FullName).ToList();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to read existing M3U {fileInfo.FullName}: {ex.Message}");
                            fileNode.M3uLines = new List<string>();
                        }
                    }

                    // If it's an existing .cue file found on disk, read its lines into the model
                    if (fileNodeType == NodeType.FileCue)
                    {
                        try
                        {
                            if (File.Exists(fileInfo.FullName))
                            {
                                fileNode.CueLines = File.ReadAllLines(fileInfo.FullName).ToList();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to read CUE {fileInfo.FullName}: {ex.Message}");
                            fileNode.CueLines = new List<string>();
                        }
                    }

                    if (fileNodeType == NodeType.Archive)
                    {
                        archiveQueue.Add((fileInfo.FullName, fileNode));
                    }

                    if (fileCount % 10 == 0)
                    {
                        progress?.Report(new ProgressReport { Message = $"Discovered {fileCount} files: {fileInfo.Name}", Percentage = 0 });
                    }

                    parentNode.Children.Add(fileNode);
                }

                foreach (var subDirInfo in currentDir.EnumerateDirectories())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    bool hasContents = subDirInfo.EnumerateFileSystemInfos().Any();
                    NodeType folderType = hasContents ? NodeType.FolderFull : NodeType.FolderEmpty;

                    var folderNode = new FileObjectNode
                    {
                        Name = subDirInfo.Name,
                        Type = folderType,
                        SourceFullPath = subDirInfo.FullName,
                        DestinationFullPath = subDirInfo.FullName,
                        Parent = parentNode // <--- Assign parent link
                    };

                    parentNode.Children.Add(folderNode);
                    ScanSinglePass(subDirInfo, folderNode, archiveQueue, ref fileCount, progress, cancellationToken);
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        public async Task<FileObjectNode> BuildPreviewResultsTreeAsync(FileObjectNode scanResultsRoot, bool buildM3u = false, IProgress<ProgressReport> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                progress?.Report(new ProgressReport { Message = "Building preview grouping...", Percentage = 0 });

                var previewRoot = new FileObjectNode
                {
                    Name = scanResultsRoot.Name,
                    Type = NodeType.Root,
                    SourceFullPath = scanResultsRoot.SourceFullPath,
                    DestinationFullPath = scanResultsRoot.DestinationFullPath,
                    Parent = null
                };

                // Map group names to all items belonging to that group
                var potentialGroups = new Dictionary<string, List<FileObjectNode>>(StringComparer.OrdinalIgnoreCase);
                var looseFiles = new List<FileObjectNode>();

                foreach (var child in scanResultsRoot.Children)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (child.Type == NodeType.FolderFull || child.Type == NodeType.FolderEmpty)
                    {
                        previewRoot.Children.Add(CloneNodeStructure(child, previewRoot));
                        continue;
                    }

                    string fileNameNoExt = Path.GetFileNameWithoutExtension(child.Name);

                    if (MediaTypeMapper.TryGetMultiMediaGroupName(fileNameNoExt, out string groupName))
                    {
                        if (!potentialGroups.TryGetValue(groupName, out var groupList))
                        {
                            groupList = new List<FileObjectNode>();
                            potentialGroups[groupName] = groupList;
                        }
                        groupList.Add(child);
                    }
                    else
                    {
                        looseFiles.Add(child);
                    }
                }

                // Also check loose files to see if any match our potential group names (e.g. companion files / manuals / .cue files)
                for (int i = looseFiles.Count - 1; i >= 0; i--)
                {
                    var looseFile = looseFiles[i];
                    string looseNameNoExt = Path.GetFileNameWithoutExtension(looseFile.Name);

                    if (potentialGroups.ContainsKey(looseNameNoExt))
                    {
                        potentialGroups[looseNameNoExt].Add(looseFile);
                        looseFiles.RemoveAt(i);
                    }
                }

                // CRITICAL FIX: Only create group folders for items that ACTUALLY have 2+ files or require grouping. Single files stay loose!
                var activeGroupFolders = new Dictionary<string, FileObjectNode>(StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in potentialGroups)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string groupName = kvp.Key;
                    var groupItems = kvp.Value;

                    if (groupItems.Count <= 1)
                    {
                        // Single file! Do not make a folder container. Dump back to loose files.
                        looseFiles.AddRange(groupItems);
                        continue;
                    }

                    // Multi-item group: create folder container
                    var groupFolderNode = new FileObjectNode
                    {
                        Name = groupName,
                        Type = NodeType.FolderFull,
                        SourceFullPath = null,
                        DestinationFullPath = Path.Combine(Path.GetDirectoryName(groupItems[0].SourceFullPath) ?? "", groupName),
                        Selected = true,
                        Parent = previewRoot
                    };

                    foreach (var item in groupItems)
                    {
                        var movedFileNode = CloneNodeStructure(item, groupFolderNode);
                        movedFileNode.DestinationFullPath = Path.Combine(groupFolderNode.DestinationFullPath ?? "", item.Name);
                        movedFileNode.Selected = true;
                        groupFolderNode.Children.Add(movedFileNode);
                    }

                    activeGroupFolders[groupName] = groupFolderNode;
                    previewRoot.Children.Add(groupFolderNode);
                }

                // Build M3U playlists if enabled
                if (buildM3u)
                {
                    foreach (var groupKv in activeGroupFolders)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string groupName = groupKv.Key;
                        var groupFolder = groupKv.Value;

                        // Collect all valid disc filenames in this group folder
                        var m3uLines = groupFolder.Children
                            .Where(c => c.Type == NodeType.FileChd ||
                                        c.Type == NodeType.FileCue ||
                                        c.Type == NodeType.FileGdi ||
                                        c.Type == NodeType.FileStandardIso)
                            .Select(c => c.Name)
                            .ToList();

                        if (m3uLines.Count > 0)
                        {
                            string m3uFileName = $"{groupName}.m3u";
                            var m3uNode = new FileObjectNode
                            {
                                Name = m3uFileName,
                                Type = NodeType.FileM3u,
                                SourceFullPath = null,
                                DestinationFullPath = Path.Combine(groupFolder.DestinationFullPath ?? "", m3uFileName),
                                M3uLines = m3uLines,
                                Selected = true,
                                Parent = groupFolder
                            };
                            groupFolder.Children.Add(m3uNode);
                        }
                    }
                }

                foreach (var loose in looseFiles)
                {
                    previewRoot.Children.Add(CloneNodeStructure(loose, previewRoot));
                }

                SortNodeRecursive(previewRoot);
                progress?.Report(new ProgressReport { Message = "Preview tree built successfully.", Percentage = 100 });
                return previewRoot;
            }, cancellationToken);
        }

        private FileObjectNode CloneNodeStructure(FileObjectNode source, FileObjectNode? parent = null)
        {
            var clone = new FileObjectNode
            {
                Name = source.Name,
                Type = source.Type,
                SourceFullPath = source.SourceFullPath,
                DestinationFullPath = source.DestinationFullPath,
                Selected = source.Selected,
                Parent = parent, // <--- Assigned here
                M3uLines = source.M3uLines != null ? new List<string>(source.M3uLines) : null,
                CueLines = source.CueLines != null ? new List<string>(source.CueLines) : null
            };

            foreach (var child in source.Children)
            {
                clone.Children.Add(CloneNodeStructure(child, clone)); // <--- Pass clone as parent
            }

            return clone;
        }

        private void InspectArchive(string archiveFilePath, FileObjectNode archiveParentNode)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string ext = Path.GetExtension(archiveFilePath);

            try
            {
                if (ext.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using (var zipArchive = ZipFile.OpenRead(archiveFilePath))
                    {
                        foreach (var entry in zipArchive.Entries)
                        {
                            string entryPath = entry.FullName;
                            if (string.IsNullOrEmpty(entryPath) || entryPath.EndsWith("/") || entryPath.EndsWith("\\")) continue;

                            var fileChildNode = PopulateArchiveEntryNode(archiveFilePath, entryPath, archiveParentNode);

                            // Read .cue contents directly from zip stream if matched
                            if (fileChildNode != null && fileChildNode.Type == NodeType.FileCue)
                            {
                                try
                                {
                                    using (var entryStream = entry.Open())
                                    using (var reader = new StreamReader(entryStream))
                                    {
                                        fileChildNode.CueLines = reader.ReadToEnd().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to read zip-embedded CUE {entryPath}: {ex.Message}");
                                    fileChildNode.CueLines = new List<string>();
                                }
                            }
                        }
                    }
                }
                else if (ext.Equals(".7z", StringComparison.OrdinalIgnoreCase))
                {
                    using (var extractor = new SharpSevenZipExtractor(archiveFilePath))
                    {
                        // Note: SharpSevenZipExtractor lets us pull streams via SafeExtract or Index
                        for (int i = 0; i < extractor.ArchiveFileData.Count; i++)
                        {
                            var entry = extractor.ArchiveFileData[i];
                            if (entry.IsDirectory) continue;

                            string entryPath = entry.FileName;
                            if (string.IsNullOrEmpty(entryPath)) continue;

                            var fileChildNode = PopulateArchiveEntryNode(archiveFilePath, entryPath, archiveParentNode);

                            if (fileChildNode != null && fileChildNode.Type == NodeType.FileCue)
                            {
                                try
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        extractor.ExtractFile(i, ms);
                                        ms.Position = 0;
                                        using (var reader = new StreamReader(ms))
                                        {
                                            fileChildNode.CueLines = reader.ReadToEnd().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to read 7z-embedded CUE {entryPath}: {ex.Message}");
                                    fileChildNode.CueLines = new List<string>();
                                }
                            }
                        }
                    }
                }
                else
                {
                    using (var archive = ArchiveFactory.OpenArchive(archiveFilePath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.IsDirectory) continue;

                            string entryPath = entry.Key;
                            if (string.IsNullOrEmpty(entryPath)) continue;

                            var fileChildNode = PopulateArchiveEntryNode(archiveFilePath, entryPath, archiveParentNode);

                            if (fileChildNode != null && fileChildNode.Type == NodeType.FileCue)
                            {
                                try
                                {
                                    using (var entryStream = entry.OpenEntryStream())
                                    using (var reader = new StreamReader(entryStream))
                                    {
                                        fileChildNode.CueLines = reader.ReadToEnd().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to read SharpCompress-embedded CUE {entryPath}: {ex.Message}");
                                    fileChildNode.CueLines = new List<string>();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to inspect archive {archiveFilePath}: {ex.Message}");
            }

            sw.Stop();
        }

        private void SortNodeRecursive(FileObjectNode node)
        {
            if (node.Children == null || node.Children.Count == 0) return;

            // Sort the children into a temporary list
            var sortedChildren = node.Children
                .OrderBy(c => c.Type == NodeType.FolderFull || c.Type == NodeType.FolderEmpty ? 0 : 1)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Clear and re-populate the existing read-only collection in-place
            node.Children.Clear();
            node.Children.AddRange(sortedChildren);

            // Recurse down children
            foreach (var child in node.Children)
            {
                SortNodeRecursive(child);
            }
        }

        private FileObjectNode PopulateArchiveEntryNode(string archiveFilePath, string entryPath, FileObjectNode archiveParentNode)
        {
            string[] pathParts = entryPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length == 0) return null;

            FileObjectNode currentParent = archiveParentNode;
            FileObjectNode fileChildNode = null;

            for (int i = 0; i < pathParts.Length; i++)
            {
                string partName = pathParts[i];
                bool isFile = (i == pathParts.Length - 1);

                if (!isFile)
                {
                    var existingFolder = currentParent.Children
                        .FirstOrDefault(c => c.Name.Equals(partName, StringComparison.OrdinalIgnoreCase)
                                          && (c.Type == NodeType.FolderFull || c.Type == NodeType.FolderEmpty));

                    if (existingFolder == null)
                    {
                        existingFolder = new FileObjectNode
                        {
                            Name = partName,
                            Type = NodeType.FolderFull,
                            SourceFullPath = null,
                            DestinationFullPath = null,
                            Parent = currentParent // <--- Assign parent link
                        };
                        currentParent.Children.Add(existingFolder);
                    }
                    currentParent = existingFolder;
                }
                else
                {
                    NodeType internalFileType = FileTypeMapper.GetNodeType(Path.GetExtension(partName));

                    fileChildNode = new FileObjectNode
                    {
                        Name = partName,
                        Type = internalFileType,
                        SourceFullPath = $"{archiveFilePath}!{entryPath}",
                        DestinationFullPath = null,
                        Parent = currentParent // Set parent link here too
                    };
                    currentParent.Children.Add(fileChildNode);
                }
            }

            return fileChildNode;
        }


    }
}