namespace CovidMassTesting.Model
{
    /// <summary>
    /// Test results strings
    /// </summary>
    public static class TestResult
    {
        /// <summary>
        /// Waiting for visitor to take the test
        /// </summary>
        public const string NotTaken = "test-not-taken";
        /// <summary>
        /// Test has been taken by visitor and waiting for test results
        /// </summary>
        public const string TestIsBeingProcessing = "test-not-processed";
        /// <summary>
        /// Test result is failed test, and must be repeated
        /// </summary>
        public const string TestMustBeRepeated = "test-to-be-repeated";
        /// <summary>
        /// Test result is positive. Waiting for visitor to take the certificate
        /// </summary>
        public const string PositiveWaitingForCertificate = "positive";
        /// <summary>
        /// Test result is positive. Certificate has been given to visitor.
        /// </summary>
        public const string PositiveCertificateTaken = "positive-certiciate-taken";
        /// <summary>
        /// Test result is negative. Waiting for visitor to take the certificate
        /// </summary>
        public const string NegativeWaitingForCertificate = "negative";
        /// <summary>
        /// Test result is negative. Certificate has been given to visitor.
        /// </summary>
        public const string NegativeCertificateTaken = "negative-certificate-taken";
        /// <summary>
        /// Test result is negative. Certificate has been given to visitor.
        /// </summary>
        public const string NegativeCertificateTakenTypo = "negative-certiciate-taken";
    }
}
