namespace MediaLibrary.FormsClerk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class ReflectionUtilities
    {
        public static IEnumerable<Initializer> GetPublicInitializers(this Type type)
        {
            var staticProperties = from staticProperty in type.GetProperties(BindingFlags.Public | BindingFlags.Static)
                                   where staticProperty.PropertyType == type
                                   select new { order = 2, initializer = new Initializer(staticProperty.Name, _ => staticProperty.GetValue(null), Array.Empty<Parameter>()) };

            var constructors = from constructor in type.GetConstructors()
                               let parameters = constructor.GetParameters().Select(p => new Parameter(p)).ToList()
                               let name = parameters.Count == 0
                                   ? "Default Instance"
                                   : $"Specify {string.Join(", ", parameters.Select(p => p.DisplayName))}"
                               select new { order = parameters.Count == 0 ? 1 : 3, initializer = new Initializer(name, constructor.Invoke, parameters) };

            return staticProperties.Concat(constructors)
                .OrderBy(p => p.order)
                .Select(p => p.initializer);
        }
    }
}
