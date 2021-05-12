using System;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Product or service served by the place provider at testing places
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Category
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// Name of the product or service
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Insurance only
        /// </summary>
        public bool InsuranceOnly { get; set; }
        /// <summary>
        /// Only for parents who need the test for the school verification
        /// </summary>
        public bool? SchoolOnly { get; set; } = false;
        /// <summary>
        /// Only for external tests - Self tests or the bookkeeping of the testing records of employees
        /// </summary>
        public bool? ExternalOnly { get; set; } = false;
        /// <summary>
        /// Corporations may check this on and only its employees are eligible for the testing
        /// </summary>
        public bool? EmployeesOnly { get; set; } = false;
        /// <summary>
        /// Corporations may check this on and only its employees can self register
        /// </summary>
        public bool? EmployeesRegistration { get; set; } = false;
        /// <summary>
        /// Collect employee id but do not verify it
        /// </summary>
        public bool? CollectEmployeeNo { get; set; } = false;
        /// <summary>
        /// Nationality is required by some insurance companies
        /// </summary>
        public bool? CollectNationality { get; set; } = false;
        /// <summary>
        /// Default for eHealth connection
        /// </summary>
        public bool? EHealthDefault { get; set; } = false;
        /// <summary>
        /// Collect insurance organisation
        /// </summary>
        public bool? CollectInsurance { get; set; } = true;

        /// <summary>
        /// Applied for all places
        /// </summary>
        public bool All { get; set; }
        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Default price. Admin can set the pricing other then default at the specific places
        /// </summary>
        public decimal DefaultPrice { get; set; }
        /// <summary>
        /// Price currency
        /// </summary>
        public string DefaultPriceCurrency { get; set; }
        /// <summary>
        /// When the resource was created
        /// </summary>
        public DateTimeOffset CreatedOn { get; set; }
        /// <summary>
        /// Time when resource was last updated
        /// </summary>
        public DateTimeOffset LastUpdate { get; set; }

    }
}
