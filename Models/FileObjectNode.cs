using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace GroupDatRom.Models
{
    internal partial class FileObjectNode: ObservableObject
    {
        // Flag to prevent infinite loops during recursive updates
        private bool _isUpdatingChildren;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private bool selected;

        [ObservableProperty]
        private NodeType type;
        public ObservableCollection<FileObjectNode> Children { get; } = new();

        public List<string> CueLines { get; set; }

        public string DestinationFullPath { get; set; }

        // Returns true if an ANCESTOR (parent up the tree) is an Archive
        public bool IsNestedWithinArchive {
            get {
                // Check if any parent up the chain is an Archive
                var current = Parent;
                while (current != null)
                {
                    if (current.Type == NodeType.Archive) return true;
                    current = current.Parent;
                }
                return false;
            }
        }

        // Returns true if this node or any ancestor up the tree is an Archive
        public bool IsWithinArchive {
            get {
                if (Type == NodeType.Archive) return true;
                return Parent?.IsWithinArchive ?? false;
            }
        }
        public List<string> M3uLines { get; set; }

        // Backreference to parent for tree traversal
        public FileObjectNode? Parent { get; set; }

        // Show checkbox if we are NOT nested inside an archive
        public bool ShowCheckbox => IsNestedWithinArchive == false;

        public string SourceFullPath { get; set; }
        public void SetSelectedRecursive(bool value)
        {
            _isUpdatingChildren = true;
            Selected = value;
            _isUpdatingChildren = false;

            foreach (var child in Children)
            {
                child.SetSelectedRecursive(value);
            }
        }

        partial void OnSelectedChanged(bool value)
        {
            if (_isUpdatingChildren) return;

            // Recursively update all child nodes when a folder's checked state changes
            foreach (var child in Children)
            {
                child.SetSelectedRecursive(value);
            }
        }
              // Null for new folders / generated M3U
    }
}
