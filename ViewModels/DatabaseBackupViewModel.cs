using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using POS_BIGA.Commands;
using POS_BIGA.Services;

namespace POS_BIGA.ViewModels
{
    public class DatabaseBackupViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseBackupService _svc = new();

        public DatabaseBackupViewModel()
        {
            // مجلد افتراضي: C:\POS_BIGA_Backups — يملك SQL Server صلاحية الكتابة فيه
            BackupFolder = @"C:\POS_BIGA_Backups";

            BackupCommand         = new RelayCommand(async _ => await DoBackupAsync());
            RestoreCommand        = new RelayCommand(async _ => await DoRestoreAsync());
            BrowseFolderCommand   = new RelayCommand(_ => BrowseFolder());
            BrowseFileCommand     = new RelayCommand(_ => BrowseFile());
            RefreshListCommand    = new RelayCommand(_ => RefreshList());
            DeleteBackupCommand   = new RelayCommand(f => DeleteBackup(f as BackupFileInfo));
            OpenFolderCommand     = new RelayCommand(_ => OpenFolder());

            RefreshList();
        }

        // ─── Commands ───────────────────────────────────────────────
        public ICommand BackupCommand       { get; }
        public ICommand RestoreCommand      { get; }
        public ICommand BrowseFolderCommand { get; }
        public ICommand BrowseFileCommand   { get; }
        public ICommand RefreshListCommand  { get; }
        public ICommand DeleteBackupCommand { get; }
        public ICommand OpenFolderCommand   { get; }

        // ─── Properties ─────────────────────────────────────────────
        private string _backupFolder = string.Empty;
        public string BackupFolder
        {
            get => _backupFolder;
            set { _backupFolder = value; OnPropertyChanged(nameof(BackupFolder)); RefreshList(); }
        }

        private string _selectedRestoreFile = string.Empty;
        public string SelectedRestoreFile
        {
            get => _selectedRestoreFile;
            set { _selectedRestoreFile = value; OnPropertyChanged(nameof(SelectedRestoreFile)); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(IsIdle)); }
        }
        public bool IsIdle => !_isBusy;

        private bool _hasStatus;
        public bool HasStatus
        {
            get => _hasStatus;
            set { _hasStatus = value; OnPropertyChanged(nameof(HasStatus)); }
        }

        private bool _isError;
        public bool IsError
        {
            get => _isError;
            set { _isError = value; OnPropertyChanged(nameof(IsError)); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        private BackupFileInfo? _selectedBackup;
        public BackupFileInfo? SelectedBackup
        {
            get => _selectedBackup;
            set { _selectedBackup = value; OnPropertyChanged(nameof(SelectedBackup)); }
        }

        public ObservableCollection<BackupFileInfo> BackupFiles { get; } = new();

        // ─── Logic ──────────────────────────────────────────────────
        private async Task DoBackupAsync()
        {
            if (string.IsNullOrWhiteSpace(BackupFolder))
            {
                SetStatus("⚠️  الرجاء تحديد مجلد الحفظ أولاً.", isError: true);
                return;
            }

            IsBusy = true;
            SetStatus("جارٍ إنشاء النسخة الاحتياطية ...", isError: false);

            var result = await _svc.BackupAsync(BackupFolder);

            IsBusy = false;
            SetStatus(result.Success
                ? $"{result.Message}\n📁 {Path.GetFileName(result.FilePath)}  ({new BackupFileInfo { SizeBytes = result.SizeBytes }.SizeText})"
                : result.Message,
                isError: !result.Success);

            RefreshList();
        }

        private async Task DoRestoreAsync()
        {
            var file = !string.IsNullOrWhiteSpace(SelectedRestoreFile) && File.Exists(SelectedRestoreFile)
                ? SelectedRestoreFile
                : null;

            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                SetStatus("⚠️  الرجاء اختيار ملف النسخة الاحتياطية أولاً.", isError: true);
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"⚠️ سيتم استبدال قاعدة البيانات الحالية بالكامل :\n\n{Path.GetFileName(file)}\n\nهل أنت متأكد؟",
                "تأكيد الاستعادة ",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            SetStatus("جارٍ استعادة قاعدة البيانات...", isError: false);

            var result = await _svc.RestoreAsync(file);

            IsBusy = false;
            SetStatus(result.Message, isError: !result.Success);
        }

        /// <summary>اختيار مجلد عبر OpenFileDialog - حيلة WPF بدون WindowsForms</summary>
        private void BrowseFolder()
        {
            var dialog = new OpenFileDialog
            {
                Title            = " اختر مجلد حفظ النسخ الاحتياطية (افتح أي ملف داخله)",
                Filter           = " مجلد|*.___none___",
                CheckFileExists  = false,
                CheckPathExists  = true,
                FileName         = "اختر هذا المجلد",
                InitialDirectory = Directory.Exists(BackupFolder) ? BackupFolder : string.Empty
            };

            if (dialog.ShowDialog() == true)
                BackupFolder = Path.GetDirectoryName(dialog.FileName) ?? BackupFolder;
        }

        private void BrowseFile()
        {
            var dialog = new OpenFileDialog
            {
                Title            = " اختر ملف النسخة الاحتياطية",
                Filter           = "ملفات النسخ الاحتياطي  (*.bak)|*.bak|كل الملفات (*.*)|*.*",
                InitialDirectory = Directory.Exists(BackupFolder) ? BackupFolder : string.Empty
            };
            if (dialog.ShowDialog() == true)
                SelectedRestoreFile = dialog.FileName;
        }

        private void RefreshList()
        {
            BackupFiles.Clear();
            if (!Directory.Exists(BackupFolder)) return;
            foreach (var f in _svc.GetBackupFiles(BackupFolder))
                BackupFiles.Add(f);
        }

        private void DeleteBackup(BackupFileInfo? info)
        {
            if (info == null) return;
            var confirm = System.Windows.MessageBox.Show(
                $"هل تريد حذف هذه النسخة ؟\n{info.FileName}",
                " تأكيد الحذف",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                File.Delete(info.FilePath);
                SetStatus($"🗑️ تم حذف {info.FileName}", isError: false);
                RefreshList();
            }
            catch (Exception ex)
            {
                SetStatus($"❌ فشل الحذف: {ex.Message}", isError: true);
            }
        }

        private void OpenFolder()
        {
            if (!Directory.Exists(BackupFolder)) Directory.CreateDirectory(BackupFolder);
            System.Diagnostics.Process.Start("explorer.exe", BackupFolder);
        }

        private void SetStatus(string msg, bool isError)
        {
            StatusMessage = msg;
            IsError       = isError;
            HasStatus     = !string.IsNullOrEmpty(msg);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
