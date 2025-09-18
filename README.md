# dotnet-api-neo4j

## Install & Run Neo4j Desktop

1. Download [Neo4j Desktop](https://neo4j.com/download/).
2. Create a **new project**, add a **local database** (v5.x).
3. Choose a password (e.g. `test1234` for exercises).
4. Start the database → green dot = running.

**Connection details**:

* **Bolt URI**: `bolt://localhost:7687`
* **User**: `neo4j`
* **Password**: your password (`test12` in examples)

---
## Add Package 
```bash
dotnet add package Neo4j.Driver
```

## Connect from .NET 8 Web API

`appsettings.json`:

```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "User": "neo4j",
    "Password": "test1234",
    "Database": "neo4j"
  }
}
```

**Program.cs**:

```csharp
using Neo4j.Driver;

builder.Services.Configure<Neo4jOptions>(
    builder.Configuration.GetSection("Neo4j"));

builder.Services.AddSingleton<IDriver>(sp =>
{
    var opt = sp.GetRequiredService<
        Microsoft.Extensions.Options.IOptions<Neo4jOptions>>().Value;
    return GraphDatabase.Driver(opt.Uri,
        AuthTokens.Basic(opt.User, opt.Password));
});

builder.Services.AddScoped<INeoSessionFactory, NeoSessionFactory>();
```

**Infrastructure types**:

```csharp
public sealed record Neo4jOptions
{
    public string Uri { get; init; } = default!;
    public string User { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string Database { get; init; } = "neo4j";
}

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
```

**Program.cs**:

```csharp
app.MapGet("/health/neo4j", async (INeoSessionFactory sessions, CancellationToken ct) =>
{
    await using var s = sessions.OpenRead();
    var ok = await s.ExecuteReadAsync(async tx =>
    {
        var cursor = await tx.RunAsync("RETURN 1 AS ok");
        return (await cursor.SingleAsync())["ok"].As<int>() == 1;
    });
    return ok ? Results.Ok(new { status = "ok" })
              : Results.Problem("neo4j failed");
});
```

**Note:** Added Create Thing and Get Things route in program.cs as examples.
