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
			return CreatePredicateCondition_Member_Constant((MemberExpression)binary.Left, (ConstantExpression)binary.Right);
		}

		if (binary.Left.NodeType == ExpressionType.Constant && binary.Right.NodeType == ExpressionType.MemberAccess)
		{
			return CreatePredicateCondition_Member_Constant((MemberExpression)binary.Right, (ConstantExpression)binary.Left);
		}

		if (binary.Left.NodeType == ExpressionType.MemberAccess && binary.Right.NodeType == ExpressionType.MemberAccess)
		{
			var left_member = (MemberExpression)binary.Left;
			var right_member = (MemberExpression)binary.Right;

			if (left_member.Expression!.NodeType == ExpressionType.Parameter && right_member.Expression!.NodeType == ExpressionType.MemberAccess)
			{
				return CreatePredicateCondition_MemberParameter_Member(left_member, right_member);
			}

			if (right_member.Expression!.NodeType == ExpressionType.Parameter && left_member.Expression!.NodeType == ExpressionType.MemberAccess)
			{
				return CreatePredicateCondition_MemberParameter_Member(right_member, left_member);
			}

			if (left_member.Expression!.NodeType == ExpressionType.Parameter && right_member.Expression!.NodeType == ExpressionType.Constant)
			{
				return CreatePredicateCondition_MemberParameter_Member(left_member, right_member);
			}

			if (right_member.Expression!.NodeType == ExpressionType.Parameter && left_member.Expression!.NodeType == ExpressionType.Constant)
			{
				return CreatePredicateCondition_MemberParameter_Member(right_member, left_member);
			}
		}

		throw new NotSupportedException("The provided BinaryExpression is not supported.");
	}

	private static PredicateCondition CreatePredicateCondition_Member_Constant(MemberExpression member, ConstantExpression constant)
	{
		var parameter = (ParameterExpression)member.Expression!;

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

	private static PredicateCondition CreatePredicateCondition_MemberParameter_Member(MemberExpression memberParameter, MemberExpression memberMember)
	{
		var innerParameter = (ParameterExpression)memberParameter.Expression!;
		var prop = (PropertyInfo)memberParameter.Member;

		var value = Expression.Lambda(memberMember).Compile().DynamicInvoke();

		var c = new PredicateCondition()
		{
			ObjectType = innerParameter.Type,
			VariableName = innerParameter.Name!,
			PropertyName = prop.Name,
			PropertyType = prop.PropertyType,
			Value = value,
		};
		return c;
	}
}