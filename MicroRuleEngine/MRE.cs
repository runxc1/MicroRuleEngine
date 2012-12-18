using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MicroRuleEngine
{
    public class MRE
    {
        private readonly ExpressionType[] _nestedOperators = new[]
                                                                 {
                                                                     ExpressionType.And,
                                                                     ExpressionType.AndAlso,
                                                                     ExpressionType.Or,
                                                                     ExpressionType.OrElse
                                                                 };

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
            return Enum.TryParse(r.Operator, out nestedOperator) && _nestedOperators.Contains(nestedOperator) &&
                   r.Rules != null && r.Rules.Any()
                       ? BuildNestedExpression<T>(r.Rules, param, nestedOperator)
                       : BuildExpr<T>(r, param);
        }

        public Func<T, bool> CompileRules<T>(IList<Rule> rules)
        {
            var paramUser = Expression.Parameter(typeof(T));
            var expr = BuildNestedExpression<T>(rules, paramUser, ExpressionType.And);
            return Expression.Lambda<Func<T, bool>>(expr, paramUser).Compile();
        }

        Expression BuildNestedExpression<T>(IEnumerable<Rule> rules, ParameterExpression param, ExpressionType operation)
        {
            List<Expression> expressions = rules.Select(r => GetExpressionForRule<T>(r, param)).ToList();

            return BinaryExpression(expressions, operation);
        }

        static Expression BinaryExpression(IList<Expression> expressions, ExpressionType operationType)
        {
            Func<Expression, Expression, Expression> methodExp;
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
                    methodExp = Expression.And;
                    break;
            }

            if (expressions.Count == 1)
                return expressions[0];
            Expression exp = methodExp(expressions[0], expressions[1]);
            for (int i = 2; expressions.Count > i; i++)
            {
                exp = methodExp(exp, expressions[i]);
            }
            return exp;
        }

        Expression AndExpressions(IList<Expression> expressions)
        {
            if (expressions.Count == 1)
                return expressions[0];
            Expression exp = Expression.And(expressions[0], expressions[1]);
            for (int i = 2; expressions.Count > i; i++)
            {
                exp = Expression.And(exp, expressions[i]);
            }
            return exp;
        }

        Expression OrExpressions(IList<Expression> expressions)
        {
            if (expressions.Count == 1)
                return expressions[0];
            Expression exp = Expression.Or(expressions[0], expressions[1]);
            for (int i = 2; expressions.Count > i; i++)
            {
                exp = Expression.Or(exp, expressions[i]);
            }
            return exp;
        }

        static Expression BuildExpr<T>(Rule r, Expression param)
        {
            Expression propExpression = null;
            Type propType = null;

            ExpressionType tBinary;
            if (string.IsNullOrEmpty(r.MemberName)) //check is against the object itself
            {
                propExpression = param;
                propType = propExpression.Type;
            }
            else if (r.MemberName.Contains('.')) //Child property
            {
                String[] childProperties = r.MemberName.Split('.');
                var property = typeof(T).GetProperty(childProperties[0]);
                var paramExp = Expression.Parameter(typeof(T), "SomeObject");

                propExpression = Expression.PropertyOrField(param, childProperties[0]);
                for (int i = 1; i < childProperties.Length; i++)
                {
                    var orig = property;
                    property = property.PropertyType.GetProperty(childProperties[i]);
                    if (property != null)
                        propExpression = Expression.PropertyOrField(propExpression, childProperties[i]);
                }
                propType = propExpression.Type;
            }
            else //Property
            {
                propExpression = Expression.PropertyOrField(param, r.MemberName);
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
                                             new[]
                                                 {
                                                     typeof (string),
                                                     typeof (string),
                                                     typeof (RegexOptions)
                                                 }),
                    propExpression,
                    Expression.Constant(r.TargetValue, typeof(string)),
                    Expression.Constant(RegexOptions.IgnoreCase, typeof(RegexOptions))
                    );
            }
            //Invoke a method on the Property
            var inputs = r.Inputs.Select(x => x.GetType()).ToArray();
            var methodInfo = propType.GetMethod(r.Operator, inputs);
            if (!methodInfo.IsGenericMethod)
                inputs = null;//Only pass in type information to a Generic Method
            var expressions = r.Inputs.Select(Expression.Constant).ToArray();
            return Expression.Call(propExpression, r.Operator, inputs, expressions);
        }

        static Expression StringToExpression(string value, Type propType)
        {
            return value.ToLower() == "null"
                       ? Expression.Constant(null)
                       : Expression.Constant(propType.IsEnum
                                                 ? Enum.Parse(propType, value)
                                                 : Convert.ChangeType(value, propType));
        }
    }

    public class Rule
    {
        public Rule()
        {
            Inputs = new List<object>();
        }
        public string MemberName { get; set; }
        public string Operator { get; set; }
        public string TargetValue { get; set; }
        public List<Rule> Rules { get; set; }
        public List<object> Inputs { get; set; }
    }

    public class RuleValue<T>
    {
        public T Value { get; set; }
        public List<Rule> Rules { get; set; }
    }

    public class RuleValueString : RuleValue<string> { }
}
