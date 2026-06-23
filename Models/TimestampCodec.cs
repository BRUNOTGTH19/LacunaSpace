using System.Text;

namespace LacunaSpace.Models
{
    public static class TimestampCodec
    {
        public static long Decode(string encoded, string encoding)
        {
            return encoding switch
            {
                "Iso8601" => DateTimeOffset.Parse(encoded).Ticks,
                "Ticks" => long.Parse(encoded),
                "TicksBinary" => BitConverter.ToInt64(Convert.FromBase64String(encoded)),
                "TicksBinaryBigEndian" => BitConverter.ToInt64(Convert.FromBase64String(encoded).Reverse().ToArray()),
                _ => throw new ArgumentException($"Unknown encoding: {encoding}")
            };
        }

        public static string Encode(long ticks, string encoding)
        {
            return encoding switch
            {
                "Iso8601" => new DateTimeOffset(ticks, TimeSpan.Zero).ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz"),
                "Ticks" => ticks.ToString(),
                "TicksBinary" => Convert.ToBase64String(BitConverter.GetBytes(ticks)),
                "TicksBinaryBigEndian" => Convert.ToBase64String(BitConverter.GetBytes(ticks).Reverse().ToArray()),
                _ => throw new ArgumentException($"Unknown encoding: {encoding}")
            };
        }
    }
}