namespace ShelterApp.Data
{
    public interface IUnitOfWork : IDisposable
    {
        public void Save();
    }
}
