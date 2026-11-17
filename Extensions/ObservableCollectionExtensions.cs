using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace GroupDatRom.Extensions
{
    internal static class ObservableCollectionExtensions
    {
        internal static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }
    }
}
