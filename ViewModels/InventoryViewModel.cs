using Microsoft.EntityFrameworkCore;
using POS_BIGA.Commands;
using POS_BIGA.Data;
using POS_BIGA.Models;
using POS_BIGA.Services;
using POS_BIGA.Views.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace POS_BIGA.ViewModels
{
    public class InventoryItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal StockQuantity { get; set; }
        public decimal Price { get; set; }
        public decimal StockValue => StockQuantity * Price;
    }

    public class InventoryViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ProductService _productService;

        public ObservableCollection<InventoryItemDto> InventoryItems { get; } = new();
        public ObservableCollection<InventoryItemDto> PagedInventoryItems { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<string> StockStatusOptions { get; } = new() { "الكل", "متوفر", "منخفض", "منتهي" };

        private InventoryItemDto? _selectedItem;
        public InventoryItemDto? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                ((RelayCommand)EditProductCommand).NotifyCanExecuteChanged();
                ((RelayCommand)DeleteProductCommand).NotifyCanExecuteChanged();
            }
        }

        private string _searchFilter = string.Empty;
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                _searchFilter = value;
                OnPropertyChanged(nameof(SearchFilter));
                _ = ApplyAllFiltersAsync();
            }
        }

        private bool _showFilters;
        public bool ShowFilters
        {
            get => _showFilters;
            set { _showFilters = value; OnPropertyChanged(nameof(ShowFilters)); }
        }

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged(nameof(SelectedCategory));
                _ = ApplyAllFiltersAsync();
            }
        }

        private string _selectedStockStatus = "الكل";
        public string SelectedStockStatus
        {
            get => _selectedStockStatus;
            set
            {
                _selectedStockStatus = value;
                OnPropertyChanged(nameof(SelectedStockStatus));
                _ = ApplyAllFiltersAsync();
            }
        }

        private string _minPrice = string.Empty;
        public string MinPrice
        {
            get => _minPrice;
            set { _minPrice = value; OnPropertyChanged(nameof(MinPrice)); _ = ApplyAllFiltersAsync(); }
        }

        private string _maxPrice = string.Empty;
        public string MaxPrice
        {
            get => _maxPrice;
            set { _maxPrice = value; OnPropertyChanged(nameof(MaxPrice)); _ = ApplyAllFiltersAsync(); }
        }

        private int _totalProducts;
        public int TotalProducts
        {
            get => _totalProducts;
            set { _totalProducts = value; OnPropertyChanged(nameof(TotalProducts)); }
        }

        private int _lowStockCount;
        public int LowStockCount
        {
            get => _lowStockCount;
            set { _lowStockCount = value; OnPropertyChanged(nameof(LowStockCount)); }
        }

        private int _outOfStockCount;
        public int OutOfStockCount
        {
            get => _outOfStockCount;
            set { _outOfStockCount = value; OnPropertyChanged(nameof(OutOfStockCount)); }
        }

        private int _lowStockOnlyCount;
        public int LowStockOnlyCount
        {
            get => _lowStockOnlyCount;
            set { _lowStockOnlyCount = value; OnPropertyChanged(nameof(LowStockOnlyCount)); }
        }

        private int _goodStockCount;
        public int GoodStockCount
        {
            get => _goodStockCount;
            set { _goodStockCount = value; OnPropertyChanged(nameof(GoodStockCount)); }
        }

        private decimal _totalStockValue;
        public decimal TotalStockValue
        {
            get => _totalStockValue;
            set { _totalStockValue = value; OnPropertyChanged(nameof(TotalStockValue)); }
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(nameof(CurrentPage)); }
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(nameof(TotalPages)); }
        }

        private int _pageSize = 25;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize == value) return;
                _pageSize = value;
                OnPropertyChanged(nameof(PageSize));
                CurrentPage = 1;
                ApplyPagination();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ToggleFiltersCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand ApplyQuickStockFilterCommand { get; }

        public InventoryViewModel()
        {
            _context = new AppDbContext();
            _productService = new ProductService();

            RefreshCommand = new RelayCommand(async _ => await LoadInventoryAsync());
            AddProductCommand = new RelayCommand(async _ => await AddProductAsync());
            EditProductCommand = new RelayCommand(async param => await EditProductAsync(param), _ => SelectedItem != null);
            DeleteProductCommand = new RelayCommand(async param => await DeleteProductAsync(param), _ => SelectedItem != null);
            ClearSearchCommand = new RelayCommand(async _ => await ClearSearchAsync());
            ToggleFiltersCommand = new RelayCommand(_ => ShowFilters = !ShowFilters);
            ExportCommand = new RelayCommand(async _ => await ExportCsvAsync());
            ApplyFiltersCommand = new RelayCommand(async _ => await ApplyAllFiltersAsync());
            ClearFiltersCommand = new RelayCommand(async _ => await ClearFiltersAsync());
            PrevPageCommand = new RelayCommand(_ => GoPrevPage(), _ => CurrentPage > 1);
            NextPageCommand = new RelayCommand(_ => GoNextPage(), _ => CurrentPage < TotalPages);
            ApplyQuickStockFilterCommand = new RelayCommand(async p => await ApplyQuickStockFilterAsync(p as string));
        }

        public async Task ApplyQuickStockFilterAsync(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return;
            SelectedStockStatus = status;
            CurrentPage = 1;
            await ApplyAllFiltersAsync();
        }

        public async Task ResetQuickFiltersAsync()
        {
            SearchFilter = string.Empty;
            SelectedCategory = null;
            SelectedStockStatus = "الكل";
            MinPrice = string.Empty;
            MaxPrice = string.Empty;
            CurrentPage = 1;
            await ApplyAllFiltersAsync();
        }

        public async Task LoadInventoryAsync()
        {
            try
            {
                Categories.Clear();
                var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                foreach (var c in categories) Categories.Add(c);

                await ApplyAllFiltersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المخزون: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ApplyAllFiltersAsync()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.UnitEntity)
                .AsNoTracking()
                .Where(p => p.IsActive)
                .ToListAsync();

            var invByProduct = new System.Collections.Generic.Dictionary<int, Inventory>();
            try
            {
                var inventories = await _context.Inventories.AsNoTracking().ToListAsync();
                invByProduct = inventories.ToDictionary(i => i.ProductId, i => i);
            }
            catch
            {
                // fallback: إذا جدول Inventories غير جاهز بعد، نعرض مخزون المنتج الأساسي
            }

            var query = products.Select(p =>
            {
                var hasInventory = invByProduct.TryGetValue(p.Id, out var inv);
                var qty = hasInventory ? inv!.QuantityAvailable : p.CurrentStock;

                return new InventoryItemDto
                {
                    Id = p.Id,
                    ProductId = p.Id,
                    Name = p.Name,
                    Barcode = p.Barcode,
                    CategoryName = p.Category?.Name ?? "غير مصنف",
                    StockQuantity = qty,
                    Price = p.Price
                };
            });

            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                var s = SearchFilter.Trim().ToLower();
                query = query.Where(x =>
                    (x.Name ?? string.Empty).ToLower().Contains(s) ||
                    (x.Barcode ?? string.Empty).ToLower().Contains(s) ||
                    (x.CategoryName ?? string.Empty).ToLower().Contains(s));
            }

            if (SelectedCategory != null)
            {
                query = query.Where(x => x.CategoryName == SelectedCategory.Name);
            }

            if (decimal.TryParse(MinPrice, out var minP))
                query = query.Where(x => x.Price >= minP);

            if (decimal.TryParse(MaxPrice, out var maxP))
                query = query.Where(x => x.Price <= maxP);

            query = SelectedStockStatus switch
            {
                "منتهي" => query.Where(x => x.StockQuantity <= 0),
                "منخفض" => query.Where(x => x.StockQuantity > 0 && x.StockQuantity <= 5),
                "متوفر" => query.Where(x => x.StockQuantity > 5),
                _ => query
            };

            var list = query.OrderBy(x => x.Name).ToList();

            InventoryItems.Clear();
            foreach (var item in list) InventoryItems.Add(item);

            UpdateStats(list);
            CurrentPage = 1;
            ApplyPagination();
        }

        private void UpdateStats(System.Collections.Generic.List<InventoryItemDto> list)
        {
            TotalProducts = list.Count;
            OutOfStockCount = list.Count(x => x.StockQuantity <= 0);
            LowStockOnlyCount = list.Count(x => x.StockQuantity > 0 && x.StockQuantity <= 5);
            GoodStockCount = list.Count(x => x.StockQuantity > 5);
            LowStockCount = LowStockOnlyCount + OutOfStockCount;
            TotalStockValue = list.Sum(x => x.StockValue);
        }

        private void ApplyPagination()
        {
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)InventoryItems.Count / PageSize));
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            var page = InventoryItems.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            PagedInventoryItems.Clear();
            foreach (var item in page) PagedInventoryItems.Add(item);

            (PrevPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (NextPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        private void GoPrevPage()
        {
            if (CurrentPage <= 1) return;
            CurrentPage--;
            ApplyPagination();
        }

        private void GoNextPage()
        {
            if (CurrentPage >= TotalPages) return;
            CurrentPage++;
            ApplyPagination();
        }

        private async Task AddProductAsync()
        {
            var win = new ProductEditorWindow { Owner = Application.Current?.MainWindow };
            if (win.ShowDialog() == true)
            {
                await _productService.AddProductAsync(win.Product);
                await EnsureInventoryRowAsync(win.Product.Id);
                await LoadInventoryAsync();
            }
        }

        public async Task AddProductFromEditorAsync(Product product)
        {
            await _productService.AddProductAsync(product);
            await EnsureInventoryRowAsync(product.Id);
            await LoadInventoryAsync();
        }

        private async Task EditProductAsync(object? param)
        {
            var target = param as InventoryItemDto ?? SelectedItem;
            if (target == null) return;

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == target.ProductId);
            if (product == null) return;

            var win = new ProductEditorWindow(product) { Owner = Application.Current?.MainWindow };
            if (win.ShowDialog() == true)
            {
                await _productService.UpdateProductAsync(win.Product);
                await EnsureInventoryRowAsync(product.Id);
                await LoadInventoryAsync();
            }
        }

        public async Task UpdateProductFromEditorAsync(Product product)
        {
            await _productService.UpdateProductAsync(product);
            await EnsureInventoryRowAsync(product.Id);
            await LoadInventoryAsync();
        }

        public async Task<Product?> GetProductForEditAsync(int productId)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        }

        private async Task DeleteProductAsync(object? param)
        {
            var target = param as InventoryItemDto ?? SelectedItem;
            if (target == null) return;

            var result = MessageBox.Show($"حذف المنتج: {target.Name}؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == target.ProductId);
            if (product == null) return;

            try
            {
                var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == product.Id);
                if (inv != null)
                {
                    _context.Inventories.Remove(inv);
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                // ignore inventory delete issues to keep product delete functional
            }

            await _productService.DeleteProductAsync(product.Id);

            await LoadInventoryAsync();
        }

        private async Task EnsureInventoryRowAsync(int productId)
        {
            try
            {
                var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
                if (inv != null) return;

                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
                if (product == null) return;

                var row = new Inventory
                {
                    ProductId = productId,
                    QuantityOnHand = product.CurrentStock,
                    QuantityReserved = 0,
                    QuantityAvailable = product.CurrentStock,
                    MinimumQuantity = product.MinimumStock,
                    MaximumQuantity = product.MinimumStock * 3,
                    ReorderQuantity = product.MinimumStock,
                    LastReceivedDate = DateTime.UtcNow,
                    IsLowStock = product.CurrentStock > 0 && product.CurrentStock <= product.MinimumStock,
                    IsOutOfStock = product.CurrentStock <= 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Inventories.Add(row);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // fallback: لا نكسر التدفق إذا جدول Inventories غير متوفر
            }
        }

        private async Task ClearSearchAsync()
        {
            SearchFilter = string.Empty;
            await ApplyAllFiltersAsync();
        }

        private async Task ClearFiltersAsync()
        {
            SelectedCategory = null;
            SelectedStockStatus = "الكل";
            MinPrice = string.Empty;
            MaxPrice = string.Empty;
            await ApplyAllFiltersAsync();
        }

        private async Task ExportCsvAsync()
        {
            if (!InventoryItems.Any()) return;
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"inventory_{DateTime.Now:yyyyMMdd_HHmm}.csv");

            var sb = new StringBuilder();
            sb.AppendLine("Id,Name,Barcode,Category,Stock,Price,StockValue");
            foreach (var i in InventoryItems)
            {
                sb.AppendLine($"{i.Id},\"{i.Name}\",\"{i.Barcode}\",\"{i.CategoryName}\",{i.StockQuantity},{i.Price},{i.StockValue}");
            }

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
            MessageBox.Show($"تم التصدير بنجاح:\n{path}", "تصدير", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose() => _context.Dispose();
    }
}
