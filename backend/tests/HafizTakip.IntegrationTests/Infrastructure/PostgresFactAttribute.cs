namespace HafizTakip.IntegrationTests.Infrastructure;

/// <summary>
/// Gerçek PostgreSQL gerektiren test. HAFIZTAKIP_TEST_PG ortam değişkeni tanımlı değilse atlanır.
/// Örnek: HAFIZTAKIP_TEST_PG="Host=localhost;Username=postgres;Password=postgres"
/// (Veritabanı adı verilmez; her test çalıştırması geçici bir veritabanı oluşturup siler.)
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "HAFIZTAKIP_TEST_PG";

    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariable)))
        {
            Skip = $"{EnvironmentVariable} tanımlı değil; PostgreSQL entegrasyon testi atlandı.";
        }
    }
}
