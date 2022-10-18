using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CardboardBox.Anime.Database.Generation
{
	public static class Extensions
	{
        //
        public static string ToSnakeCase(this string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length < 2) return text;

            var sb = new StringBuilder();
            sb.Append(char.ToLowerInvariant(text[0]));
            for (int i = 1; i < text.Length; ++i)
            {
                char c = text[i];
                if (!char.IsUpper(c))
                {
                    sb.Append(c);
                    continue;
                }

                sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }

            return sb.ToString();
        }
        //
        public static string ToPascalCase(this string? text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var chars = text.ToCharArray();
            chars[0] = char.ToLowerInvariant(chars[0]);
            return new string(chars);
        }

        public static PropertyInfo GetPropertyInfo<TSource, TProp>(Expression<Func<TSource, TProp>>? propertyLambda)
        {
            if (propertyLambda == null) throw new ArgumentNullException(nameof(propertyLambda));

            var type = typeof(TSource);

            if (propertyLambda.Body is not MemberExpression member)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda.ToString()));

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propertyLambda.ToString()));

            if (propInfo.ReflectedType != null &&
                type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a property that is not from type {1}.",
                    propertyLambda.ToString(),
                    type));

            return propInfo;
        }
    }
}
