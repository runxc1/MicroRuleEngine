using System.Collections.Generic;

namespace MicroRuleEngine
{
    public class RuleValue<T>
    {
        public T Value { get; set; }
        public List<Rule> Rules { get; set; }
    }
}