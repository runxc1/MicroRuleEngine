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
    }
}