using Neo4j.Driver;

namespace DotNetApiNeo4j.NeoInfrastructure;

public interface INeoSessionFactory
{
    IAsyncSession OpenRead();
    IAsyncSession OpenWrite();
}

public sealed class NeoSessionFactory : INeoSessionFactory
{
    private readonly IDriver _driver;
    private readonly string _db;
    public NeoSessionFactory(IDriver driver,
        Microsoft.Extensions.Options.IOptions<Neo4jOptions> opt)
    {
        _driver = driver;
        _db = opt.Value.Database;
    }

    public IAsyncSession OpenRead() =>
        _driver.AsyncSession(o => o.WithDatabase(_db)
                                   .WithDefaultAccessMode(AccessMode.Read));

    public IAsyncSession OpenWrite() =>
        _driver.AsyncSession(o => o.WithDatabase(_db)
                                   .WithDefaultAccessMode(AccessMode.Write));
}