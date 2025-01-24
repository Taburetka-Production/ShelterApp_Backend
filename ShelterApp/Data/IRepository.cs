namespace ShelterApp.Data
{
    public interface IRepository<T> : IDisposable
        where T : class
    {
        IEnumerable<T> GetList();
        T Get(Guid id);
        void Create(T item);
        void Update(T item);
        void Delete(Guid id);
        void Save();
    }
}
