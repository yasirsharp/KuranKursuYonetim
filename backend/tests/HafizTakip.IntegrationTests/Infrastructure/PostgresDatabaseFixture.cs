using HafizTakip.DataAccess.Concrete.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HafizTakip.IntegrationTests.Infrastructure;

/// <summary>
/// Test çalıştırması başında geçici bir PostgreSQL veritabanı oluşturur, şemayı
/// database/ klasöründeki SQL scriptlerinden kurar (script-first), iki test kursu ekler
/// ve çalıştırma sonunda veritabanını siler.
/// </summary>
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    public static readonly Guid TenantA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid TenantB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    public static readonly Guid TeacherA = Guid.Parse("aaaaaaaa-1111-0000-0000-000000000001");
    public static readonly Guid TeacherB = Guid.Parse("bbbbbbbb-1111-0000-0000-000000000002");
    public static readonly Guid StudentA = Guid.Parse("aaaaaaaa-2222-0000-0000-000000000001");
    public static readonly Guid StudentB = Guid.Parse("bbbbbbbb-2222-0000-0000-000000000002");

    private readonly string? _serverConnectionString =
        Environment.GetEnvironmentVariable(PostgresFactAttribute.EnvironmentVariable);

    private readonly string _databaseName = $"hafiztakip_it_{Guid.NewGuid():N}"[..30];

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_serverConnectionString);

    public string ConnectionString { get; private set; } = string.Empty;

    static PostgresDatabaseFixture()
    {
        // Uygulama ile aynı Npgsql zaman damgası davranışı (Program.cs)
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public async Task InitializeAsync()
    {
        if (!IsAvailable) return;

        var builder = new NpgsqlConnectionStringBuilder(_serverConnectionString) { Database = "postgres" };
        await using (var admin = new NpgsqlConnection(builder.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        builder.Database = _databaseName;
        ConnectionString = builder.ConnectionString;

        await ExecuteScriptAsync(FindDatabaseFile("00_full_schema.sql"));
        await ExecuteSqlAsync(SeedSql);
    }

    public async Task DisposeAsync()
    {
        if (!IsAvailable || string.IsNullOrEmpty(ConnectionString)) return;

        NpgsqlConnection.ClearAllPools();
        var builder = new NpgsqlConnectionStringBuilder(_serverConnectionString) { Database = "postgres" };
        await using var admin = new NpgsqlConnection(builder.ConnectionString);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    /// <summary>Verilen kurs bağlamıyla yeni bir DbContext (uygulamadaki ayarlarla aynı).</summary>
    public HafizTakipDbContext CreateContext(Guid tenantId) => CreateContext(new TestTenantContext(tenantId));

    public HafizTakipDbContext CreateContext(TestTenantContext tenantContext, CommandCounter? commandCounter = null)
    {
        var builder = new DbContextOptionsBuilder<HafizTakipDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention();
        if (commandCounter != null) builder.AddInterceptors(commandCounter);
        return new HafizTakipDbContext(builder.Options, tenantContext);
    }

    /// <summary>Testler arası veri çakışmasını önlemek için A kursunda yeni bir öğrenci oluşturur.</summary>
    public async Task<Guid> CreateStudentAsync(string firstName = "Test", string lastName = "Öğrenci", Guid? groupId = null)
    {
        var id = Guid.NewGuid();
        var group = groupId.HasValue ? $"'{groupId}'" : "NULL";
        await ExecuteSqlAsync($"""
            INSERT INTO students (id, tenant_id, group_id, first_name, last_name, identification_number)
            VALUES ('{id}', '{TenantA}', {group}, '{firstName}', '{lastName}', '');
            """);
        return id;
    }

    public async Task ExecuteSqlAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T?> ScalarAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? default : (T)Convert.ChangeType(value, typeof(T));
    }

    public Task ExecuteScriptAsync(string path) => ExecuteSqlAsync(File.ReadAllText(path));

    /// <summary>Test çıktı klasöründen yukarı çıkarak depo kökündeki database/ dosyasını bulur.</summary>
    public static string FindDatabaseFile(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "database", fileName);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException($"database/{fileName} bulunamadı.");
    }

    private static string SeedSql => $"""
        INSERT INTO tenants (id, name, slug, status, is_active) VALUES
            ('{TenantA}', 'Test Kursu A', 'test-a', 'active', true),
            ('{TenantB}', 'Test Kursu B', 'test-b', 'active', true);
        INSERT INTO users (id, tenant_id, first_name, last_name, phone_number, role) VALUES
            ('{TeacherA}', '{TenantA}', 'Hoca', 'A', '5000000001', 'head_teacher'),
            ('{TeacherB}', '{TenantB}', 'Hoca', 'B', '5000000002', 'head_teacher');
        INSERT INTO students (id, tenant_id, first_name, last_name, identification_number) VALUES
            ('{StudentA}', '{TenantA}', 'Öğrenci', 'A', ''),
            ('{StudentB}', '{TenantB}', 'Öğrenci', 'B', '');
        """;
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "Postgres";
}
