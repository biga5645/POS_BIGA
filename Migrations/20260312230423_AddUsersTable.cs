using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS_BIGA.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // إنشاء الجدول فقط إذا لم يكن موجوداً
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                BEGIN
                    CREATE TABLE [Users] (
                        [Id]           INT IDENTITY(1,1) NOT NULL,
                        [Username]     NVARCHAR(100) NOT NULL,
                        [FullName]     NVARCHAR(150) NOT NULL,
                        [PasswordHash] NVARCHAR(256) NOT NULL,
                        [Pin]          NVARCHAR(20)  NULL,
                        [Role]         INT NOT NULL,
                        [IsActive]     BIT NOT NULL DEFAULT 1,
                        [CreatedAt]    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedAt]    DATETIME2 NULL,
                        [LastLoginAt]  DATETIME2 NULL,
                        [CreatedBy]    NVARCHAR(100) NOT NULL DEFAULT '',
                        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
                    );

                    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
                    CREATE INDEX [IX_Users_IsActive] ON [Users] ([IsActive]);

                    INSERT INTO [Users] ([Username],[FullName],[PasswordHash],[Pin],[Role],[IsActive],[CreatedAt],[UpdatedAt],[LastLoginAt],[CreatedBy])
                    VALUES (N'admin', N'مدير النظام',
                            N'$2a$11$K7JUGRrFxl0A4eQ.RiSWxeqvXGMsBMPPHPGfz8rGFiJi4sCoqQ3Oy',
                            NULL, 1, 1, '2024-01-01 00:00:00', NULL, NULL, N'System');
                END
            ");

            // إضافة المدير الافتراضي إذا كان الجدول موجوداً لكن لا يحتوي على admin
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                   AND NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = N'admin')
                BEGIN
                    INSERT INTO [Users] ([Username],[FullName],[PasswordHash],[Pin],[Role],[IsActive],[CreatedAt],[UpdatedAt],[LastLoginAt],[CreatedBy])
                    VALUES (N'admin', N'مدير النظام',
                            N'$2a$11$K7JUGRrFxl0A4eQ.RiSWxeqvXGMsBMPPHPGfz8rGFiJi4sCoqQ3Oy',
                            NULL, 1, 1, '2024-01-01 00:00:00', NULL, NULL, N'System');
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Users");
        }
    }
}
