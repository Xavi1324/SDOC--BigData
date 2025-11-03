namespace SDOC.Domain.Entities.Dwh.Dimensions
{
    public class DimTime
    {
        public  int TimeSK { get; set; }

        public DateTime Date { get; set; }

        public int Year { get; set; }

        public byte Month { get; set; }

        public string MonthName { get; set; } = string.Empty;

        public byte Day { get; set; }

        public string DayName { get; set; } = string.Empty;

        public byte Quarter { get; set; }

        public byte WeekOfYear { get; set; }
    }
}
