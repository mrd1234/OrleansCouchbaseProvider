namespace CouchBaseDocumentExpiry.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class MultipleExpiryCalculatorsForGrainTypeException : Exception
    {
        public MultipleExpiryCalculatorsForGrainTypeException()
        {
        }

        public MultipleExpiryCalculatorsForGrainTypeException(string message) : base(message)
        {
        }

        public MultipleExpiryCalculatorsForGrainTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MultipleExpiryCalculatorsForGrainTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}