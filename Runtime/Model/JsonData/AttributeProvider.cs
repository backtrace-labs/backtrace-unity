using Backtrace.Unity.Model.Attributes;
using System;
using System.Collections.Generic;

namespace Backtrace.Unity.Model.JsonData
{
    internal sealed class AttributeProvider
    {
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
            },
            dynamicAttributeProvider: new List<IDynamicAttributeProvider>()
            {
                new MachineStateAttributeProvider(),
                new ProcessAttributeProvider(),
                new SceneAttributeProvider(),
                new PiiAttributeProvider()
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
        internal void AddDynamicAttributeProvider(IDynamicAttributeProvider attributeProvider)
        {
            if (attributeProvider == null)
            {
                return;
            }
            _dynamicAttributeProvider.Add(attributeProvider);
        }

        /// <summary>
        /// Get Dictionary of scoped attributes
        /// </summary>
        /// <returns>Attributes dictionary</returns>
        internal IDictionary<string, string> Get()
        {
            return _attributes;
        }


        /// <summary>
        /// Generate report attributes
        /// </summary>
        /// <returns>Client attributes</returns>
        internal void AddAttributes(IDictionary<string, string> source)
        {
            // add dynamic scoped attributes
            foreach (var dynamicAttributeProvider in _dynamicAttributeProvider)
            {
                dynamicAttributeProvider.GetAttributes(source);
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
        internal IDictionary<string, string> GenerateAttributes()
        {
            var attributes = new Dictionary<string, string>();
            AddAttributes(attributes);
            return attributes;
        }

    }
}
