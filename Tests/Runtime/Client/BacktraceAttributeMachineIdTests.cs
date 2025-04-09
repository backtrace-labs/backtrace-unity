using Backtrace.Unity.Extensions;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.DataProvider;
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
            var machineIdStorage = new MachineIdStorage();

            var machineId = machineIdStorage.GenerateMachineId();

            Assert.IsFalse(GuidHelper.IsNullOrEmpty(machineId));
        }

        [Test]
        public void TestMachineAttributes_ShouldUseMac_ShouldReturnNetowrkingIdentifier()
        {
            var networkIdentifierDataProvider = new NetworkIdentifierDataProvider();
            var expectedMachineId = networkIdentifierDataProvider.Get();
            var machineIdStorage = new MachineIdStorage(new IMachineIdentifierProvider[] { networkIdentifierDataProvider }, new SessionStorageDataProvider());

            var machineId = machineIdStorage.GenerateMachineId();

            Assert.IsFalse(GuidHelper.IsNullOrEmpty(expectedMachineId));
        }

        [Test]
        public void TestMachineAttributes_ShouldUseRandomMachineId_ShouldReturnRandomMachineId()
        {
            var machineIdStorage = new MachineIdStorage(new IMachineIdentifierProvider[0], new SessionStorageDataProvider());

            var machineId = machineIdStorage.GenerateMachineId();

            Assert.IsFalse(GuidHelper.IsNullOrEmpty(machineId));
        }

        [Test]
        public void TestMachineAttributes_ShouldConvertInvalidIdIntoGuid_ValidIdIsAlwaysUsed()
        {
            var invalidValue = "randomValue";
            PlayerPrefs.SetString(MachineIdStorage.MachineIdentifierKey, invalidValue);
            var expectedGuid = GuidHelper.FromString(invalidValue).ToString();

            var machineId = new MachineIdStorage().GenerateMachineId();

            Assert.IsTrue(expectedGuid == machineId);
        }

        [Test]
        public void TestMachineAttributes_ShouldRetrieveValueFromStorage_IdentifierIsStored()
        {
            // make sure it's always empty
            PlayerPrefs.DeleteKey(MachineIdStorage.MachineIdentifierKey);

            var machineId = new MachineIdStorage().GenerateMachineId();

            var storage = new SessionStorageDataProvider();
            var storedMachineId = storage.GetString(MachineIdStorage.MachineIdentifierKey);

            Assert.IsTrue(machineId == storedMachineId);
        }

        [Test]
        public void TestMachineAttributes_ShouldAlwaysReturnTheSameValueUnityId_IdentifierAreTheSame()
        {
            var firstMachineIdStorage = new MachineIdStorage().GenerateMachineId();
            var secGenerationOfMachineIdStorage = new MachineIdStorage().GenerateMachineId();

            Assert.IsTrue(firstMachineIdStorage == secGenerationOfMachineIdStorage);
        }

        [Test]
        public void TestMachineAttributes_ShouldAlwaysGenerateTheSameUntiyAttribute_ShouldReturnTheSameUnityIdentitfier()
        {
            var machineIdStorage = new MachineIdStorage();

            var machineId = machineIdStorage.GenerateMachineId();
            PlayerPrefs.DeleteKey(MachineIdStorage.MachineIdentifierKey);
            var machineIdAfterCleanup = machineIdStorage.GenerateMachineId();

            Assert.AreEqual(machineId, machineIdAfterCleanup);
        }

        [Test]
        public void TestMachineAttributes_ShouldAlwaysGenerateTheSameMacAttribute_ShouldReturnTheSameMacIdentitfier()
        {
            var machineIdStorage = new MachineIdStorage(new IMachineIdentifierProvider[] { new NetworkIdentifierDataProvider() }, new SessionStorageDataProvider());

            var machineId = machineIdStorage.GenerateMachineId();
            PlayerPrefs.DeleteKey(MachineIdStorage.MachineIdentifierKey);
            var machineIdAfterCleanup = machineIdStorage.GenerateMachineId();

            Assert.AreEqual(machineId, machineIdAfterCleanup);
        }
    }
}
