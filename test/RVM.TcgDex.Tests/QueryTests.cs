namespace RVM.TcgDex.Tests;

public sealed class QueryTests
{
    // Expected values come from the "Filtering, Sorting & Pagination" page of the TCGdex docs.
    public static TheoryData<Func<Query, Query>, string, string> Filters => new()
    {
        { q => q.Contains("name", "fu"), "name", "fu" },
        { q => q.Equal("name", "Furret"), "name", "eq:Furret" },
        { q => q.Not.Equal("name", "Furret"), "name", "neq:Furret" },
        { q => q.Not.Contains("name", "fu"), "name", "not:fu" },
        { q => q.GreaterThan("hp", 50), "hp", "gt:50" },
        { q => q.GreaterOrEqualThan("hp", 50), "hp", "gte:50" },
        { q => q.LesserThan("hp", 50), "hp", "lt:50" },
        { q => q.LesserOrEqualThan("hp", 50), "hp", "lte:50" },
        { q => q.IsNull("effect"), "effect", "null:" },
        { q => q.NotNull("effect"), "effect", "notnull:" },
        { q => q.Not.IsNull("effect"), "effect", "notnull:" },
    };

    [Theory]
    [MemberData(nameof(Filters))]
    public void Filter_UsesTheRestPrefix(Func<Query, Query> filter, string key, string value)
    {
        var query = filter(Query.Create());

        Assert.Equal([new(key, value)], query.Parameters);
    }

    [Fact]
    public void Sort_And_Paginate_AddTwoParametersEach()
    {
        var query = Query.Create().Sort("name", SortOrder.Desc).Paginate(3, 20);

        Assert.Equal(
            [new("sort:field", "name"), new("sort:order", "DESC"), new("pagination:page", "3"), new("pagination:itemsPerPage", "20")],
            query.Parameters);
    }

    [Fact]
    public void Sort_IsAscendingByDefault()
    {
        Assert.Contains(new KeyValuePair<string, string>("sort:order", "ASC"), Query.Create().Sort("name").Parameters);
    }

    [Fact]
    public void Numbers_IgnoreTheCurrentCulture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("pt-BR");
        try
        {
            Assert.Equal("gte:1.5", Query.Create().GreaterOrEqualThan("x", 1.5m).Parameters[0].Value);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void ToString_EncodesKeysAndValues_InInsertionOrder()
    {
        var query = Query.Create().Equal("name", "Furret|Pikachu").Sort("localId");

        Assert.Equal("?name=eq%3AFurret%7CPikachu&sort%3Afield=localId&sort%3Aorder=ASC", query.ToString());
    }

    [Fact]
    public void ToString_IsEmpty_WithoutParameters()
    {
        Assert.Equal(string.Empty, Query.Create().ToString());
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public void Paginate_RejectsNonPositiveValues(int page, int itemsPerPage)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Query.Create().Paginate(page, itemsPerPage));
    }

    [Fact]
    public void BlankKey_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => Query.Create().Equal(" ", "x"));
    }
}
