namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System;
    using Orleans;

    public class GrainKeyMatcher
    {
        private enum KeyType
        {
            Guid,
            String,
            Integer
        }

        private static KeyType DetermineKeyType(object source)
        {
            var isGuidKey = source is IGrainWithGuidKey;
            var isStringKey = source is IGrainWithStringKey;
            var isIntKey = source is IGrainWithIntegerKey;

            if (isGuidKey) return KeyType.Guid;
            if (isStringKey) return KeyType.String;
            if (isIntKey) return KeyType.Integer;

            throw new UnableToDetermineKeyTypeException($"Unable to determine the type of primary key for {source.GetType().Name}");
        }

        public static bool KeyMatches(object source, string grainKey, string identityString)
        {
            switch (DetermineKeyType(source))
            {
                case KeyType.Guid:
                {
                    var key = identityString.Split('/')[2].Split('-')[0];

                    if (key == grainKey.Split('=')[1])
                    {
                        return true;
                    }
                    break;
                }
                case KeyType.String:
                {
                    var key = identityString.Split('/')[2];
                    key = key.Substring(0, key.LastIndexOf("-", StringComparison.InvariantCultureIgnoreCase));
                    //key = key.Split('+')[1];

                    //var targetGrainKey = grainKey.Split('+')[1];
                    var targetGrainKey = grainKey.Split('=')[1];

                    if (key == targetGrainKey)
                    {
                        return true;
                    }
                    break;
                }
                case KeyType.Integer:
                {
                    break;
                }
            }

            return false;
        }
    }
}
