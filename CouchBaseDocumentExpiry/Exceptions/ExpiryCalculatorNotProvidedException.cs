namespace CouchBaseDocumentExpiry.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class ExpiryCalculatorNotProvidedException : Exception
    {
        public ExpiryCalculatorNotProvidedException()
        {
        }

        public ExpiryCalculatorNotProvidedException(string message) : base(message)
        {
        }

        public ExpiryCalculatorNotProvidedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExpiryCalculatorNotProvidedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}