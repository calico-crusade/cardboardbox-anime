using System.Data;

namespace CardboardBox.Anime.Bot.Services
{
    public class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        private static readonly DateTime unixOrigin = new(1970, 1, 1, 0, 0, 0, 0);

        public override DateTime Parse(object value)
        {
            return value switch
            {
                DateTime time => time,
                string str => DateTime.TryParse(str, out var res) ? res : DateTime.MinValue,
                long val => unixOrigin.AddSeconds(val),
                int val => unixOrigin.AddSeconds(val),
                float val => unixOrigin.AddSeconds((long)val),
                _ => DateTime.MinValue
            };
        }

        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = value.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }

    public class NullableDateTimeHandler : SqlMapper.TypeHandler<DateTime?>
    {
        private static readonly DateTime unixOrigin = new(1970, 1, 1, 0, 0, 0, 0);

        public override DateTime? Parse(object value)
        {
            if (value == null) return null;

            return value switch
            {
                DateTime time => time,
                string str => DateTime.TryParse(str, out var res) ? res : null,
                long val => unixOrigin.AddSeconds(val),
                int val => unixOrigin.AddSeconds(val),
                float val => unixOrigin.AddSeconds((long)val),
                _ => null
            };
        }

        public override void SetValue(IDbDataParameter parameter, DateTime? value)
        {
            parameter.Value = value?.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }
}
