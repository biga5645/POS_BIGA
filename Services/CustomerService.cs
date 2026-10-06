using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using POS_BIGA.Data.Interfaces;
using POS_BIGA.Data.Repositories;
using POS_BIGA.Models;

namespace POS_BIGA.Services
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer> GetByIdAsync(int id);
        Task AddAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task DeleteAsync(int id);
    }

    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repo;
        private readonly IAppStateService _appState;

        public CustomerService(ICustomerRepository repo, IAppStateService appState)
        {
            _repo = repo;
            _appState = appState;
        }

        public async Task<List<Customer>> GetAllAsync() => await _repo.GetAllCustomersAsync();

        public async Task<Customer> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);

        public async Task AddAsync(Customer customer)
        {
            try
            {
                await _repo.AddCustomerAsync(customer);
                _appState.NotifyCustomersChanged();
            }
            catch (System.Exception ex)
            {
                // Surface the exception to the user for debugging the save problem
                MessageBox.Show($"??? ????? ??? ??????: {ex.Message}", "??? ???", MessageBoxButton.OK, MessageBoxImage.Error);
                // swallow to keep caller simple; caller may reload or react accordingly
            }
        }

        public async Task UpdateAsync(Customer customer)
        {
            try
            {
                await _repo.UpdateCustomerAsync(customer);
                _appState.NotifyCustomersChanged();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"??? ????? ????? ?????? ??????: {ex.Message}", "??? ?????", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                await _repo.DeleteCustomerAsync(id);
                _appState.NotifyCustomersChanged();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"??? ????? ??? ??????: {ex.Message}", "??? ???", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
