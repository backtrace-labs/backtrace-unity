using Backtrace.Unity.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backtrace.Unity.Tests.Runtime.Client.Mocks
{
    internal class MachineIdStorageMock : MachineIdStorage
    {
        private readonly bool _allowUnityIdentifier;
        private readonly bool _allowNetworking;
        public MachineIdStorageMock(bool allowUnityIdentifier = true, bool allowNetworking = true) : base()
        {
            _allowUnityIdentifier = allowUnityIdentifier;
            _allowNetworking = allowNetworking;
        }


        protected override string UseNetworkingIdentifier()
        {
            if (!_allowNetworking)
            {
                return null;
            }
            return base.UseNetworkingIdentifier();
        }

        protected override string UseUnityIdentifier()
        {
            if (!_allowUnityIdentifier)
            {
                return null;
            }
            return base.UseUnityIdentifier();
        }
    }
}
