using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace SFMA_API.Logger
{
    public class PropertyBagEnricher : ILogEventEnricher
    {
        private readonly Dictionary<string, Tuple<object, bool>> _properties;

        public PropertyBagEnricher()
        {
            _properties = new Dictionary<string, Tuple<object, bool>>(StringComparer.OrdinalIgnoreCase);
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            foreach (KeyValuePair<string, Tuple<object, bool>> prop in _properties)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(prop.Key, prop.Value.Item1, prop.Value.Item2));
            }
        }

        public PropertyBagEnricher Add(string key, object value, bool destructureObject = false)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (!_properties.ContainsKey(key)) _properties.Add(key, Tuple.Create(value, destructureObject));
            return this;
        }
    }
}
