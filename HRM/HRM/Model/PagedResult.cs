namespace HRM.Model
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public SearchDebugInfo? SearchDebug { get; set; }
    }

    public class SearchDebugInfo
    {
        public string? SearchKeyword { get; set; }
        public string? SearchType { get; set; }
        public string? DecryptedValue { get; set; }
        public string? CompareResult { get; set; }
        public long TotalMs { get; set; }
        public long Step1Ms { get; set; }
        public long Step2Ms { get; set; }
        public int CandidateCount { get; set; }
        public int ScannedRecordCount { get; set; }
        public int CollisionCount { get; set; }
        public int ResultCount { get; set; }
    }
}
