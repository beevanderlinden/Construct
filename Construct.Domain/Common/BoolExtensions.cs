namespace Construct.Domain.Common
{
    public static class BoolExtensions
    {
        public static string ToEmoji(this bool value)
        {
            return value ? "✅" : "❌";
        }

        public static string ToEmoji(this bool value, string trueEmoji = "✅", string falseEmoji = "❌")
        {
            return value ? trueEmoji : falseEmoji;
        }

    }



}
