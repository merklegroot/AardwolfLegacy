using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace reflection_lib
{
    public static class ReflectionUtil
    {
        public static U CloneToType<T, U>(T source)
               where T : class
               where U : class
        {
            if (source == null) { return null; }

            var sourceProps = typeof(T).GetProperties();
            var destProps = typeof(U).GetProperties();

            var dest = Activator.CreateInstance<U>();

            foreach (var sourceProp in sourceProps)
            {
                var matchingProp = destProps.SingleOrDefault(queryDestProp =>
                    string.Equals(sourceProp.Name, queryDestProp.Name)
                    && queryDestProp.SetMethod != null
                    && queryDestProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType));

                if (matchingProp == null) { continue; }
                var sourceValue = sourceProp.GetValue(source);
                matchingProp.SetValue(dest, sourceValue);
            }

            return dest;
        }
    }
}
