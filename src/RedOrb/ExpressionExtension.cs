using System.Linq.Expressions;
using System.Reflection;

namespace RedOrb;

public static class ExpressionExtension
{
	public static PredicateCondition ToCondition(this LambdaExpression lambda)
	{
		if (lambda.Body is BinaryExpression binary)
		{
			return binary.ToCondition();
		}

		throw new NotSupportedException("The provided LambdaExpression is not supported.");
	}

	internal static PredicateCondition ToCondition(this BinaryExpression binary)
	{
		if (binary.NodeType == ExpressionType.AndAlso && binary.Left is BinaryExpression left && binary.Right is BinaryExpression right)
		{
			var c = left.ToCondition();
			c.Add(right.ToCondition());
			return c;
		}

		if (binary.Left.NodeType == ExpressionType.MemberAccess && binary.Right.NodeType == ExpressionType.Constant)
		{
			var member = (MemberExpression)binary.Left;
			var parameter = (ParameterExpression)member.Expression!;
			var constant = (ConstantExpression)binary.Right;

			var c = new PredicateCondition()
			{
				ObjectType = parameter.Type,
				VariableName = parameter.Name!,
				PropertyName = member.Member.Name,
				PropertyType = constant.Type,
				Value = constant.Value,
			};
			return c;
		}

		if (binary.Left.NodeType == ExpressionType.Constant && binary.Right.NodeType == ExpressionType.MemberAccess)
		{
			var member = (MemberExpression)binary.Right;
			var parameter = (ParameterExpression)member.Expression!;
			var constant = (ConstantExpression)binary.Left;

			var c = new PredicateCondition()
			{
				ObjectType = parameter.Type,
				VariableName = parameter.Name!,
				PropertyName = member.Member.Name,
				PropertyType = constant.Type,
				Value = constant.Value,
			};
			return c;
		}

		if (binary.Left.NodeType == ExpressionType.MemberAccess && binary.Right.NodeType == ExpressionType.MemberAccess)
		{
			var left_member = (MemberExpression)binary.Left;
			var right_member = (MemberExpression)binary.Right;

			if (left_member.Expression!.NodeType == ExpressionType.Parameter && right_member.Expression!.NodeType == ExpressionType.MemberAccess)
			{
				var member = left_member;
				var parameter = (ParameterExpression)member.Expression!;

				var prop = (PropertyInfo)right_member.Member;
				var value = Expression.Lambda(right_member).Compile().DynamicInvoke();

				var c = new PredicateCondition()
				{
					ObjectType = parameter.Type,
					VariableName = parameter.Name!,
					PropertyName = member.Member.Name,
					PropertyType = prop.PropertyType,
					Value = value,
				};
				return c;
			}

			if (right_member.Expression!.NodeType == ExpressionType.Parameter && left_member.Expression!.NodeType == ExpressionType.MemberAccess)
			{
				var member = right_member;
				var parameter = (ParameterExpression)member.Expression!;

				var prop = (PropertyInfo)left_member.Member;
				var value = Expression.Lambda(left_member).Compile().DynamicInvoke();

				var c = new PredicateCondition()
				{
					ObjectType = parameter.Type,
					VariableName = parameter.Name!,
					PropertyName = member.Member.Name,
					PropertyType = prop.PropertyType,
					Value = value,
				};
				return c;
			}
		}

		throw new NotSupportedException("The provided BinaryExpression is not supported.");
	}
}