using DotNetApiNeo4j.Example;
using DotNetApiNeo4j.NeoInfrastructure;
using Neo4j.Driver;

var builder = WebApplication.CreateBuilder(args);
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



var app = builder.Build();


// ------------------------------------------------------------------------------------------
// Is DB Online
// -- 
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

// ------------------------------------------------------------------------------------------
// Create Things
// -- 
app.MapPost("/things", async (INeoSessionFactory sessions, Thing input, CancellationToken ct) =>
{
    await using var s = sessions.OpenWrite();
    var created = await s.ExecuteWriteAsync(async tx =>
    {
        var id = Guid.NewGuid().ToString("N");
        var cursor = await tx.RunAsync(
            "CREATE (c:Case {id: $id, name: $name}) RETURN c.id AS id, c.name AS name",
            new { id, name = input.Name });
        var record = await cursor.SingleAsync();
        return new Thing(record["id"].As<string>(), record["name"].As<string>());
    });
    return Results.Created($"/cases/{created.Id}", created);
});

// ------------------------------------------------------------------------------------------
// All Things
// -- 
app.MapGet("/things", async (INeoSessionFactory sessions) =>
{
    await using var s = sessions.OpenRead();
    var items = await s.ExecuteReadAsync(async tx =>
    {
        var cursor = await tx.RunAsync(
            "MATCH (c:Case) RETURN c.id AS id, c.name AS name");

        // Build the list in one go:
        return await cursor.ToListAsync(r => new Thing(
            r["id"].As<string>(),
            r["name"].As<string>()
        ));
    });

    return Results.Ok(items);
});

// ------------------------------------------------------------------------------------------
// One Thing
// -- 
app.MapGet("/things/{id}", async (string id, INeoSessionFactory sessions) =>
{
    await using var s = sessions.OpenRead();
    var thing = await s.ExecuteReadAsync(async tx =>
    {
        var result = await tx.RunAsync(
            "MATCH (c:Case {id:'" + id + "'}) RETURN c.id AS id, c.name AS name",
            new { id });
        return await result.SingleAsync();
    });
    return Results.Ok(new Thing(
            thing["id"].As<string>(),
            thing["name"].As<string>()
        ));
});

// app.MapGet("/things/{id}", async (string id, INeoSessionFactory sessions) =>
// {
//     await using var s = sessions.OpenRead();

//     return await s.ExecuteReadAsync<IResult>(async tx =>
//     {
//         var cursor = await tx.RunAsync(
//             "MATCH (c:Case {id:$id}) RETURN c.id AS id, c.name AS name", new { id });

//         var record = await cursor.SingleAsync();
//         if (record is null) return Results.NotFound();

//         var thing = new Thing(
//             record["id"].As<string>(),
//             record["name"].As<string>());

//         return Results.Ok(thing);
//     }, ct);
// });


app.Run();
