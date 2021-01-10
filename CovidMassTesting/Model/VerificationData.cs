using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// VerificationData
    /// </summary>
    public class VerificationData
    {
        /// <summary>
        /// ResultGUID
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Person name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Result
        /// </summary>
        public string Result { get; set; } = TestResult.NotTaken;
        /// <summary>
        /// Testing time
        /// </summary>
        public DateTimeOffset Time { get; set; }
        /// <summary>
        /// Identification of testing provider
        /// </summary>
        public string TestingEntity { get; set; }
        /// <summary>
        /// Address where user has been tested
        /// </summary>
        public string TestingAddress { get; set; }
        /// <summary>
        /// Product - PCR or Antigen test
        /// </summary>
        public string Product { get; set; }
    }
}
