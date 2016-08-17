using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MicroRuleEngine
{
    internal static class ExpressionBuilder
    {
        private const string StrIsMatch = "IsMatch";

        private const string StrNull = "null";

        private static readonly ExpressionType[] NestedOperators =
        {
            ExpressionType.And,
            ExpressionType.AndAlso,
            ExpressionType.Or,
            ExpressionType.OrElse
        };

        public static Expression Build<T>(Rule rule, ParameterExpression parameterExpression)
        {
            ExpressionType nestedOperator;
            return Enum.TryParse(rule.Operator, out nestedOperator) &&
                    NestedOperators.Contains(nestedOperator) &&
                    rule.Rules != null &&
                    rule.Rules.Any()
                       ? Build<T>(rule.Rules, parameterExpression, nestedOperator)
                       : BuildExpression<T>(rule, parameterExpression);
        }

        public static Expression Build<T>(IEnumerable<Rule> rules, ParameterExpression parameterExpression, ExpressionType operation)
        {
            var expressions = rules.Select(r => Build<T>(r, parameterExpression));

            return Build(expressions, operation);
        }



        private static Expression Build(IEnumerable<Expression> expressions, ExpressionType operationType)
        {
            Func<Expression, Expression, Expression> expressionAggregateMethod;
            switch (operationType)
            {
                case ExpressionType.Or:
                    expressionAggregateMethod = Expression.Or;
                    break;
                case ExpressionType.OrElse:
                    expressionAggregateMethod = Expression.OrElse;
                    break;
                case ExpressionType.AndAlso:
                    expressionAggregateMethod = Expression.AndAlso;
                    break;
                default:
                    expressionAggregateMethod = Expression.And;
                    break;
            }

            return BuildExpression(expressions, expressionAggregateMethod);
        }

        private static Expression BuildExpression(IEnumerable<Expression> expressions, Func<Expression, Expression, Expression> expressionAggregateMethod)
        {
            return expressions.Aggregate<Expression, Expression>(null,
                (current, expression) => current == null
                    ? expression
                    : expressionAggregateMethod(current, expression)
            );
        }

        private static Expression BuildExpression<T>(Rule rule, Expression expression)
        {
            Expression propExpression;
            Type propType;
            if (string.IsNullOrEmpty(rule.MemberName)) //check is against the object itself
            {
                propExpression = expression;
                propType = propExpression.Type;
            }
            else if (rule.MemberName.Contains('.')) //Child property
            {
                var childProperties = rule.MemberName.Split('.');
                var property = typeof(T).GetProperty(childProperties[0]);
                // not being used?
                // ParameterExpression paramExp = Expression.Parameter(typeof(T), "SomeObject");

                propExpression = Expression.PropertyOrField(expression, childProperties[0]);
                for (var i = 1; i < childProperties.Length; i++)
                {
                    // not being used?
                    // PropertyInfo orig = property;
                    if (property == null) continue;
                    property = property.PropertyType.GetProperty(childProperties[i]);
                    if (property == null) continue;
                    propExpression = Expression.PropertyOrField(propExpression, childProperties[i]);
                }
                propType = propExpression.Type;
            }
            else //Property
            {
                propExpression = Expression.PropertyOrField(expression, rule.MemberName);
                propType = propExpression.Type;
            }

            ExpressionType tBinary;
            // is the operator a known .NET operator?
            if (Enum.TryParse(rule.Operator, out tBinary))
            {
                var right = StringToExpression(rule.TargetValue, propType);
                return Expression.MakeBinary(tBinary, propExpression, right);
            }
            if (rule.Operator == StrIsMatch)
            {
                return Expression.Call(
                    typeof(Regex).GetMethod(StrIsMatch,
                        new[]
                        {
                            typeof (string),
                            typeof (string),
                            typeof (RegexOptions)
                        }
                    ),
                    propExpression,
                    Expression.Constant(rule.TargetValue, typeof(string)),
                    Expression.Constant(RegexOptions.IgnoreCase, typeof(RegexOptions))
                    );
            }
            //Invoke a method on the Property
            var inputs = rule.Inputs.Select(x => x.GetType()).ToArray();
            var methodInfo = propType.GetMethod(rule.Operator, inputs);
            if (!methodInfo.IsGenericMethod)
                inputs = null; //Only pass in type information to a Generic Method
            var expressions = rule.Inputs.Select(Expression.Constant).ToArray();
            return Expression.Call(propExpression, rule.Operator, inputs, expressions);
        }

        private static Expression StringToExpression(string value, Type propType)
        {
            return value.ToLower() == StrNull
                ? Expression.Constant(null)
                : Expression.Constant(propType.IsEnum
                    ? Enum.Parse(propType, value)
                    : Convert.ChangeType(value, propType));
        }
    }
}
