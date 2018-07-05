namespace CouchBaseDocumentExpiry.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class UnableToDeterminePrimaryKeyException : Exception
    {
        public UnableToDeterminePrimaryKeyException()
        {
        }

        public UnableToDeterminePrimaryKeyException(string message) : base(message)
        {
        }

        public UnableToDeterminePrimaryKeyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnableToDeterminePrimaryKeyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}