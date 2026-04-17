namespace BlueFood.Api.Models;

public class DashboardOverviewDto
{
    public int TotalBatches { get; set; }
    public int TotalTraceEvents { get; set; }
    public int TotalCertificatesAttached { get; set; }
    public IReadOnlyList<DashboardChartItemDto> EventTypeDistribution { get; set; } = [];
    public IReadOnlyList<DashboardChartItemDto> TimelineSeries { get; set; } = [];
}

public class DashboardChartItemDto
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}
