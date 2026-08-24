using System;
using System.IO;
using Backtrace.Unity.Runtime.Native.OSX;
using NUnit.Framework;
using UnityEngine;

namespace Backtrace.Unity.Tests.Runtime
{
    public class PendingCrashReportQuarantineTests
    {
        private string _reportDirectory;

        [SetUp]
        public void Setup()
        {
            _reportDirectory = Path.Combine(Path.GetTempPath(), "backtrace-quarantine-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_reportDirectory);
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(_reportDirectory))
            {
                Directory.Delete(_reportDirectory, true);
            }
        }

        private string LiveReportPath
        {
            get { return Path.Combine(_reportDirectory, PendingCrashReportQuarantine.LiveReportFileName); }
        }

        private string QuarantinedReportPath
        {
            get { return Path.Combine(_reportDirectory, PendingCrashReportQuarantine.QuarantinedReportFileName); }
        }

        [Test]
        public void Quarantine_MovesPendingReportOutOfUnitysSight()
        {
            File.WriteAllText(LiveReportPath, "pending-report");

            Assert.IsTrue(PendingCrashReportQuarantine.QuarantineLiveReport(_reportDirectory));

            Assert.IsFalse(File.Exists(LiveReportPath));
            Assert.AreEqual("pending-report", File.ReadAllText(QuarantinedReportPath));
        }

        [Test]
        public void Quarantine_WithoutPendingReport_DoesNothing()
        {
            Assert.IsFalse(PendingCrashReportQuarantine.QuarantineLiveReport(_reportDirectory));

            Assert.IsFalse(File.Exists(LiveReportPath));
            Assert.IsFalse(File.Exists(QuarantinedReportPath));
        }

        [Test]
        public void Quarantine_ReplacesStaleQuarantinedReportWithNewerCrash()
        {
            File.WriteAllText(QuarantinedReportPath, "stale-report");
            File.WriteAllText(LiveReportPath, "newer-report");

            Assert.IsTrue(PendingCrashReportQuarantine.QuarantineLiveReport(_reportDirectory));

            Assert.IsFalse(File.Exists(LiveReportPath));
            Assert.AreEqual("newer-report", File.ReadAllText(QuarantinedReportPath));
        }

        [Test]
        public void Restore_HandsQuarantinedReportBackToPlCrashReporter()
        {
            File.WriteAllText(QuarantinedReportPath, "pending-report");

            Assert.IsTrue(PendingCrashReportQuarantine.RestoreLiveReport(_reportDirectory));

            Assert.IsFalse(File.Exists(QuarantinedReportPath));
            Assert.AreEqual("pending-report", File.ReadAllText(LiveReportPath));
        }

        [Test]
        public void Restore_WithoutQuarantinedReport_DoesNothing()
        {
            Assert.IsFalse(PendingCrashReportQuarantine.RestoreLiveReport(_reportDirectory));

            Assert.IsFalse(File.Exists(LiveReportPath));
            Assert.IsFalse(File.Exists(QuarantinedReportPath));
        }

        [Test]
        public void Restore_KeepsBothReportsWhenAFreshLiveReportExists()
        {
            File.WriteAllText(QuarantinedReportPath, "quarantined-report");
            File.WriteAllText(LiveReportPath, "fresh-report");

            Assert.IsFalse(PendingCrashReportQuarantine.RestoreLiveReport(_reportDirectory));

            Assert.AreEqual("fresh-report", File.ReadAllText(LiveReportPath));
            Assert.AreEqual("quarantined-report", File.ReadAllText(QuarantinedReportPath));
        }

        [Test]
        public void QuarantineAndRestore_NeverThrowForInvalidDirectories()
        {
            var missingDirectory = Path.Combine(_reportDirectory, "does-not-exist");

            Assert.DoesNotThrow(() => PendingCrashReportQuarantine.QuarantineLiveReport(null));
            Assert.DoesNotThrow(() => PendingCrashReportQuarantine.RestoreLiveReport(null));
            Assert.IsFalse(PendingCrashReportQuarantine.QuarantineLiveReport(missingDirectory));
            Assert.IsFalse(PendingCrashReportQuarantine.RestoreLiveReport(missingDirectory));
        }

        [Test]
        public void DefaultReportDirectory_FollowsPlCrashReporterLayout()
        {
            var expected = Path.Combine(Path.Combine("caches", "com.plausiblelabs.crashreporter.data"), "com.example.app");

            Assert.AreEqual(expected, PendingCrashReportQuarantine.GetDefaultReportDirectory("caches", "com.example.app"));
        }

        [Test]
        public void ApplicationReportDirectory_NeverThrows()
        {
            Assert.DoesNotThrow(() => PendingCrashReportQuarantine.GetApplicationReportDirectory());
        }

        [Test]
        public void CaptureActiveFlag_RoundTripsThroughPlayerPrefs()
        {
            var hadKey = PlayerPrefs.HasKey(PendingCrashReportQuarantine.CaptureActivePlayerPrefsKey);
            var previousValue = hadKey ? PlayerPrefs.GetInt(PendingCrashReportQuarantine.CaptureActivePlayerPrefsKey) : 0;
            try
            {
                PendingCrashReportQuarantine.SetCaptureActive(true);
                Assert.IsTrue(PendingCrashReportQuarantine.IsCaptureActive());

                PendingCrashReportQuarantine.SetCaptureActive(false);
                Assert.IsFalse(PendingCrashReportQuarantine.IsCaptureActive());
            }
            finally
            {
                if (hadKey)
                {
                    PlayerPrefs.SetInt(PendingCrashReportQuarantine.CaptureActivePlayerPrefsKey, previousValue);
                }
                else
                {
                    PlayerPrefs.DeleteKey(PendingCrashReportQuarantine.CaptureActivePlayerPrefsKey);
                }
                PlayerPrefs.Save();
            }
        }
    }
}
