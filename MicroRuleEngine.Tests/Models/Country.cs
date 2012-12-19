namespace MicroRuleEngine.Tests.Models
{
    public class Country
        : IVisitable<Country>
    {
        public string CountryCode { get; set; }

        public void Accept(IVisitor<Country> visitor)
        {
            visitor.Visit(this);
        }

        public static Country Make(string countryCode)
        {
            return new Country
                       {
                           CountryCode = countryCode
                       };
        }
    }
}