using System.Collections.Generic;

namespace MicroRuleEngine
{
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
}