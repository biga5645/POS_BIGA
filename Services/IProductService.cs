using System.Collections.Generic;
using System.Threading.Tasks;
using POS_BIGA.Models;

namespace POS_BIGA.Services
{
    public interface IProductService
    {
        Task<List<Product>> GetProductsAsync();
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int productId);
        Task<Product> GetProductByBarcodeAsync(string barcode);
    }
}
