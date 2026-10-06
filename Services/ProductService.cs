using System.Threading.Tasks;
using POS_BIGA.Models;
using POS_BIGA.Data.Interfaces;
using POS_BIGA.Data.Repositories;
using System.Collections.Generic;

namespace POS_BIGA.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;

        public ProductService()
        {
            _repo = new ProductRepository();
        }

        public async Task<List<Product>> GetProductsAsync() => await _repo.GetAllProductsAsync();

        public async Task AddProductAsync(Product product) => await _repo.AddProductAsync(product);

        public async Task UpdateProductAsync(Product product) => await _repo.UpdateProductAsync(product);

        public async Task<bool> DeleteProductAsync(int productId) => await _repo.DeleteProductAsync(productId);

        public async Task<Product> GetProductByBarcodeAsync(string barcode) => await _repo.GetByBarcodeAsync(barcode);
    }
}
