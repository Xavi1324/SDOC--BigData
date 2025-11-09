namespace SDOC.Application.Interfaces.IRepository
{
    public interface ISourceReader<TEntity> where TEntity : class
    {
        Task<IEnumerable<TEntity>> ReadAsync();
    }
}
