using POS_BIGA.Commands;
using POS_BIGA.Models;
using POS_BIGA.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace POS_BIGA.ViewModels
{
    public class UnitViewModel : INotifyPropertyChanged
    {
        private readonly IUnitService _service;
        private readonly IAppStateService _appState;

        public ObservableCollection<Unit> Units { get; } = new ObservableCollection<Unit>();

        private Unit _selectedUnit;
        public Unit SelectedUnit
        {
            get => _selectedUnit;
            set { _selectedUnit = value; OnPropertyChanged(nameof(SelectedUnit)); ((RelayCommand)EditCommand).NotifyCanExecuteChanged(); ((RelayCommand)DeleteCommand).NotifyCanExecuteChanged(); }
        }

        private string _newUnitName;
        public string NewUnitName { get => _newUnitName; set { _newUnitName = value; OnPropertyChanged(nameof(NewUnitName)); ((RelayCommand)AddCommand).NotifyCanExecuteChanged(); } }

        private string _newUnitAbbrev;
        public string NewUnitAbbrev { get => _newUnitAbbrev; set { _newUnitAbbrev = value; OnPropertyChanged(nameof(NewUnitAbbrev)); ((RelayCommand)AddCommand).NotifyCanExecuteChanged(); } }

        // ? ????? ????? ????????
        private bool _isDivisible;
        public bool IsDivisible 
        { 
            get => _isDivisible; 
            set 
            { 
                _isDivisible = value; 
                OnPropertyChanged(nameof(IsDivisible));
                if (!value)
                {
                    SubUnitName = string.Empty;
                    SubUnitQuantity = "1";
                }
            } 
        }

        private string _subUnitName;
        public string SubUnitName 
        { 
            get => _subUnitName; 
            set { _subUnitName = value; OnPropertyChanged(nameof(SubUnitName)); } 
        }

        private string _subUnitQuantity = "1";
        public string SubUnitQuantity 
        { 
            get => _subUnitQuantity; 
            set { _subUnitQuantity = value; OnPropertyChanged(nameof(SubUnitQuantity)); } 
        }

        // ? ????? ?????? ?????? ??????
        private bool _allowCustomPrice;
        public bool AllowCustomPrice
        {
            get => _allowCustomPrice;
            set
            {
                _allowCustomPrice = value;
                OnPropertyChanged(nameof(AllowCustomPrice));
            }
        }

        // ? ????? ???? ?????
        private DateTime _lastUpdateTime = DateTime.Now;
        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set { _lastUpdateTime = value; OnPropertyChanged(nameof(LastUpdateTime)); }
        }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand QuickAddCommand { get; } // ? ??????? ???????
        public ICommand SelectForEditCommand { get; } // ? ??????? ?? ??????
        public ICommand DeleteItemCommand { get; } // ? ????? ?? ??????

        public UnitViewModel() : this(new UnitService(), (Application.Current?.Resources["AppStateService"] as IAppStateService) ?? new AppStateService()) { }

        public UnitViewModel(IUnitService service, IAppStateService appState)
        {
            _service = service;
            _appState = appState;

            RefreshCommand = new RelayCommand(async _ => await LoadAsync());
            AddCommand = new RelayCommand(async _ => await AddAsync(), _ => !string.IsNullOrWhiteSpace(NewUnitName) && !string.IsNullOrWhiteSpace(NewUnitAbbrev));
            EditCommand = new RelayCommand(async _ => await EditAsync(), _ => SelectedUnit != null);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedUnit != null);

            // ? Commands ???????
            QuickAddCommand = new RelayCommand(async param => await QuickAddAsync(param));
            SelectForEditCommand = new RelayCommand(SelectForEdit);
            DeleteItemCommand = new RelayCommand(async param => await DeleteItemAsync(param));

            _appState.UnitsChanged += async (s, e) => await LoadAsync();

            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            try
            {
                Units.Clear();
                var list = await _service.GetAllAsync();
                foreach (var u in list) Units.Add(u);
                SelectedUnit = Units.FirstOrDefault();

                // ? ????? ?????
                LastUpdateTime = DateTime.Now;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"??? ??? ????? ???????: {ex.Message}", "???", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task AddAsync()
        {
            try
            {
                var name = (NewUnitName ?? string.Empty).Trim();
                var ab = (NewUnitAbbrev ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(ab)) return;

                if (await _service.ExistsByAbbreviationAsync(ab))
                {
                    MessageBox.Show("??? ???????? ????? ??????!", "?????", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ? ????? ???? ????? ?? ????? ???????
                var newUnit = new Unit 
                { 
                    Name = name, 
                    Abbreviation = ab,
                    IsDivisible = IsDivisible,
                    SubUnitName = IsDivisible ? SubUnitName?.Trim() : null,
                    SubUnitQuantity = IsDivisible && decimal.TryParse(SubUnitQuantity, out var qty) ? qty : 1,
                    AllowCustomPrice = AllowCustomPrice
                };

                await _service.AddAsync(newUnit);

                // ????? ??????
                NewUnitName = NewUnitAbbrev = string.Empty;
                IsDivisible = false;
                SubUnitName = string.Empty;
                SubUnitQuantity = "1";
                AllowCustomPrice = false;

                (_appState as AppStateService)?.NotifyUnitsChanged();
                // ✅ لا حاجة لـ LoadAsync() هنا - سيُستدعى تلقائياً من UnitsChanged event

                MessageBox.Show($"✅ تمت إضافة الوحدة: {name}", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"??? ??? ????? ????? ??????: {ex.Message}", "???", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task EditAsync()
        {
            if (SelectedUnit == null) return;
            try
            {
                SelectedUnit.Name = (SelectedUnit.Name ?? string.Empty).Trim();
                SelectedUnit.Abbreviation = (SelectedUnit.Abbreviation ?? string.Empty).Trim();

                // ? ????? ????? ??????? ?????? ??????
                SelectedUnit.IsDivisible = IsDivisible;
                SelectedUnit.SubUnitName = IsDivisible ? SubUnitName?.Trim() : null;
                SelectedUnit.SubUnitQuantity = IsDivisible && decimal.TryParse(SubUnitQuantity, out var qty) ? qty : 1;
                SelectedUnit.AllowCustomPrice = AllowCustomPrice;

                await _service.UpdateAsync(SelectedUnit);
                (_appState as AppStateService)?.NotifyUnitsChanged();
                // ✅ لا حاجة لـ LoadAsync() هنا - سيُستدعى تلقائياً من UnitsChanged event

                MessageBox.Show($"✅ تم تحديث الوحدة", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"??? ?? ????? ??????: {ex.Message}", "???", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DeleteAsync()
        {
            if (SelectedUnit == null) return;
            if (MessageBox.Show($"?? ???? ??? ?????? {SelectedUnit.Name}?", "?????", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                await _service.DeleteAsync(SelectedUnit.Id);
                (_appState as AppStateService)?.NotifyUnitsChanged();
                await LoadAsync();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"??? ??? ????? ??? ??????: {ex.Message}", "???", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ? ????? ????? ?? ???????
        private async Task QuickAddAsync(object parameter)
        {
            if (parameter is string data)
            {
                var parts = data.Split('|');
                if (parts.Length >= 2)
                {
                    var name = parts[0]?.Trim();
                    var ab = parts[1]?.Trim();
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(ab))
                    {
                        NewUnitName = name;
                        NewUnitAbbrev = ab;

                        // ? ????? ?????? ?????? ??????
                        IsDivisible = parts.Length > 2 && bool.TryParse(parts[2], out var div) && div;
                        SubUnitName = IsDivisible && parts.Length > 3 ? parts[3]?.Trim() : string.Empty;
                        SubUnitQuantity = IsDivisible && parts.Length > 4 ? parts[4]?.Trim() : "1";
                        AllowCustomPrice = parts.Length > 5 && bool.TryParse(parts[5], out var allow) && allow;

                        await AddAsync();
                    }
                }
            }
        }

        // ? ??????? ?? ??????
        private void SelectForEdit(object parameter)
        {
            if (parameter is Unit unit)
            {
                SelectedUnit = unit;
                NewUnitName = unit.Name;
                NewUnitAbbrev = unit.Abbreviation;

                // ? ????? ????? ??????? ?????? ??????
                IsDivisible = unit.IsDivisible;
                SubUnitName = unit.SubUnitName ?? string.Empty;
                SubUnitQuantity = unit.SubUnitQuantity.ToString();
                AllowCustomPrice = unit.AllowCustomPrice;

                System.Diagnostics.Debug.WriteLine($"📝 تم تحميل بيانات الوحدة للتعديل: {unit.Name}");
            }
        }

        // ? ????? ?? ??????
        private async Task DeleteItemAsync(object parameter)
        {
            if (parameter is Unit unit)
            {
                SelectedUnit = unit;
                await DeleteAsync();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
