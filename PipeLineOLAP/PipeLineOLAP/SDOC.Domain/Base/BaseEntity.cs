namespace SDOC.Domain.Base
{
    public abstract class BaseEntity <Tkey>
    {
        public required Tkey Id { get; set; }
        public DateTime Date { get; set; }
    }
}
