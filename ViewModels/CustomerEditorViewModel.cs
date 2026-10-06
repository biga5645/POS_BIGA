using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Commands;
using POS_BIGA.Data;
using POS_BIGA.Models;

namespace POS_BIGA.ViewModels
{
    /// <summary>
    /// MVVM ViewModel for Customer Editor
    /// </summary>
    public class CustomerEditorViewModel : INotifyPropertyChanged
    {
        private Customer _customer;
        private readonly bool _isEditMode;
        private readonly int _customerId;

        public event Action<bool> RequestClose;

        #region Properties

        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(nameof(FullName)); }
        }

        private string _phone;
        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(nameof(Phone)); }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }

        private string _address;
        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(nameof(Address)); }
        }

        private string _creditLimitText;
        public string CreditLimitText
        {
            get => _creditLimitText;
            set { _creditLimitText = value; OnPropertyChanged(nameof(CreditLimitText)); }
        }

        private string _notes;
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(nameof(DialogResult)); }
        }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        public CustomerEditorViewModel() : this(0)
        {
        }

        public CustomerEditorViewModel(int customerId)
        {
            _customerId = customerId;
            _isEditMode = customerId > 0;
            _customer = new Customer();

            Title = _isEditMode ? "تعديل بيانات العميل" : "إضافة عميل جديد";

            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            CancelCommand = new RelayCommand(_ => Cancel());

            if (_isEditMode)
            {
                _ = LoadCustomerDataAsync();
            }
            else
            {
                LoadCustomerData();
            }
        }

        private async Task LoadCustomerDataAsync()
        {
            try
            {
                using var ctx = new AppDbContext();
                var customer = await ctx.Customers.FindAsync(_customerId);
                if (customer != null)
                {
                    _customer = customer;
                    LoadCustomerData();
                }
            }
            catch
            {
                // Ignore
            }
        }

        private void LoadCustomerData()
        {
            FullName = _customer.FullName ?? string.Empty;
            Phone = _customer.Phone ?? string.Empty;
            Email = _customer.Email ?? string.Empty;
            Address = _customer.Address ?? string.Empty;
            CreditLimitText = _customer.CreditLimit.ToString("0.00");
            Notes = _customer.Notes ?? string.Empty;
        }

        private async Task SaveAsync()
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(FullName))
                {
                    MessageBox.Show("⚠️ يرجى إدخال اسم العميل!", "حقل مطلوب",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Parse credit limit (optional - default to 0 if empty)
                decimal creditLimit = 0m;
                if (!string.IsNullOrWhiteSpace(CreditLimitText))
                {
                    if (!decimal.TryParse(CreditLimitText, out creditLimit))
                    {
                        MessageBox.Show("⚠️ سقف الدين يجب أن يكون رقماً صحيحاً!", "خطأ في الإدخال",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Validate email format if provided
                if (!string.IsNullOrWhiteSpace(Email))
                {
                    var emailTrimmed = Email.Trim();
                    if (!System.Text.RegularExpressions.Regex.IsMatch(emailTrimmed, 
                        @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    {
                        MessageBox.Show("⚠️ صيغة البريد الإلكتروني غير صحيحة!", "خطأ في الإدخال",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Update customer object
                _customer.FullName = FullName.Trim();
                _customer.Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
                _customer.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
                _customer.Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim();
                _customer.CreditLimit = creditLimit;
                _customer.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();

                using var ctx = new AppDbContext();

                if (_isEditMode)
                {
                    // Edit mode
                    var existing = await ctx.Customers.FindAsync(_customer.Id);
                    if (existing != null)
                    {
                        existing.FullName = _customer.FullName;
                        existing.Phone = _customer.Phone;
                        existing.Email = _customer.Email;
                        existing.Address = _customer.Address;
                        existing.CreditLimit = _customer.CreditLimit;
                        existing.Notes = _customer.Notes;
                        existing.UpdatedAt = DateTime.Now;
                    }
                }
                else
                {
                    // Add mode - check for duplicate phone (only if phone is provided)
                    if (!string.IsNullOrWhiteSpace(_customer.Phone))
                    {
                        var duplicate = await ctx.Customers
                            .FirstOrDefaultAsync(c => c.Phone == _customer.Phone);

                        if (duplicate != null)
                        {
                            MessageBox.Show(
                                $"⚠️ يوجد عميل بنفس رقم الهاتف:\n{duplicate.FullName}",
                                "عميل مكرر",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning
                            );
                            return;
                        }
                    }

                    // Set required fields for new customer
                    _customer.CreatedAt = DateTime.UtcNow;
                    _customer.CreatedBy = "المدير";
                    _customer.IsActive = true;

                    ctx.Customers.Add(_customer);
                }

                await ctx.SaveChangesAsync();

                // Notify app state
                var appState = App.SharedAppState;
                appState?.NotifyCustomersChanged();
                appState?.NotifyCustomerUpdated(_customer);

                MessageBox.Show(
                    _isEditMode ? "✅ تم تحديث بيانات العميل بنجاح!" : "✅ تم إضافة العميل بنجاح!",
                    "نجح الحفظ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                DialogResult = true;
            }
            catch (Exception ex)
            {
                // Build detailed error message
                var errorMessage = $"❌ خطأ في حفظ بيانات العميل:\n\n";
                errorMessage += $"الرسالة: {ex.Message}\n\n";

                // Show inner exception if exists
                if (ex.InnerException != null)
                {
                    errorMessage += $"التفاصيل: {ex.InnerException.Message}\n\n";

                    // SQL Server specific errors
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage += $"خطأ SQL: {ex.InnerException.InnerException.Message}";
                    }
                }

                MessageBox.Show(
                    errorMessage,
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Cancel()
        {
            DialogResult = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
