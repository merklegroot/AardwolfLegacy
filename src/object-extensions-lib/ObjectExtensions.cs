using System;
using System.Linq;

namespace object_extensions_lib
{
    public static class ObjectExtensions
    {
        public static T CloneAs<T>(this object source)
        {
            if (source == null) { return default(T); }

            var dest = Activator.CreateInstance<T>();
            var sourceProps = source.GetType().GetProperties().ToList();
            var destProps = typeof(T).GetProperties().ToList();

            foreach (var sourceProp in sourceProps)
            {
                // first try to do an exact match. if that fails, ignore case.
                var match = destProps
                    .FirstOrDefault(queryDestProp => string.Equals(queryDestProp.Name, sourceProp.Name, StringComparison.Ordinal))
                    ?? destProps.FirstOrDefault(queryDestProp => string.Equals(queryDestProp.Name, sourceProp.Name, StringComparison.InvariantCultureIgnoreCase)); ;

                if (match == null) { continue; }
                match.SetValue(dest, sourceProp.GetValue(source));
            }

            return dest;
        }
    }
}
