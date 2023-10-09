namespace ZoaReference.Features.Charts.Models;

public class Chart
{
    public string AirportName { get; set; }
    public string FaaIdent { get; set; }
    public string IcaoIdent { get; set; }
    public string ChartSeq { get; set; }
    public string ChartCode { get; set; }
    public string ChartName { get; set; }
    public ICollection<ChartPage> Pages { get; set;}
    
    public static Chart FromAviationApiDto(AviationApiChartDto chartDto, string name = "", int pageNumber = -1)
    {
        return new Chart
        {
            AirportName = chartDto.AirportName,
            FaaIdent = chartDto.FaaIdent,
            IcaoIdent = chartDto.IcaoIdent,
            ChartSeq = chartDto.ChartSeq,
            ChartCode = chartDto.ChartCode,
            ChartName = string.IsNullOrEmpty(name) ? chartDto.ChartName : name,
            Pages = new List<ChartPage>
            {
                new ChartPage
                {
                    PageNumber = pageNumber == -1 ? 1 : pageNumber,
                    PdfName = chartDto.PdfName,
                    PdfPath = chartDto.PdfPath
                }
            }
        };
    }

}

public class ChartPage
{
    public int PageNumber { get; set; }
    public string PdfName { get; set; }
    public string PdfPath { get; set; }
}
