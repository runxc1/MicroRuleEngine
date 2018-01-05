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
        private ExpressionType[] nestedOperators = new ExpressionType[] { ExpressionType.And, ExpressionType.AndAlso, ExpressionType.Or, ExpressionType.OrElse };

        private static readonly MethodInfo funcRegexIsMatch = typeof(Regex).GetMethod("IsMatch",  new[] { typeof(string), typeof(string), typeof(RegexOptions) });

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
            if (ExpressionType.TryParse(r.Operator, out nestedOperator) && nestedOperators.Contains(nestedOperator) && r.Rules != null && r.Rules.Any())
                return BuildNestedExpression<T>(r.Rules, param, nestedOperator);
            else
                return BuildExpr<T>(r, param);
        }

        public Func<T, bool> CompileRules<T>(IList<Rule> rules)
        {
            var paramUser = Expression.Parameter(typeof(T));
            var expr = BuildNestedExpression<T>(rules, paramUser, ExpressionType.And);
            return Expression.Lambda<Func<T, bool>>(expr, paramUser).Compile();
        }

        Expression BuildNestedExpression<T>(IList<Rule> rules, ParameterExpression param, ExpressionType operation)
        {
            List<Expression> expressions = new List<Expression>(rules.Count);
            foreach (var r in rules)
            {
                expressions.Add(GetExpressionForRule<T>(r,param));
            }

            Expression expr = BinaryExpression(expressions, operation);
            return expr;
        }

        Expression BinaryExpression(IList<Expression> expressions, ExpressionType operationType)
        {
            Func<Expression, Expression, Expression> methodExp = (x1, x2) => Expression.And(x1, x2);
            switch (operationType)
            {
                case ExpressionType.Or:
                    methodExp = (x1, x2) => Expression.Or(x1, x2);
                    break;
                case ExpressionType.OrElse:
                    methodExp = (x1, x2) => Expression.OrElse(x1, x2);
                    break;
                case ExpressionType.AndAlso:
                    methodExp = (x1, x2) => Expression.AndAlso(x1, x2);
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

        Expression BuildExpr<T>(Rule r, ParameterExpression param)
        {
            Expression propExpression = null;
            Type propType = null;

            ExpressionType tBinary;
            if (string.IsNullOrEmpty(r.MemberName))//check is against the object itself
            {
                propExpression = param;
                propType = propExpression.Type;
            }
            else if (r.MemberName.Contains('.'))//Child property
            {
                String[] childProperties = r.MemberName.Split('.');
                var property = typeof(T).GetProperty(childProperties[0]);
//                var paramExp = Expression.Parameter(typeof(T), "SomeObject");

                propExpression = Expression.PropertyOrField(param, childProperties[0]);
                for (int i = 1; i < childProperties.Length; i++)
                {
//                    var orig = property;
                    property = property.PropertyType.GetProperty(childProperties[i]);
                    if (property != null)
                        propExpression = Expression.PropertyOrField(propExpression, childProperties[i]);
                }
                propType = propExpression.Type;
            }
            else//Property
            {
                propExpression = Expression.PropertyOrField(param, r.MemberName);
                propType = propExpression.Type;
            }

            // is the operator a known .NET operator?
            if (ExpressionType.TryParse(r.Operator, out tBinary))
            {
                var right = this.StringToExpression(r.TargetValue, propType);
                return Expression.MakeBinary(tBinary, propExpression, right);
            }
            else if (r.Operator == "IsMatch")
            {
                return Expression.Call(funcRegexIsMatch,
                    propExpression,
                    Expression.Constant(r.TargetValue, typeof(string)),
                    Expression.Constant(RegexOptions.IgnoreCase, typeof(RegexOptions))
                );
            }
            else //Invoke a method on the Property
            {
                var inputs = r.Inputs.Select(x=> x.GetType()).ToArray();
                var methodInfo = propType.GetMethod(r.Operator, inputs);
                if (!methodInfo.IsGenericMethod)
                    inputs = null;//Only pass in type information to a Generic Method
                var expressions = r.Inputs.Select(x => Expression.Constant(x)).ToArray();
                return Expression.Call(propExpression, r.Operator,inputs,expressions);
            }
        }

        private Expression StringToExpression(string value, Type propType)
        {
            ConstantExpression right = null;
            if (value.ToLower() == "null")
            {
                right = Expression.Constant(null);
            }
            else
            {
                right = Expression.Constant(Convert.ChangeType(value, propType));
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

        public static Rule MethodOnChild(string member, string oper, params object[] inputs )
        {
            return new Rule { MemberName = member,  Inputs = inputs.ToList(), Operator = oper};
        }

        public static Rule Method(string oper, params object[] inputs )
        {
            return new Rule {Inputs = inputs.ToList(), Operator = oper};
        }

        public override string ToString()
        {
            return $"{MemberName} {Operator} {TargetValue}";
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
