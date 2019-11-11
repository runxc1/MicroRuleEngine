using System;

namespace MicroRuleEngine
{
    public class DataRule : Rule
    {
        public string Type { get; set; }

        public static DataRule Create<T>(string member, MreOperator oper, T target)
        {
            return new DataRule
            {
                MemberName = member,
                TargetValue = target,
                Operator = oper.ToString(),
                Type = typeof(T).FullName
            };
        }

        public static DataRule Create<T>(string member, MreOperator oper, string target)
        {
            return new DataRule
            {
                MemberName = member,
                TargetValue = target,
                Operator = oper.ToString(),
                Type = typeof(T).FullName
            };
        }


        public static DataRule Create(string member, MreOperator oper, object target, Type memberType)
        {
            return new DataRule
            {
                MemberName = member,
                TargetValue = target,
                Operator = oper.ToString(),
                Type = memberType.FullName
            };
        }
    }
}