using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace GroupDatRom.Models
{
    internal partial class FileObjectNode: ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private NodeType type;

        public string SourceFullPath { get; set; }      // Null for new folders / generated M3U
        public string DestinationFullPath { get; set; }
        public List<string> M3uLines { get; set; }    
        public List<string> CueLines { get; set; }      

        public ObservableCollection<FileObjectNode> Children { get; } = new();
    }
}
