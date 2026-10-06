using System;

namespace POS_BIGA.Models
{
    public class DuplicateProductException : Exception
    {
        public string Field { get; }
        public string Value { get; }

        public DuplicateProductException(string field, string value)
            : base($"يوجد منتج آخر بنفس  {field}: \"{value}\".")
        {
            Field = field;
            Value = value;
        }
    }
}
