namespace HRM.Model.Healthcare
{
    /// <summary>
    /// Kết quả đo đạc cho 1 thuật toán tìm kiếm.
    /// </summary>
    public class AlgoSearchResult
    {
        public string AlgoName { get; set; } = string.Empty;
        public long TotalMs { get; set; }
        public long Step1Ms { get; set; }
        public long Step2Ms { get; set; }
        public int TotalRecords { get; set; }
        public int CandidateCount { get; set; }
        public int ScannedCount { get; set; }
        public int ResultCount { get; set; }
        public List<PatientDto> Results { get; set; } = new();
    }

    /// <summary>
    /// Kết quả so sánh song song giữa V1 Baseline và V2 BitGram.
    /// </summary>
    public class BenchmarkResult
    {
        public string Keyword { get; set; } = string.Empty;
        public AlgoSearchResult V1_Baseline { get; set; } = new();
        public AlgoSearchResult V2_BitGram { get; set; } = new();
    }
}
