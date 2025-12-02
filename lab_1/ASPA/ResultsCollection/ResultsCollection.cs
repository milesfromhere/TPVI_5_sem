using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ResultsCollection
{
    public record ResultItem(int Id, string Value);

    public class ResultsCollection : IResultsCollection
    {
        private readonly string _filePath;
        private static SemaphoreSlim _semaphore = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions;



        public ResultsCollection(IConfiguration configuration)
        {
            _filePath = configuration["FilePath"] ?? "default.json";

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }



        public async Task<List<ResultItem>> GetAllAsync()
        {
            try
            {
                var items = await ReadFromJsonAsync();
                return items;
            }
            catch (Exception ex)
            {
                throw new ResultsCollectionException("Ошибка при получении всех элементов", ex);
            }
        }

        public async Task<ResultItem?> GetAsync(int id)
        {
            try
            {
                var items = await ReadFromJsonAsync();
                return items.FirstOrDefault(r => r.Id == id);
            }
            catch (Exception ex)
            {
                throw new ResultsCollectionException($"Ошибка при получении элемента с идентификатором {id}", ex);
            }
        }

        public async Task<ResultItem> AddAsync(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Новое значение пустое", nameof(value));

            try
            {
                var items = await ReadFromJsonAsync();
                var newId = items.Count > 0 ? items.Max(r => r.Id) + 1 : 1;
                var newItem = new ResultItem(newId, value);
                items.Add(newItem);
                await WriteToJsonAsync(items);
                return newItem;
            }
            catch (Exception ex)
            {
                throw new ResultsCollectionException("Ошибка добавления элемента", ex);
            }
        }

        public async Task<ResultItem> UpdateAsync(int id, string newValue)
        {
            if (string.IsNullOrWhiteSpace(newValue))
                throw new ArgumentException("Новое значение пустое.", nameof(newValue));

            try
            {
                var items = await ReadFromJsonAsync();
                var item = items.FirstOrDefault(r => r.Id == id) ?? throw new KeyNotFoundException($"Элемент с идентификатором {id} не найден");
                var updatedItem = item with { Value = newValue };
                items[items.IndexOf(item)] = updatedItem;
                await WriteToJsonAsync(items);
                return updatedItem;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ResultsCollectionException($"Ошибка обновления элемента с идентификатором {id}", ex);
            }
        }

        public async Task<ResultItem> RemoveAsync(int id)
        {
            try
            {
                var items = await ReadFromJsonAsync();
                var item = items.FirstOrDefault(r => r.Id == id);

                if (item == null)
                    throw new KeyNotFoundException($"Элемент с идентификатором {id} не найден");

                items.Remove(item);
                await WriteToJsonAsync(items);
                return item;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ResultsCollectionException($"Ошибка удаления элемента с идентификатором {id}", ex);
            }
        }



        private async Task WriteToJsonAsync(List<ResultItem> items)
        {
            await _semaphore.WaitAsync();

            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(items, _jsonOptions);
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch (Exception ex)
            {
                throw new ResultsCollectionException("Ошибка записи в файл хранилища", ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<List<ResultItem>> ReadFromJsonAsync()
        {
            await _semaphore.WaitAsync();

            try
            {
                if (!File.Exists(_filePath))
                    return new List<ResultItem>();

                var json = await File.ReadAllTextAsync(_filePath);

                if (string.IsNullOrWhiteSpace(json))
                    return new List<ResultItem>();

                var items = JsonSerializer.Deserialize<List<ResultItem>>(json, _jsonOptions);
                return items ?? new List<ResultItem>();
            }
            catch (JsonException ex)
            {
                throw new ResultsCollectionException("Ошибка десериализации данных из файла хранилища", ex);
            }
            catch (Exception ex)
            {
                throw new ResultsCollectionException("Ошибка чтения из файла хранилища", ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
