using Backtrace.Unity.Extensions;
using Backtrace.Unity.Model;
using Backtrace.Unity.Tests.Runtime.Client.Mocks;
using NUnit.Framework;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime.Client
{
    class BacktraceAttributeMachineIdTests
    {
        [SetUp]
        public void Cleanup()
        {
            PlayerPrefs.DeleteKey(MachineIdStorage.MachineIdentifierKey);
        }

        [Test]
        public void TestMachineAttributes_ShouldUseUnityIdentifier_ShouldReturnUnityIdentitfier()
        {
            var machineIdStorage = new MachineIdStorageMock();

            var machineId = machineIdStorage.GenerateMachineId();

            Assert.IsFalse(GuidHelper.IsNullOrEmpty(machineId));
        }

        [Test]
        public void TestMachineAttributes_ShouldUseMac_ShouldReturnNetowrkingIdentifier()
        {
            var machineIdStorage = new MachineIdStorageMock(false);

            var machineId = machineIdStorage.GenerateMachineId();

            Assert.IsFalse(GuidHelper.IsNullOrEmpty(machineId));
        }

        [Test]
        public void TestMachineAttributes_ShouldUseRandomMachineId_ShouldReturnRandomMachineId()
        {
            var machineIdStorage = new MachineIdStorageMock(false, false);

            var machineId = machineIdStorage.GenerateMachineId();

            Assert.IsFalse(GuidHelper.IsNullOrEmpty(machineId));
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

        [Test]
        public void TestMachineAttributes_ShouldAlwaysGenerateTheSameUntiyAttribute_ShouldReturnTheSameUnityIdentitfier()
        {
            var machineIdStorage = new MachineIdStorageMock();

            var machineId = machineIdStorage.GenerateMachineId();
            PlayerPrefs.DeleteKey(MachineIdStorage.MachineIdentifierKey);
            var machineIdAfterCleanup = machineIdStorage.GenerateMachineId();

            Assert.AreEqual(machineId, machineIdAfterCleanup);
        }

        [Test]
        public void TestMachineAttributes_ShouldAlwaysGenerateTheSameMacAttribute_ShouldReturnTheSameMacIdentitfier()
        {
            var machineIdStorage = new MachineIdStorageMock(false);

            var machineId = machineIdStorage.GenerateMachineId();
            PlayerPrefs.DeleteKey(MachineIdStorage.MachineIdentifierKey);
            var machineIdAfterCleanup = machineIdStorage.GenerateMachineId();

            Assert.AreEqual(machineId, machineIdAfterCleanup);
        }
    }
}
