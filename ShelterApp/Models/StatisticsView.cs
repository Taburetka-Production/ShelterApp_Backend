using Microsoft.EntityFrameworkCore;

namespace ShelterApp
{
    [Keyless]
    public class StatisticsView
    {
        public int totalshelters { get; set; }
        public int totalanimals { get; set; }
        public int totalusers { get; set; }
        public int totalregions { get; set; }
        public int totaladoptions { get; set; }
    }
}
