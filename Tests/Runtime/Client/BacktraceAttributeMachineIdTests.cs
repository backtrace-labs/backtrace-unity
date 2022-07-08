using Backtrace.Unity.Extensions;
using Backtrace.Unity.Model;
using Backtrace.Unity.Tests.Runtime.Client.Mocks;
using NUnit.Framework;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime.Client
{
    class BacktraceAttributeMachineIdTests
    {
        [TearDown]
        public void Cleanup()
        {
            PlayerPrefs.DeleteKey(MachineIdStorage.MachineIdentifierKey);
        }

        [Test]
        public void TestMachineAttributes_ShouldUseUnityIdentifier_ShouldReturnUnityIdentitfier()
        {
            var machineIdStorage = new MachineIdStorageMock();

            var machineId = machineIdStorage.GenerateMachineId();

            Assert.IsFalse(GuidHelper.IsEmptyGuid(machineId));
        }

        [Test]
        public void TestMachineAttributes_ShouldUseMac_ShouldReturnNetowrkingIdentifier()
        {
            var machineIdStorage = new MachineIdStorageMock(false);

            var machineId = machineIdStorage.GenerateMachineId();

            Assert.IsFalse(GuidHelper.IsEmptyGuid(machineId));
        }

        [Test]
        public void TestMachineAttributes_ShouldUseRandomMachineId_ShouldReturnRandomMachineId()
        {
            var machineIdStorage = new MachineIdStorageMock(false, false);

            var machineId = machineIdStorage.GenerateMachineId();

            Assert.IsFalse(GuidHelper.IsEmptyGuid(machineId));
        }

        [Test]
        public void TestMachineAttributes_ShouldAlwaysReturnTheSameValueUnityId_IdentifierAreTheSame()
        {
            var firstMachineIdStorage = new MachineIdStorageMock().GenerateMachineId();
            var secGenerationOfMachineIdStorage = new MachineIdStorageMock().GenerateMachineId();

            Assert.IsTrue(firstMachineIdStorage == secGenerationOfMachineIdStorage);
        }

        [Test]
        public void TestMachineAttributes_ShouldAlwaysReturnTheSameValueMacId_IdentifierAreTheSame()
        {
            var firstMachineIdStorage = new MachineIdStorageMock(false).GenerateMachineId();
            var secGenerationOfMachineIdStorage = new MachineIdStorageMock(false).GenerateMachineId();

            Assert.IsTrue(firstMachineIdStorage == secGenerationOfMachineIdStorage);
        }

        [Test]
        public void TestMachineAttributes_ShouldAlwaysReturnTheSameValueRandomId_IdentifierAreTheSame()
        {
            var firstMachineIdStorage = new MachineIdStorageMock(false, false).GenerateMachineId();
            var secGenerationOfMachineIdStorage = new MachineIdStorageMock(false, false).GenerateMachineId();

            Assert.IsTrue(firstMachineIdStorage == secGenerationOfMachineIdStorage);
        }
    }
}
