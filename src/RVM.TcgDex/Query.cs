using System.Globalization;
using System.Text;

namespace RVM.TcgDex;

/// <summary>Sort direction for <see cref="Query.Sort"/>.</summary>
public enum SortOrder
{
    /// <summary>Ascending (<c>ASC</c>).</summary>
    Asc,
    /// <summary>Descending (<c>DESC</c>).</summary>
    Desc,
}

/// <summary>
/// Filters, sorting and pagination for list endpoints, translated to the REST filter syntax
/// (<c>name=eq:Furret</c>, <c>hp=gte:100</c>, <c>sort:field=name</c>…). Same shape as the
/// <c>Query</c> of the official SDKs:
/// <code>
/// var cards = await tcgdex.Card.ListAsync(Query.Create().Equal("name", "Furret").Sort("localId"));
/// </code>
/// </summary>
public sealed class Query
{
    private readonly List<KeyValuePair<string, string>> _parameters = [];

    /// <summary>Creates an empty query. <see cref="Create"/> reads better in a chain.</summary>
    public Query() => Not = new NotFilters(this);

    /// <summary>Creates an empty query.</summary>
    public static Query Create() => new();

    /// <summary>The query-string parameters, in the order they were added.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Parameters => _parameters;

    /// <summary>Negated filters: <c>Query.Create().Not.Equal("rarity", "Common")</c>.</summary>
    public NotFilters Not { get; }

    /// <summary>
    /// Case-insensitive "contains" match — the API default. <c>*</c> anchors the start or end:
    /// <c>"*chu"</c> matches Pikachu but not Pikachu on the Ball.
    /// </summary>
    public Query Contains(string key, string value) => Add(key, value);

    /// <summary>Exact, case-sensitive match. Several values: <c>"Furret|Pikachu"</c>.</summary>
    public Query Equal(string key, string value) => Add(key, "eq:" + value);

    /// <summary>Numeric field strictly greater than <paramref name="value"/>.</summary>
    public Query GreaterThan(string key, decimal value) => Add(key, "gt:" + Number(value));

    /// <summary>Numeric field greater than or equal to <paramref name="value"/>.</summary>
    public Query GreaterOrEqualThan(string key, decimal value) => Add(key, "gte:" + Number(value));

    /// <summary>Numeric field strictly lesser than <paramref name="value"/>.</summary>
    public Query LesserThan(string key, decimal value) => Add(key, "lt:" + Number(value));

    /// <summary>Numeric field lesser than or equal to <paramref name="value"/>.</summary>
    public Query LesserOrEqualThan(string key, decimal value) => Add(key, "lte:" + Number(value));

    /// <summary>Field has no value (e.g. cards without effect).</summary>
    public Query IsNull(string key) => Add(key, "null:");

    /// <summary>Field has a value.</summary>
    public Query NotNull(string key) => Add(key, "notnull:");

    /// <summary>Sorts by a field of the returned objects. API default: releaseDate, localId, id.</summary>
    public Query Sort(string field, SortOrder order = SortOrder.Asc)
    {
        Add("sort:field", field);
        return Add("sort:order", order == SortOrder.Desc ? "DESC" : "ASC");
    }

    /// <summary>Returns only one page. Pages start at 1.</summary>
    public Query Paginate(int page, int itemsPerPage)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(itemsPerPage);

        Add("pagination:page", page.ToString(CultureInfo.InvariantCulture));
        return Add("pagination:itemsPerPage", itemsPerPage.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary><c>?key=value&amp;…</c>, URL-encoded; empty string when there is no parameter.</summary>
    public override string ToString()
    {
        if (_parameters.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var (key, value) in _parameters)
        {
            sb.Append(sb.Length == 0 ? '?' : '&')
              .Append(Uri.EscapeDataString(key))
              .Append('=')
              .Append(Uri.EscapeDataString(value));
        }
        return sb.ToString();
    }

    private Query Add(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        _parameters.Add(new(key, value));
        return this;
    }

    private static string Number(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Negated filters, reached through <see cref="Not"/>.</summary>
    public sealed class NotFilters
    {
        private readonly Query _query;

        internal NotFilters(Query query) => _query = query;

        /// <summary>Field is not exactly <paramref name="value"/>.</summary>
        public Query Equal(string key, string value) => _query.Add(key, "neq:" + value);

        /// <summary>Field does not contain <paramref name="value"/> (case-insensitive).</summary>
        public Query Contains(string key, string value) => _query.Add(key, "not:" + value);

        /// <summary>Field has a value — same as <see cref="Query.NotNull"/>.</summary>
        public Query IsNull(string key) => _query.NotNull(key);
    }
}
