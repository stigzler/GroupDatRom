using System;
using System.Collections.Generic;
using System.Text;

namespace GroupDatRom.Models
{
    internal class ProgressReport
    {
        internal string Message { get; set; }
        internal int Percentage { get; set; }
        public int CurrentItem { get; set; }
        public int TotalItems { get; set; }
    }
}
