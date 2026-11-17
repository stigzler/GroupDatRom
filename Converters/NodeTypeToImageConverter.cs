using GroupDatRom.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace GroupDatRom.Converters
{
    internal class NodeTypeToImageConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not NodeType nodeType) return null;

            string iconFileName = nodeType switch
            {
                NodeType.Root => "Root.png",
                NodeType.FolderFull => "FolderFull.png",
                NodeType.FolderEmpty => "FolderEmpty.png",
                NodeType.FileM3u => "FileM3u.png",
                NodeType.FileCue => "FileCue.png",
                NodeType.FileGdi => "FileGdi.png",
                NodeType.FileChd => "FileChd.png",
                NodeType.FileRvzWia => "FileRvzWia.png",
                NodeType.FileCsoZso => "FileCsoZso.png",
                NodeType.File3DsContainer => "File3DsContainer.png",
                NodeType.FileStandardIso => "FileStandardIso.png",
                NodeType.FileBin => "FileBin.png",
                NodeType.FileCartridge => "FileCartridge.png",
                NodeType.FileVintageMedia => "FileVintageMedia.png",
                NodeType.FileImage => "FileImage.png",
                NodeType.FileMusic => "FileMusic.png",
                NodeType.Archive => "Archive.png",
                _ => "FileOther.png"
            };

            string fullPath = Path.Combine(AppContext.BaseDirectory, "Icons", iconFileName);

            if (File.Exists(fullPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
