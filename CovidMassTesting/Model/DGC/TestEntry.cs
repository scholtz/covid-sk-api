namespace CovidMassTesting.Model.DGC
{
    public class TestEntry
    {
        public string Uvci { get; set; }
        public string IssuerId { get; set; }
        public string TestType { get; set; }
        public string TestResult { get; set; }
        public string TestName { get; set; }
        public string SampleCollectedAt { get; set; }
        public string ResultProducedAt { get; set; }
        public string CollectionCentreName { get; set; }
    }
}