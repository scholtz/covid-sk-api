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
        /// Available values in State
        /// </summary>
        public class Values
        {
            /// <summary>
            /// Not found
            /// </summary>
            public const string NotFound = "code-not-found";
            /// <summary>
            /// Positive
            /// </summary>
            public const string Positive = "positive";
            /// <summary>
            /// Negative
            /// </summary>
            public const string Negative = "negative";
            /// <summary>
            /// ToRepeat
            /// </summary>
            public const string ToRepeat = "test-to-be-repeated";
            /// <summary>
            /// If technical delay eg 15 minutes is not met from the testing time, the result should not be processed
            /// </summary>
            public const string ResultTooSoon = "result-too-soon";
        }
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Id of testing set
        /// </summary>
        public string TestingSetId { get; set; }
        /// <summary>
        /// State. Available states are listed in Model.TestResult
        /// </summary>
        public string State { get; set; }
        /// <summary>
        /// Verification id is used to share the test results with others. It does not contain any sensitive data such as personal number, but it contains information when the visitor has taken the test with the test result and his name.
        /// </summary>
        public string VerificationId { get; set; }
        /// <summary>
        /// Time when result has been inserted
        /// </summary>
        public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;
        /// <summary>
        /// Matched with visitor
        /// </summary>
        public bool Matched { get; set; }
        /// <summary>
        /// Returns true if time is valid
        /// </summary>
        public bool TimeIsValid { get; set; }
    }
}
