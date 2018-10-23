using System;
using System.Collections.Generic;
using System.Reflection;

namespace Backtrace.Unity.Model.JsonData
{
    /// <summary>
    /// Get all application dependencies
    /// </summary>
    public class ApplicationDependencies
    {
        /// <summary>
        /// All listed dependencies with version
        /// </summary>
        public Dictionary<string, string> AvailableDependencies = new Dictionary<string, string>();

        /// <summary>
        /// Create new instance of application dependecies object
        /// </summary>
        /// <param name="assembly">Calling assembly</param>
        public ApplicationDependencies(Assembly assembly)
        {
            ReadDependencies(assembly);
        }

        /// <summary>
        /// Parse all dependencies from assembly to dependency dictionary
        /// </summary>
        /// <param name="assembly">Current assembly</param>
        private void ReadDependencies(Assembly assembly)
        {
            if (assembly == null)
            {
                return;
            }
            AssemblyName[] referencedAssemblies = null;
            try
            {
                referencedAssemblies = assembly.GetReferencedAssemblies();
            }
            catch (Exception)
            {
                return;
            }
            foreach (var refAssembly in referencedAssemblies)
            {
                if (!AvailableDependencies.ContainsKey(refAssembly.Name))
                {
                    AvailableDependencies.Add(refAssembly.Name, refAssembly.Version.ToString());
                }
            }
        }

    }
}
