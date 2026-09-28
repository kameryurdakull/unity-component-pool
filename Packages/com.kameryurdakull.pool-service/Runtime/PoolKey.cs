using System;

namespace Pooling
{
    public static class PoolKey
    {
        private const uint OffsetBasis = 2166136261;
        private const uint Prime = 16777619;

        public static int FromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Pool name cannot be empty.", nameof(name));
            }

            var hash = OffsetBasis;
            for (var index = 0; index < name.Length; index++)
            {
                hash = unchecked((hash ^ name[index]) * Prime);
            }

            return unchecked((int)hash);
        }
    }
}
