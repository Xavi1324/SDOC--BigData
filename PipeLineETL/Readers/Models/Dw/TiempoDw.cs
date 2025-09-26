namespace Readers.Models.Dw
{
    public class TiempoDw
    {
        public int Time_Id { get; set; }
        public DateTime Date { get; set; }
        public int Year { get; set; }
        public byte Month { get; set; }
        public byte Day { get; set; }
        public byte? Hour { get; set; }
        public int DateKey { get; set; }                // computado en SQL
    }
}
