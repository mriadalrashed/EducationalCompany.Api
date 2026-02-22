namespace EducationalCompany.Api.Domain.Common
{
    // Base class for value objects (compared by value, not by Id)
    public abstract class ValueObject
    {
        // Handles == comparison between two value objects
        protected static bool EqualOperator(ValueObject left, ValueObject right)
        {
            if (left is null ^ right is null)
            {
                return false;
            }
            return left?.Equals(right) != false;
        }

        // Returns the properties that define equality
        protected abstract IEnumerable<object> GetEqualityComponents();

        // Compares two value objects based on their values
        public override bool Equals(object? obj)
        {
            if (obj is null || obj.GetType() != GetType())
            {
                return false;
            }
            var other = (ValueObject)obj;
            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        // Generates hash code based on equality components
        public override int GetHashCode()
        {
            return GetEqualityComponents()
             .Select(x => x != null ? x.GetHashCode() : 0)
             .Aggregate((x, y) => x ^ y);
        }

    }
}
