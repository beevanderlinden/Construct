namespace Construct.Domain.Common
{
    public static class EnumerableExtensions
    {

        public static string FormatGroupedProperty<T, TGroupKey>(
            this IEnumerable<T> items,
            Func<T, TGroupKey> groupBySelector,
            out int count,
            string separator = ", ",
            Func<TGroupKey, string>? groupKeyToString = null

            )
        {
            groupKeyToString ??= k => k?.ToString() ?? string.Empty;


            var grouped = items.GroupBy(groupBySelector).ToList();
            count = grouped.Count;


            if (grouped.Count == 0)
            {
                return "Geen data gevonden";
            }
            else if (grouped.Count == 1)
            {
                return groupKeyToString(grouped.First().Key);
            }
            else
            {
                var results = grouped.OrderBy(g => g.Key).Select(g => groupKeyToString(g.Key));
                return string.Join(separator, results);
            }
        }


        public static string FormatGroupedProperty<T, TGroupKey, TSubItem>(
    this IEnumerable<T> items,
    Func<T, TGroupKey> groupBySelector,
    Func<T, TSubItem>? subItemSelector = null,
    string separator = ", ",
    Func<TGroupKey, string>? groupKeyToString = null,
    Func<TSubItem, string>? subItemToString = null
)
        {
            groupKeyToString ??= k => k?.ToString() ?? string.Empty;
            subItemToString ??= s => s?.ToString() ?? string.Empty;

            var grouped = items.GroupBy(groupBySelector).ToList();

            if (grouped.Count == 0)
                return "Geen data gevonden";

            if (grouped.Count == 1)
                return groupKeyToString(grouped.First().Key);

            var results = grouped.Select(group =>
            {
                var groupKeyStr = groupKeyToString(group.Key);

                if (subItemSelector != null)
                {
                    var subItems = group.Select(subItemSelector).Distinct();
                    var subItemNames = string.Join(separator, subItems.Select(subItemToString));
                    return $"{groupKeyStr} ({subItemNames})";
                }
                else
                {
                    return groupKeyStr;
                }
            });

            return string.Join(separator, results);
        }

    }

}
