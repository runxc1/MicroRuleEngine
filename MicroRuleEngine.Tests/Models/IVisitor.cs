namespace MicroRuleEngine.Tests.Models
{
    public interface IVisitor<in T>
    {
        void Visit(T element);
    }
}