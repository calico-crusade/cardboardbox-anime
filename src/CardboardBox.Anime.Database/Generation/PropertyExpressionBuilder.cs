using System.Linq.Expressions;
using System.Reflection;

namespace CardboardBox.Anime.Database.Generation;

public interface IPropertyExpressionBuilder<T>
{
	IPropertyExpressionBuilder<T> With<TProp>(Expression<Func<T, TProp>> property);
}

public class PropertyExpressionBuilder<T> : IPropertyExpressionBuilder<T>
{
	public List<PropertyInfo> Properties { get; } = new();

	public IPropertyExpressionBuilder<T> With<TProp>(Expression<Func<T, TProp>> property)
	{
		Properties.Add(Extensions.GetPropertyInfo(property));
		return this;
	}
}
