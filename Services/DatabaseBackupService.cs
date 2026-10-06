using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace POS_BIGA.Services
{
    public class BackupResult
    {
        public bool   Success  { get; set; }
        public string Message  { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long   SizeBytes{ get; set; }
    }

    public class DatabaseBackupService
    {
        // ─── استخراج Connection String ──────────────────────────────
        private static string GetConnectionString()
        {
            var fromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv;

            // محاولة قراءة appsettings.json
            foreach (var candidate in new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
                Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "appsettings.json"))
            })
            {
                try
                {
                    if (!File.Exists(candidate)) continue;
                    var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(candidate));
                    if (json.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                        cs.TryGetProperty("DefaultConnection", out var dc))
                    {
                        var v = dc.GetString();
                        if (!string.IsNullOrWhiteSpace(v)) return v;
                    }
                }
                catch { }
            }
            return "Server=localhost\\SQLEXPRESS;Database=POS_BIGA;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        private static string GetDatabaseName()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(GetConnectionString());
                return builder.InitialCatalog;
            }
            catch { return "POS_BIGA"; }
        }

        // ─── إنشاء نسخة احتياطية ────────────────────────────────────
        public async Task<BackupResult> BackupAsync(string destinationFolder)
        {
            try
            {
                Directory.CreateDirectory(destinationFolder);

                // ✅ منح SQL Server صلاحية الكتابة على المجلد
                GrantSqlServerAccess(destinationFolder);

                var dbName   = GetDatabaseName();
                var stamp    = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var fileName = $"{dbName}_backup_{stamp}.bak";
                var filePath = Path.Combine(destinationFolder, fileName);

                var connStr = GetConnectionString();
                await using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                var sql = $"BACKUP DATABASE [{dbName}] TO DISK = N'{filePath}' WITH NOFORMAT, INIT, NAME = N'{dbName} Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 300 };
                await cmd.ExecuteNonQueryAsync();

                var size = new FileInfo(filePath).Length;
                return new BackupResult
                {
                    Success   = true,
                    Message   = " ✅  تمت النسخة الاحتياطية بنجاح",
                    FilePath  = filePath,
                    SizeBytes = size
                };
            }
            catch (Exception ex)
            {
                return new BackupResult
                {
                    Success = false,
                    Message = $"❌  فشل النسخ الاحتياطي: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// منح حساب خدمة SQL Server صلاحية الكتابة الكاملة على المجلد
        /// </summary>
        private static void GrantSqlServerAccess(string folder)
        {
            try
            {
                // حسابات خدمة SQL Server الشائعة
                var sqlAccounts = new[]
                {
                    "NT Service\\MSSQLSERVER",
                    "NT Service\\MSSQL$SQLEXPRESS",
                    "NETWORK SERVICE",
                    "NT AUTHORITY\\NETWORK SERVICE"
                };

                var dirInfo = new System.IO.DirectoryInfo(folder);
                var security = dirInfo.GetAccessControl();

                foreach (var account in sqlAccounts)
                {
                    try
                    {
                        security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                            account,
                            System.Security.AccessControl.FileSystemRights.FullControl,
                            System.Security.AccessControl.InheritanceFlags.ContainerInherit |
                            System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                            System.Security.AccessControl.PropagationFlags.None,
                            System.Security.AccessControl.AccessControlType.Allow));
                    }
                    catch { /* الحساب غير موجود في هذا النظام */ }
                }

                dirInfo.SetAccessControl(security);
            }
            catch
            {
                // تجاهل أخطاء الصلاحيات — SQL Server قد يملكها بالفعل
            }
        }

        // ─── استعادة من نسخة احتياطية ───────────────────────────────
        public async Task<BackupResult> RestoreAsync(string backupFilePath)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                    return new BackupResult { Success = false, Message = "❌ الملف غير موجود." };

                var dbName  = GetDatabaseName();
                var connStr = new SqlConnectionStringBuilder(GetConnectionString())
                {
                    InitialCatalog = "master"
                }.ConnectionString;

                await using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                // فصل كل الاتصالات أولاً
                var killSql = $@"
                    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    RESTORE DATABASE [{dbName}] FROM DISK = N'{backupFilePath}'
                        WITH REPLACE, RECOVERY, STATS = 10;
                    ALTER DATABASE [{dbName}] SET MULTI_USER;";

                await using var cmd = new SqlCommand(killSql, conn) { CommandTimeout = 600 };
                await cmd.ExecuteNonQueryAsync();

                return new BackupResult
                {
                    Success  = true,
                    Message  = $"✅ تمت استعادة قاعدة البيانات بنجاح ",
                    FilePath = backupFilePath
                };
            }
            catch (Exception ex)
            {
                return new BackupResult
                {
                    Success  = false,
                    Message  = $"❌ فشل الاستعادة: {ex.Message}"
                };
            }
        }

        // ─── قائمة النسخ الموجودة في مجلد ──────────────────────────
        public List<BackupFileInfo> GetBackupFiles(string folder)
        {
            var result = new List<BackupFileInfo>();
            if (!Directory.Exists(folder)) return result;

            foreach (var file in Directory.GetFiles(folder, "*.bak"))
            {
                var info = new FileInfo(file);
                result.Add(new BackupFileInfo
                {
                    FileName    = info.Name,
                    FilePath    = file,
                    SizeBytes   = info.Length,
                    CreatedAt   = info.CreationTime
                });
            }
            result.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
            return result;
        }
    }

    public class BackupFileInfo
    {
        public string   FileName  { get; set; } = string.Empty;
        public string   FilePath  { get; set; } = string.Empty;
        public long     SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string   SizeText  => SizeBytes < 1024 * 1024
            ? $"{SizeBytes / 1024.0:N1} KB"
            : $"{SizeBytes / (1024.0 * 1024):N2} MB";
    }
}
