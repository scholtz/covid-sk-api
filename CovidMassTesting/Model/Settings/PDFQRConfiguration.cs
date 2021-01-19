using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Settings
{
    /// <summary>
    /// Settings for pdf generation
    /// </summary>
    public class PDFQRConfiguration
    {
        public string Prefix { get; set; } = "BA01";
        public long Increment { get; set; } = 3;
        public long OffsetBranch { get; set; } = 100000;
        public int OffsetIter { get; set; } = 2;
        public int Columns { get; set; } = 3;
        public int Count { get; set; } = 100;
        public float Height { get; set; } = 50f;
        public float Width { get; set; } = 200f;
        public float CellPaddingTop { get; set; } = 5F;
        public float CellPaddingRight { get; set; } = 10F;
        public float CellPaddingLeft { get; set; } = 10F;
        public float CellPaddingBottom { get; set; } = 5F;
        public float PageMarginTop { get; set; } = 10f;
        public float PageMarginRight { get; set; } = 10f;
        public float PageMarginLeft { get; set; } = 10f;
        public float PageMarginBottom { get; set; } = 10f;
        public string Type { get; set; } = "QRCode";
        public float Scale { get; set; } = 2.5f;
    }
}
