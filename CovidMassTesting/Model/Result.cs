using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// This object hold the public information for the visitor result
    /// </summary>
    public class Result
    {
        /// <summary>
        /// State. Available states are listed in Model.TestResult
        /// </summary>
        public string State { get; set; }
        /// <summary>
        /// Verification id is used to share the test results with others. It does not contain any sensitive data such as personal number, but it contains information when the visitor has taken the test with the test result and his name.
        /// </summary>
        public string VerificationId { get; set; }
    }
}
