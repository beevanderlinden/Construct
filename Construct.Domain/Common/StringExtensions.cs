namespace Construct.Domain.Common
{
    public static class StringExtensions
    {
        [Obsolete("Gebruik CONVERT en base64 voor conversie tussen string en byte[]")]
        public static byte[] ToUtf8BytesOrEmpty(this string? input)
        {
            if (string.IsNullOrEmpty(input))
                return Array.Empty<byte>();

            return System.Text.Encoding.UTF8.GetBytes(input);
        }

    }
}
