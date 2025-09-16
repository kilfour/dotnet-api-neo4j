namespace DotNetApiNeo4j.NeoInfrastructure;

public sealed record Neo4jOptions
{
    public string Uri { get; init; } = default!;
    public string User { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string Database { get; init; } = "neo4j";
}
