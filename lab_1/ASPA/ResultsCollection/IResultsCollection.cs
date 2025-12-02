namespace ResultsCollection
{
    public interface IResultsCollection
    {
        Task<List<ResultItem>> GetAllAsync();
        Task<ResultItem?> GetAsync(int id);
        Task<ResultItem> AddAsync(string value);
        Task<ResultItem> UpdateAsync(int id, string newValue);
        Task<ResultItem> RemoveAsync(int id);
    }
}