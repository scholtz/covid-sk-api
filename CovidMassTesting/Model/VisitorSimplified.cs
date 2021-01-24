using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// NCZI - Datova veta pre registracny system Ag testov
    /// </summary>
    public class VisitorSimplified
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Meno { get; set; }
        /// <summary>
        /// Last name
        /// </summary>
        public string Priezvisko { get; set; }
        /// <summary>
        /// Last name
        /// </summary>
        public string RodneCislo { get; set; }
        /// <summary>
        /// Telefon
        /// </summary>
        public string Telefon { get; set; }
        /// <summary>
        /// Mail
        /// </summary>
        public string Mail { get; set; }
        /// <summary>
        /// PSC
        /// </summary>
        public string PSC { get; set; }
        /// <summary>
        /// Mesto
        /// </summary>
        public string Mesto { get; set; }
        /// <summary>
        /// Ulica
        /// </summary>
        public string Ulica { get; set; }
        /// <summary>
        /// Cislo
        /// </summary>
        public string Cislo { get; set; }
        /// <summary>
        /// DatumVysetrenia
        /// </summary>
        public string DatumVysetrenia { get; set; }
        /// <summary>
        /// DatumVysetrenia
        /// </summary>
        public string TypVysetrenia { get; set; }
        /// <summary>
        /// DatumVysetrenia
        /// </summary>
        public string VysledokVysetrenia { get; set; }
    }
}
