using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using RVM.TcgDex.Serialization;

namespace RVM.TcgDex.ContractTests;

/// <summary>
/// Runs against the live TCGdex API. Fails when the API gains or loses a field on the sample
/// resources, or when a field it sends is not exposed by the SDK models.
/// <para>
/// A failure is a decision to make, not a flake to silence: map the new field in the model (or
/// accept it is gone), then regenerate the baseline with <c>UPDATE_CONTRACT_BASELINE=1 dotnet test</c>
/// and review the diff of <c>contract-baseline.json</c>.
/// </para>
/// </summary>
public sealed class ApiContractTests
{
    private const string Api = TcgDexOptions.DefaultEndpoint + "/en/";

    // Four eras, the three categories, a card without prices, a set and a serie — every model.
    private static readonly (string Resource, Type Model)[] Samples =
    [
        ("cards/swsh3-136", typeof(Card)),
        ("cards/A1-001", typeof(Card)),
        ("cards/dp1-121", typeof(Card)),
        ("cards/swsh1-181", typeof(Card)),
        ("cards/sm1-164", typeof(Card)),
        ("cards/base1-4", typeof(Card)),
        ("sets/swsh3", typeof(Set)),
        ("series/swsh", typeof(Serie)),
    ];

    private static readonly HttpClient Http = RetryOnServerError.Client();

    public static TheoryData<string> Resources => new(Samples.Select(s => s.Resource));

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task LiveFields_MatchTheBaseline(string resource)
    {
        var live = await LiveKeyPathsAsync(resource);
        var baseline = LoadBaseline()[resource];

        var added = live.Except(baseline).Order().ToList();
        var removed = baseline.Except(live).Order().ToList();

        Assert.True(added.Count == 0 && removed.Count == 0,
            $"TCGdex changed {resource}. Added: [{string.Join(", ", added)}] Removed: [{string.Join(", ", removed)}]");
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public void BaselineFields_AreAllExposedByTheModels(string resource)
    {
        var model = Samples.Single(s => s.Resource == resource).Model;

        var unmapped = LoadBaseline()[resource].Except(ModelKeyPaths(model)).Order().ToList();

        Assert.True(unmapped.Count == 0, $"{model.Name} does not expose: [{string.Join(", ", unmapped)}]");
    }

    [Fact]
    public async Task EveryEndpoint_DeserializesThroughTheSdk()
    {
        var tcgdex = new TCGdex(Http);

        foreach (var (resource, _) in Samples.Where(s => s.Model == typeof(Card)))
            Assert.NotNull(await tcgdex.Card.GetAsync(resource["cards/".Length..]));

        var set = (await tcgdex.Set.GetAsync("swsh3"))!;
        Assert.NotEmpty(set.Cards);
        Assert.NotEmpty((await set.GetSerieAsync()).Sets);
        Assert.NotEmpty(await tcgdex.Card.ListAsync(Query.Create().Equal("name", "Furret")));
        Assert.NotEmpty(await tcgdex.Set.ListAsync());
        Assert.NotEmpty(await tcgdex.Serie.ListAsync());
        Assert.NotEmpty((await tcgdex.DexIds.GetAsync(162))!.Cards);
        Assert.NotNull(await tcgdex.Random.GetCardAsync());

        Assert.NotEmpty(await tcgdex.Types.ListAsync());
        Assert.NotEmpty(await tcgdex.Hp.ListAsync());
        Assert.NotEmpty(await tcgdex.Illustrators.ListAsync());
        Assert.NotEmpty(await tcgdex.Rarities.ListAsync());
        Assert.NotEmpty(await tcgdex.Categories.ListAsync());
        Assert.NotEmpty(await tcgdex.EnergyTypes.ListAsync());
        Assert.NotEmpty(await tcgdex.Retreats.ListAsync());
        Assert.NotEmpty(await tcgdex.Stages.ListAsync());
        Assert.NotEmpty(await tcgdex.Suffixes.ListAsync());
        Assert.NotEmpty(await tcgdex.TrainerTypes.ListAsync());
        Assert.NotEmpty(await tcgdex.DexIds.ListAsync());
        Assert.NotEmpty(await tcgdex.RegulationMarks.ListAsync());
        Assert.NotEmpty(await tcgdex.Variants.ListAsync());
    }

    [Fact]
    public async Task EveryLanguage_IsServed()
    {
        var tcgdex = new TCGdex(Http);

        foreach (var language in Enum.GetValues<Language>())
        {
            tcgdex.SetLanguage(language);
            Assert.NotNull(await tcgdex.Serie.ListAsync()); // a language-invalid 404 would throw
        }
    }

    /// <summary>Not a test: regenerates the baseline when <c>UPDATE_CONTRACT_BASELINE=1</c>.</summary>
    [Fact]
    public async Task UpdateBaseline_WhenAsked()
    {
        if (Environment.GetEnvironmentVariable("UPDATE_CONTRACT_BASELINE") != "1")
            return;

        var baseline = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var (resource, _) in Samples)
            baseline[resource] = (await LiveKeyPathsAsync(resource)).Order().ToArray();

        await File.WriteAllTextAsync(SourceBaselinePath(),
            JsonSerializer.Serialize(baseline, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    private static async Task<HashSet<string>> LiveKeyPathsAsync(string resource)
    {
        using var response = await Http.GetAsync(Api + resource);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        var paths = new HashSet<string>(StringComparer.Ordinal);
        Collect(json.RootElement, "", paths);
        return paths;
    }

    /// <summary><c>attacks[].cost</c>-style paths of every property; array indices collapse to <c>[]</c>.</summary>
    private static void Collect(JsonElement element, string prefix, HashSet<string> paths)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var path = prefix.Length == 0 ? property.Name : $"{prefix}.{property.Name}";
                paths.Add(path);
                Collect(property.Value, path, paths);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                Collect(item, prefix + "[]", paths);
        }
    }

    /// <summary>The same kind of paths, as the SDK source-generated serializers know them.</summary>
    private static HashSet<string> ModelKeyPaths(Type model)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        Walk(TcgDexJsonContext.Default.GetTypeInfo(model)!, "", paths);
        return paths;

        static void Walk(JsonTypeInfo info, string prefix, HashSet<string> paths)
        {
            if (info.Kind == JsonTypeInfoKind.Enumerable)
            {
                Walk(TcgDexJsonContext.Default.GetTypeInfo(info.ElementType!)!, prefix + "[]", paths);
                return;
            }
            if (info.Kind != JsonTypeInfoKind.Object)
                return;

            foreach (var property in info.Properties)
            {
                var path = prefix.Length == 0 ? property.Name : $"{prefix}.{property.Name}";
                paths.Add(path);
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                Walk(TcgDexJsonContext.Default.GetTypeInfo(type)!, path, paths);
            }
        }
    }

    private static Dictionary<string, HashSet<string>> LoadBaseline() =>
        JsonSerializer.Deserialize<Dictionary<string, HashSet<string>>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "contract-baseline.json")))!;

    private static string SourceBaselinePath([CallerFilePath] string thisFile = "") =>
        Path.Combine(Path.GetDirectoryName(thisFile)!, "contract-baseline.json");
}
