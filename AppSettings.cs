namespace ZoaReference;

public class AppSettings
{
    public const string SectionKeyName = "AppSettings";
    public int VatsimDataKeepForfHours { get; set; } = 24;
    public int VatsimDatafeedRefreshSeconds { get; set; } = 15;
    public int DigitalAtisRefreshSeoncds { get; set; } = 60;
    public int RvrRefreshSeconds { get; set; } = 120;
    public int BindersRefreshSeconds { get; set; } = 60;
    public CacheTtlSettings CacheTtls { get; set; } = new();
    public UrlsSettings Urls { get; set; } = new();
    public ArtccAirportsSettings ArtccAirports { get; set; } = new();
    public string ReferenceBindersDirectoryInWwwroot { get; set; } = string.Empty;


    public class CacheTtlSettings
    {
        public int FlightAwareRoutes { get; set; } = 1200;
        public int Charts { get; set; } = 3600;
        public int AirlineCodes { get; set; } = 3600;
        public int VnasData { get; set; } = 3600;
        public int VatsimUserStats { get; set; } = 3600;
    }

    public class UrlsSettings
    {
        public string AppBase { get; set; } = string.Empty;
        public string AirlinesCsv { get; set; } = string.Empty;
        public string AircraftCsv { get; set; } = string.Empty;
        public string ChartsApiEndpoint { get; set; } = string.Empty;
        public string ClowdDatisApiEndpoint { get; set; } = string.Empty;
        public string NasrApiEndpoint { get; set; } = string.Empty;
        public string FlightAwareIfrRouteBase { get; set; } = string.Empty;
        public string VatsimDatafeed { get; set; } = string.Empty;
        public string MetarsXml { get; set; } = string.Empty;
        public string FaaRvrBase { get; set; } = string.Empty;
        public string FaaRvrAirportLookup { get; set; } = string.Empty;
        public string ArtccBoundariesGeoJson { get; set; } = string.Empty;
        public string VnasApiEndpoint { get; set; } = string.Empty;
        public string VatsimApiEndpoint { get; set; } = string.Empty;
        public string AliasTextFile { get; set;} = string.Empty;
        public string VatspyData { get; set; } = string.Empty;
    }

    public class ArtccAirportsSettings
    {
        public ICollection<string> Bravos { get; set; } = new List<string>();
        public ICollection<string> Charlies { get; set; } = new List<string>();
        public ICollection<string> Deltas { get; set; } = new List<string>();
        public ICollection<string> Other { get; set; } = new List<string>();
        public ICollection<string> All => Bravos.Concat(Charlies).Concat(Deltas).ToList();
    }
}
