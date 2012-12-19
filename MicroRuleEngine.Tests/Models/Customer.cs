namespace MicroRuleEngine.Tests.Models
{
    public class Customer
        : IVisitable<Customer>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Country Country { get; set; }

        public void Accept(IVisitor<Customer> visitor)
        {
            visitor.Visit(this);
        }

        public static Customer Make(string firstName, string lastName, string countryCode)
        {
            return new Customer
                       {
                           FirstName = firstName,
                           LastName = lastName,
                           Country = Country.Make(countryCode)
                       };
        }
    }
}