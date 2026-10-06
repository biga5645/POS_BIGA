using System.Collections.Generic;
using System.Threading.Tasks;
using POS_BIGA.Data.Interfaces;
using POS_BIGA.Data.Repositories;
using POS_BIGA.Models;

namespace POS_BIGA.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllAsync();
        Task<Category> GetByIdAsync(int id);
        Task AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(int id);
        Task<bool> ExistsByNameAsync(string name);
    }

    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        private readonly IAppStateService _appState;

        public CategoryService(ICategoryRepository repo, IAppStateService appState)
        {
            _repo = repo;
            _appState = appState;
        }

        public async Task<List<Category>> GetAllAsync() => await _repo.GetAllCategoriesAsync();

        public async Task<Category> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);

        public async Task AddAsync(Category category)
        {
            await _repo.AddCategoryAsync(category);
            _appState.NotifyCategoriesChanged();
        }

        public async Task UpdateAsync(Category category)
        {
            await _repo.UpdateCategoryAsync(category);
            _appState.NotifyCategoriesChanged();
        }

        public async Task DeleteAsync(int id)
        {
            await _repo.DeleteCategoryAsync(id);
            _appState.NotifyCategoriesChanged();
        }

        public async Task<bool> ExistsByNameAsync(string name) => await _repo.ExistsByNameAsync(name);
    }
}
