using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MicroRuleEngine
{
	public class MRE
	{
		private static readonly ExpressionType[] _nestedOperators = new ExpressionType[] { ExpressionType.And, ExpressionType.AndAlso, ExpressionType.Or, ExpressionType.OrElse };
		private static readonly Lazy<MethodInfo> _miRegexIsMatch = new Lazy<MethodInfo>(() => typeof(Regex).GetMethod("IsMatch", new[] { typeof(string), typeof(string), typeof(RegexOptions) }));
		private static readonly Lazy<MethodInfo> _miGetItem = new Lazy<MethodInfo>(() => typeof(System.Data.DataRow).GetMethod("get_Item", new Type[] { typeof(string) }));

		public bool PassesRules<T>(IList<Rule> rules, T toInspect)
		{
			return CompileRules<T>(rules).Invoke(toInspect);
		}

		public Func<T, bool> CompileRule<T>(Rule r)
		{
			var paramUser = Expression.Parameter(typeof(T));
			Expression expr = GetExpressionForRule<T>(r, paramUser);

			return Expression.Lambda<Func<T, bool>>(expr, paramUser).Compile();
		}

		Expression GetExpressionForRule<T>(Rule r, ParameterExpression param)
		{
			ExpressionType nestedOperator;
			if (ExpressionType.TryParse(r.Operator, out nestedOperator) && _nestedOperators.Contains(nestedOperator) && r.Rules != null && r.Rules.Any())
				return BuildNestedExpression<T>(r.Rules, param, nestedOperator);
			else
				return BuildExpr<T>(r, param);
		}

		public Func<T, bool> CompileRules<T>(IEnumerable<Rule> rules)
		{
			var paramUser = Expression.Parameter(typeof(T));
			var expr = BuildNestedExpression<T>(rules, paramUser, ExpressionType.And);
			return Expression.Lambda<Func<T, bool>>(expr, paramUser).Compile();
		}

		Expression BuildNestedExpression<T>(IEnumerable<Rule> rules, ParameterExpression param, ExpressionType operation)
		{
			var expressions = rules.Select(r => GetExpressionForRule<T>(r, param));
			return  BinaryExpression(expressions, operation);
		}

		Expression BinaryExpression(IEnumerable<Expression> expressions, ExpressionType operationType)
		{
			Func<Expression, Expression, Expression> methodExp = null;
			switch (operationType)
			{
				case ExpressionType.Or:
					methodExp = Expression.Or;
					break;
				case ExpressionType.OrElse:
					methodExp = Expression.OrElse;
					break;
				case ExpressionType.AndAlso:
					methodExp = Expression.AndAlso;
					break;
				default:
				case ExpressionType.And:
					methodExp = Expression.And;
					break;
			}
			return  expressions.ApplyOperation(methodExp);
			}
		Expression AndExpressions(IEnumerable<Expression> expressions)
		{
			return expressions.ApplyOperation(Expression.And);
		}

		Expression OrExpressions(IEnumerable<Expression> expressions)
		{
			return expressions.ApplyOperation(Expression.Or);
		}

		private static Expression GetProperty(Expression param, string propname)
		{
			Expression propExpression = param;
			String[] childProperties = propname.Split('.');
			var propertyType = param.Type;

			foreach(var chidprop in childProperties)
			{
				var property = propertyType.GetProperty(chidprop);
				if (property == null)
					throw new RulesException(String.Format("Cannot find property {0} in class {1} (\"{2}\")", chidprop, propertyType.Name, propname));
				propExpression = Expression.PropertyOrField(propExpression, chidprop);
				propertyType = property.PropertyType;
		}

			return propExpression;
		}
		private static Expression BuildExpr<T>(Rule r, Expression param)
		{
			Expression propExpression = null;
			Type propType = null;

			ExpressionType tBinary;
			var drule = r as DataRule;

			if (string.IsNullOrEmpty(r.MemberName)) //check is against the object itself
			{
				propExpression = param;
				propType = propExpression.Type;
			}
			else if (drule != null)
			{
				if (typeof(T) != typeof(System.Data.DataRow))
					throw new RulesException(" Bad rule");
				propExpression = GetDataRowField(param, drule.MemberName, drule.Type);
				propType = propExpression.Type;
			}
			else 
			{
				propExpression = GetProperty(param, r.MemberName);
				propType = propExpression.Type;
			}

			// is the operator a known .NET operator?
			if (Enum.TryParse(r.Operator, out tBinary))
				{
				var right = StringToExpression(r.TargetValue, propType);
				return Expression.MakeBinary(tBinary, propExpression, right);
				}
			if (r.Operator == "IsMatch")
			{
				return Expression.Call(
					typeof(Regex).GetMethod("IsMatch",
											new[] { typeof(string), typeof(string), typeof(RegexOptions) }),
					propExpression,
					Expression.Constant(r.TargetValue, typeof(string)),
					Expression.Constant(RegexOptions.IgnoreCase, typeof(RegexOptions))
				);
		   }
			else //Invoke a method on the Property
			{
				var inputs = r.Inputs.Select(x => x.GetType()).ToArray();
				var methodInfo = propType.GetMethod(r.Operator, inputs);
				if (!methodInfo.IsGenericMethod)
				inputs = null;//Only pass in type information to a Generic Method
			var expressions = r.Inputs.Select(Expression.Constant).ToArray();
				return Expression.Call(propExpression, r.Operator, inputs, expressions);
			}
		}

		private static Expression GetDataRowField(Expression prm, string member, string type)
		{
			return
					Expression.Convert(
						Expression.Call(prm, _miGetItem.Value, Expression.Constant(member, typeof(string)))
					, Type.GetType(type));
		}

		private static Expression StringToExpression(string value, Type propType)
		{
			ConstantExpression right = null;

			if (value == null || value.ToLower() == "null")
			{
				right = Expression.Constant(null);
			}
			else
			{
				right = Expression.Constant(Convert.ChangeType(value, propType), propType);
			}
			return right;
		}
	}

	public class Rule
	{
		public Rule()
		{
			Inputs = Enumerable.Empty<object>();
		}
		public string MemberName { get; set; }
		public string Operator { get; set; }
		public string TargetValue { get; set; }
		public List<Rule> Rules { get; set; }
		public IEnumerable<object> Inputs { get; set; }


		public static Rule operator|(Rule lhs, Rule rhs)
		{
			var rule = new Rule { Operator = "Or" };
			return mergeRulesInto(rule, lhs, rhs);
	}
		public static Rule operator&(Rule lhs, Rule rhs)
		{
			var rule = new Rule { Operator = "AndAlso" };
			return mergeRulesInto(rule, lhs, rhs);
		}

		private static Rule mergeRulesInto(Rule target, Rule lhs, Rule rhs)
		{
			target.Rules = new List<Rule>();

			if (lhs.Rules != null  && lhs.Operator == target.Operator)         // left is multiple
			{
				target.Rules.AddRange(lhs.Rules);
				if (rhs.Rules != null && rhs.Operator == target.Operator)
					target.Rules.AddRange(rhs.Rules);     // left & right are multiple
				else
					target.Rules.Add(rhs);                // left multi, right single
			}
			else if (rhs.Rules != null && rhs.Operator == target.Operator)
			{
				target.Rules.Add(lhs);                    // left single, right multi
				target.Rules.AddRange(rhs.Rules);
			}
			else
			{
				target.Rules.Add(lhs);
				target.Rules.Add(rhs);
			}


			return target;
		}

		public static Rule Create(string member, mreOperator oper, string target )
		{
			return new Rule { MemberName = member, TargetValue = target, Operator = oper.ToString() };
		}

		public static Rule MethodOnChild(string member, string methodName, params object[] inputs )
		{
			return new Rule { MemberName = member,  Inputs = inputs.ToList(), Operator = methodName};
		}

		public static Rule Method(string methodName, params object[] inputs )
		{
			return new Rule {Inputs = inputs.ToList(), Operator = methodName};
		}

		public override string ToString()
		{
			return $"{MemberName} {Operator} {TargetValue}";
		}
	}

	public class DataRule : Rule
	{
		public string Type { get; set; }

		public static DataRule Create<T>(string member, mreOperator oper, string target)
		{
			return new DataRule { MemberName = member, TargetValue = target, Operator = oper.ToString(), Type = typeof(T).FullName };
		}
	}

	// Nothing specific to MRE.  Can be moved to a more common location
	public static class Extensions
	{
		public static TOperand ApplyOperation<TOperand, TReturn>(this IEnumerable<TOperand> source, Func<TOperand, TOperand, TReturn> oper)
			where TReturn : TOperand
		{
			var iter = source.GetEnumerator();
			var more = iter.MoveNext();
			if (!more)
				throw new ArgumentOutOfRangeException("source", "Collection must have at least one item");

			var lhs = iter.Current;
			while (more = iter.MoveNext())
			{
				lhs = oper(lhs, iter.Current);
			}

			return lhs;
		}
	}

	public class RulesException : ApplicationException
	{
		public RulesException() : base()
		{
		}

		public RulesException(string message) :base(message)
		{
		}

		public RulesException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}



	//
	// Summary:
	//     Describes the node types for the nodes of an expression tree.
	public enum mreOperator
	{
		//
		// Summary:
		//     An addition operation, such as a + b, without overflow checking, for numeric
		//     operands.
		Add = 0,
		//
		// Summary:
		//     A bitwise or logical AND operation, such as (a & b) in C# and (a And b) in Visual
		//     Basic.
		And = 2,
		//
		// Summary:
		//     A conditional AND operation that evaluates the second operand only if the first
		//     operand evaluates to true. It corresponds to (a && b) in C# and (a AndAlso b)
		//     in Visual Basic.
		AndAlso = 3,
		//
		// Summary:
		//     A node that represents an equality comparison, such as (a == b) in C# or (a =
		//     b) in Visual Basic.
		Equal = 13,
		//
		// Summary:
		//     A "greater than" comparison, such as (a > b).
		GreaterThan = 15,
		//
		// Summary:
		//     A "greater than or equal to" comparison, such as (a >= b).
		GreaterThanOrEqual = 16,
		//
		// Summary:
		//     A "less than" comparison, such as (a < b).
		LessThan = 20,
		//
		// Summary:
		//     A "less than or equal to" comparison, such as (a <= b).
		LessThanOrEqual = 21,
		//
		// Summary:
		//     An inequality comparison, such as (a != b) in C# or (a <> b) in Visual Basic.
		NotEqual = 35,
		//
		// Summary:
		//     A bitwise or logical OR operation, such as (a | b) in C# or (a Or b) in Visual
		//     Basic.
		Or = 36,
		//
		// Summary:
		//     A short-circuiting conditional OR operation, such as (a || b) in C# or (a OrElse
		//     b) in Visual Basic.
		OrElse = 37,

		IsMatch
	}


	public class RuleValue<T>
	{
		public T Value { get; set; }
		public List<Rule> Rules { get; set; }
	}

	public class RuleValueString : RuleValue<string> { }
}
