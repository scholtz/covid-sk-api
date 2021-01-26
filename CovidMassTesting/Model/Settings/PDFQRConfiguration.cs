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
        public long OffsetBranch { get; set; } = 1000000;
        public int OffsetIter { get; set; } = 2;
        public int Columns { get; set; } = 3;
        public int Count { get; set; } = 8 * 3 * 3;
        public float Height { get; set; } = 50f;
        public float Width { get; set; } = 180f;
        public float CellPaddingTop { get; set; } = 1f;
        public float CellPaddingRight { get; set; } = 10F;
        public float CellPaddingLeft { get; set; } = 20F;
        public float CellPaddingBottom { get; set; } = 1f;
        public float PageMarginTop { get; set; } = 15f;
        public float PageMarginRight { get; set; } = 10f;
        public float PageMarginLeft { get; set; } = 10f;
        public float PageMarginBottom { get; set; } = 10f;
        public string Type { get; set; } = "QRCode";
        public float Scale { get; set; } = 1.9f;
    }
}
