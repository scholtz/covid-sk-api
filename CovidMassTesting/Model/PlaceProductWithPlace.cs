using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Extended information about place product with product object filled in
    /// </summary>
    public class PlaceProductWithPlace : PlaceProduct
    {
        /// <summary>
        /// Product
        /// </summary>
        public Product Product { get; set; }
    }
}
