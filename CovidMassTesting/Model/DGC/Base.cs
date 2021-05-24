using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.DGC
{
    public class Base
    {
        public string DgcLanguage { get; set; }
        public string[] RequiredAttachments { get; set; }
        public Subject Subject { get; set; }
        public TestEntry TestEntry { get; set; }
    }
}
