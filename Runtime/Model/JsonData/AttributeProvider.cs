using Backtrace.Unity.Model.Attributes;
using Backtrace.Unity.Services;
using System;
using System.Collections.Generic;

namespace Backtrace.Unity.Model.JsonData
{
    public sealed class AttributeProvider
    {
        /// <summary>
        /// Application version attribute
        /// </summary>
        public string ApplicationVersion
        {
            get
            {
                return this["application.version"];
            }
        }

        /// <summary>
        /// Application session key
        /// </summary>
        public string ApplicationSessionKey
        {
            get
            {
                string result;
                if (_attributes.TryGetValue(BacktraceMetrics.ApplicationSessionKey, out result))
                {
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// Application machine identifier
        /// </summary>
        public string ApplicationGuid
        {
            get
            {
                return this["guid"];
            }
        }

        private readonly IDictionary<string, string> _attributes = new Dictionary<string, string>();
        private readonly IList<IDynamicAttributeProvider> _dynamicAttributeProvider;

        /// <summary>
        /// Initialize new Attribute provider class with default library attribute providers
        /// </summary>
        internal AttributeProvider() : this(
            scopeAttributeProvider: new List<IScopeAttributeProvider>()
            {
                new MachineAttributeProvider(),
                new RuntimeAttributeProvider(),
                new PiiAttributeProvider()
            },
            dynamicAttributeProvider: new List<IDynamicAttributeProvider>()
            {
                new MachineStateAttributeProvider(),
                new ProcessAttributeProvider(),
                new SceneAttributeProvider(),
            })
        { }

        /// <summary>
        /// Initialize new Attribute provider class with defined attribute providers
        /// </summary>
        /// <param name="scopeAttributeProvider">Attribute providers that generated scoped attributes (scoped = attributes that don't change over the time)</param>
        /// <param name="dynamicAttributeProvider">Attribute providers that generated dynamic attributes (that can change over game lifecycle)</param>
        internal AttributeProvider(
            IEnumerable<IScopeAttributeProvider> scopeAttributeProvider,
            IList<IDynamicAttributeProvider> dynamicAttributeProvider)
        {
            if (scopeAttributeProvider == null)
            {
                throw new ArgumentException("Scoped attributes provider collection is not defined");
            }
            if (dynamicAttributeProvider == null)
            {
                throw new ArgumentException("dynamic attributes provider colleciton is not defined");
            }

            // run scope attribute providers at the game startup
            foreach (var attributeProvider in scopeAttributeProvider)
            {
                attributeProvider.GetAttributes(_attributes);
            }
            _dynamicAttributeProvider = dynamicAttributeProvider;
        }

        /// <summary>
        /// Attribute object accessor
        /// </summary>
        public string this[string index]
        {
            get
            {
                return _attributes[index];
            }
            set
            {
                _attributes[index] = value;

            }
        }

        /// <summary>
        /// Returns number of scoped attributes stored in the system.
        /// </summary>
        /// <returns>Number of scoped attributes</returns>
        public int Count()
        {
            return _attributes.Count;
        }


        /// <summary>
        /// Add dynamic attribute provider to Backtrace client
        /// </summary>
        /// <param name="attributeProvider">dynamic attribute provider</param>
        public void AddDynamicAttributeProvider(IDynamicAttributeProvider attributeProvider)
        {
            if (attributeProvider == null)
            {
                return;
            }
            _dynamicAttributeProvider.Add(attributeProvider);
        }

        /// <summary>
        /// Add scoped attribute provider to Backtrace client
        /// </summary>
        /// <param name="attributeProvider">Scoped attribute provider</param>
        internal void AddScopedAttributeProvider(IScopeAttributeProvider attributeProvider)
        {
            if (attributeProvider == null)
            {
                return;
            }
            attributeProvider.GetAttributes(_attributes);
        }

        /// <summary>
        /// Generate report attributes
        /// </summary>
        /// <returns>Client attributes</returns>
        internal void AddAttributes(IDictionary<string, string> source, bool includeDynamic = true)
        {
            if (includeDynamic)
            {
                // add dynamic scoped attributes
                foreach (var dynamicAttributeProvider in _dynamicAttributeProvider)
                {
                    dynamicAttributeProvider.GetAttributes(source);
                }
            }
            // apply scoped attribtues
            foreach (var attribute in _attributes)
            {
                source[attribute.Key] = attribute.Value;
            }
        }

        /// <summary>
        /// Generates attributes for current application state
        /// </summary>
        /// <returns>Dictionary with all game attributes</returns>
        internal IDictionary<string, string> GenerateAttributes(bool includeDynamic = true)
        {
            var attributes = new Dictionary<string, string>();
            AddAttributes(attributes, includeDynamic);
            return attributes;
        }
    }
}
