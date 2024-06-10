namespace ZoaReference.Features.VnasData.Models;

public class FacilityExtended(Facility facility, IEnumerable<VideoMap> videoMaps)
{
    public Facility Facility { get; set; } = facility;
    public List<VideoMap> VideoMaps { get; set; } = videoMaps.ToList();
}
