// Unity iOS C ABI for Backtrace Cocoa 2.2.0+.
#import <Foundation/Foundation.h>
#import <CoreData/CoreData.h>
#import <CrashReporter/CrashReporter.h>
#import <Backtrace/Backtrace-Swift.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <sys/sysctl.h>
#include <unistd.h>

#if !__has_feature(objc_arc)
#error "BacktraceUnityBridge.mm must be compiled with -fobjc-arc"
#endif

// Internal @objc entry point supplied by the pinned Cocoa 2.2.0 runtime.
@interface BacktraceClient (BacktraceUnityIOSLifecycle)
- (void)shutdownForNativeBridge;
@end

#define BT_EXPORT extern "C" __attribute__((visibility("default")))
struct BTEntry { const char *Key; const char *Value; };
enum BTResult : int32_t {
    BTSuccess = 0, BTAlreadyActive = 1, BTInvalidArguments = 2,
    BTStorageFailure = 3, BTInvalidUrl = 4, BTClientFailure = 5,
    BTUnexpectedFailure = 6, BTRestartRequired = 7
};

// This object is never released after handler registration may have begun.
// PLCrashReporter's installed callbacks refer to the reporter's context.
@interface BTUnityIOSRuntime : NSObject
@end
@implementation BTUnityIOSRuntime
@end
static BacktraceClient *BTClient = nil;
static BacktraceCrashReporter *BTReporter = nil;
static BOOL BTActive = NO;
static BOOL BTInstallationAttempted = NO;
static BOOL BTStopped = NO;
static BOOL BTDisableSent = NO;

static void BTLog(const char *operation) {
    // Only compile-time operation names; never exception reasons, URLs or paths.
    @try { NSLog(@"[Backtrace] iOS native operation failed: %s", operation); }
    @catch (__unused NSException *exception) { }
}

static NSString *BTString(const char *value) {
    if (value == NULL) return @"";
    return [NSString stringWithUTF8String:value];
}

static bool BTDebuggerAttached(void) {
    int mib[4] = {CTL_KERN, KERN_PROC, KERN_PROC_PID, getpid()};
    struct kinfo_proc information = {};
    size_t size = sizeof(information);
    return sysctl(mib, 4, &information, &size, NULL, 0) == 0 &&
        (information.kp_proc.p_flag & P_TRACED) != 0;
}

static BacktraceClient *BTCurrentClient(void) {
    @synchronized([BTUnityIOSRuntime class]) { return BTActive ? BTClient : nil; }
}

static void BTFree(BTEntry *entries, int32_t count) {
    if (entries == NULL) return;
    for (int32_t i = 0; i < count; ++i) {
        free(const_cast<char *>(entries[i].Key));
        free(const_cast<char *>(entries[i].Value));
    }
    free(entries);
}

static char *BTCopy(NSString *value) {
    const char *utf8 = value.UTF8String;
    if (utf8 == NULL) return NULL;
    size_t size = strlen(utf8) + 1;
    char *copy = static_cast<char *>(malloc(size));
    if (copy != NULL) memcpy(copy, utf8, size);
    return copy; // Never return a static empty string to a caller that frees it.
}

static int32_t BTStart(const char *submissionUrl,
                      const char *keys[], const char *values[], int32_t count,
                      bool oom, const char *attachments[], int32_t attachmentCount,
                      bool unwind, int32_t rate, const char *basePath) {
    @try {
        @autoreleasepool {
            @synchronized([BTUnityIOSRuntime class]) {
                if (BTActive) return BTAlreadyActive;
                if (BTStopped || BTInstallationAttempted) return BTRestartRequired;
                if (BacktraceClient.shared != nil) return BTClientFailure;
                // V3 iOS intentionally preserves the released default pending path.
                // A non-null path must not silently strand an old iOS pending crash.
                if (basePath != NULL || submissionUrl == NULL || count < 0 || count > 16384 ||
                    attachmentCount < 0 || attachmentCount > 1024 || rate < 0 ||
                    (count != 0 && (keys == NULL || values == NULL)) ||
                    (attachmentCount != 0 && attachments == NULL)) return BTInvalidArguments;

                NSString *urlText = BTString(submissionUrl);
                NSURL *url = urlText.length == 0 ? nil : [NSURL URLWithString:urlText];
                NSString *scheme = url.scheme.lowercaseString;
                if (url.host.length == 0 ||
                    (! [scheme isEqualToString:@"https"] && ! [scheme isEqualToString:@"http"]))
                    return BTInvalidUrl;

                // Validate and allocate everything possible before process-wide registration.
                NSMutableDictionary<NSString *, NSString *> *initialAttributes = [NSMutableDictionary dictionary];
                for (int32_t i = 0; i < count; ++i) {
                    NSString *key = BTString(keys[i]);
                    NSString *value = BTString(values[i]);
                    if (key == nil || value == nil || key.length == 0) return BTInvalidArguments;
                    initialAttributes[key] = value;
                }
                initialAttributes[@"error.type"] = @"Crash";
                NSMutableArray<NSURL *> *initialAttachments = [NSMutableArray array];
                for (int32_t i = 0; i < attachmentCount; ++i) {
                    NSString *path = BTString(attachments[i]);
                    if (path == nil || !path.isAbsolutePath) return BTInvalidArguments;
                    [initialAttachments addObject:[NSURL fileURLWithPath:path]];
                }
                if (![BacktraceClient instancesRespondToSelector:@selector(shutdownForNativeBridge)])
                    return BTClientFailure;

                BacktraceCredentials *credentials = [[BacktraceCredentials alloc] initWithSubmissionUrl:url];
                BacktraceClientConfiguration *configuration = [[BacktraceClientConfiguration alloc]
                    initWithCredentials:credentials dbSettings:[BacktraceDatabaseSettings new]
                    reportsPerMin:static_cast<NSInteger>(rate) allowsAttachingDebugger:NO
                    oomMode:(oom ? BacktraceOomModeLight : BacktraceOomModeNone)];
                configuration.loggingDestinations = [NSSet setWithObject:
                    [[BacktraceConsoleDestination alloc] initWithLevel:BacktraceLogLevelWarning]];
                // Backtrace.framework already links the static PLCrashReporter objects.
                // Resolve its existing class, rather than introducing a static class
                // reference that could pull a second copy into UnityFramework.
                Class configurationClass = NSClassFromString(@"PLCrashReporterConfig");
                if (configurationClass == Nil) return BTClientFailure;
                PLCrashReporterConfig *crashConfig = [[configurationClass alloc]
                    initWithSignalHandlerType:PLCrashReporterSignalHandlerTypeBSD
                    symbolicationStrategy:(unwind ? PLCrashReporterSymbolicationStrategyAll : PLCrashReporterSymbolicationStrategyNone)];
                if (crashConfig == nil || configuration == nil) return BTStorageFailure;

                BTReporter = [[BacktraceCrashReporter alloc] initWithConfig:crashConfig];
                if (BTReporter == nil) return BTStorageFailure;
                // Set before the fallible initializer; never release a possibly installed
                // callback context, including when Swift/Objective-C initialization fails.
                BTInstallationAttempted = YES;
                NSError *error = nil;
                BTClient = [[BacktraceClient alloc] initWithConfiguration:configuration
                    crashReporter:BTReporter error:&error];
                if (BTClient == nil || error != nil) {
                    BTStopped = YES;
                    return BTClientFailure;
                }
                BTClient.attributes = initialAttributes;
                BTClient.attachments = initialAttachments;
                BacktraceClient.shared = BTClient;
                BTActive = YES;
                return BTSuccess;
            }
        }
    } @catch (__unused NSException *exception) {
        BacktraceClient *failedClient = nil;
        @synchronized([BTUnityIOSRuntime class]) {
            BTActive = NO;
            if (BTInstallationAttempted) {
                BTStopped = YES;
                if (BTClient != nil && !BTDisableSent) {
                    BTDisableSent = YES;
                    failedClient = BTClient;
                    if (BacktraceClient.shared == BTClient) BacktraceClient.shared = nil;
                }
            }
        }
        // Release nonfatal work after an exception in final attribute/publication setup,
        // but keep the possible fatal callback owner alive. Never drain Cocoa queues while holding the bridge monitor.
        @try { [failedClient shutdownForNativeBridge]; }
        @catch (__unused NSException *shutdownException) { BTLog("failed-start-cleanup"); }
        BTLog("initialize");
        return BTUnexpectedFailure;
    }
}

BT_EXPORT int32_t BacktraceUnityBridgeVersion(void) { return 3; }

BT_EXPORT int32_t StartBacktraceIntegrationV3(const char *url,
    const char *keys[], const char *values[], int32_t count, bool oom,
    const char *attachments[], int32_t attachmentCount, bool unwind,
    int32_t rate, const char *basePath) {
    return BTStart(url, keys, values, count, oom, attachments, attachmentCount, unwind, rate, basePath);
}

// Retain the previously shipped entry point for older managed callers. New code uses V3.
BT_EXPORT void StartBacktraceIntegration(const char *url,
    const char *keys[], const char *values[], int32_t count, bool oom,
    const char *attachments[], int32_t attachmentCount, bool unwind) {
    (void)BTStart(url, keys, values, count, oom, attachments, attachmentCount, unwind, 30, NULL);
}

BT_EXPORT void GetAttributes(BTEntry **entriesOut, int32_t *countOut) {
    BTEntry *entries = NULL;
    int32_t count = 0;
    if (entriesOut != NULL) *entriesOut = NULL;
    if (countOut != NULL) *countOut = 0;
    if (entriesOut == NULL || countOut == NULL) return;
    @try {
        @autoreleasepool {
            BacktraceClient *client = BTCurrentClient();
            NSDictionary<NSString *, NSString *> *attributes = client == nil ? @{} : client.attributes;
            NSArray<NSString *> *keys = [attributes.allKeys sortedArrayUsingSelector:@selector(compare:)];
            if (keys.count == 0 || keys.count > 16384) return;
            count = static_cast<int32_t>(keys.count);
            entries = static_cast<BTEntry *>(calloc(static_cast<size_t>(count), sizeof(BTEntry)));
            if (entries == NULL) return;
            for (int32_t i = 0; i < count; ++i) {
                NSString *key = keys[static_cast<NSUInteger>(i)];
                NSString *value = [NSString stringWithFormat:@"%@", attributes[key] ?: @""];
                entries[i].Key = BTCopy(key);
                entries[i].Value = BTCopy(value);
                if (entries[i].Key == NULL || entries[i].Value == NULL) {
                    BTFree(entries, count);
                    return;
                }
            }
            *entriesOut = entries;
            *countOut = count;
            entries = NULL;
        }
    } @catch (__unused NSException *exception) {
        BTFree(entries, count);
        *entriesOut = NULL;
        *countOut = 0;
        BTLog("get-attributes");
    }
}

BT_EXPORT void FreeAttributes(BTEntry *entries, int32_t count) {
    @try { BTFree(entries, count); }
    @catch (__unused NSException *exception) { BTLog("free-attributes"); }
}

BT_EXPORT void AddAttribute(const char *key, const char *value) {
    @try {
        @autoreleasepool {
            NSString *attributeKey = BTString(key);
            NSString *attributeValue = BTString(value);
            if (attributeKey.length == 0 || attributeValue == nil) return;
            // Serialize read/modify/write even when invoked by a compatibility caller.
            @synchronized([BTUnityIOSRuntime class]) {
                if (!BTActive) return;
                NSMutableDictionary *attributes = [BTClient.attributes mutableCopy] ?: [NSMutableDictionary dictionary];
                attributes[attributeKey] = attributeValue;
                BTClient.attributes = attributes;
            }
        }
    } @catch (__unused NSException *exception) { BTLog("add-attribute"); }
}

BT_EXPORT void NativeReport(const char *message, bool mainThread, bool ignoreDebugger) {
    @try {
        @autoreleasepool {
            (void)mainThread; // Cocoa's current live-report API does not expose this selector.
            if (ignoreDebugger && BTDebuggerAttached()) return;
            BacktraceClient *client = BTCurrentClient();
            NSString *text = BTString(message);
            if (client == nil || text == nil) return;
            [client sendWithMessage:text attachmentPaths:@[] completion:^(__unused BacktraceResult *result) {}];
        }
    } @catch (__unused NSException *exception) { BTLog("report"); }
}

BT_EXPORT const char *BtCrash(void) { return "ok"; } // Legacy ABI: not a crash trigger.

BT_EXPORT void Disable(void) {
    @try {
        @autoreleasepool {
            BacktraceClient *client = nil;
            @synchronized([BTUnityIOSRuntime class]) {
                if (!BTInstallationAttempted || BTDisableSent) return;
                BTDisableSent = YES;
                BTStopped = YES;
                BTActive = NO;
                client = BTClient;
                if (client != nil && BacktraceClient.shared == client) BacktraceClient.shared = nil;
            }
            // Never wait for Cocoa queues under the bridge state monitor.
            [client shutdownForNativeBridge];
            // BTClient and BTReporter remain retained until process exit.
        }
    } @catch (__unused NSException *exception) { BTLog("disable"); }
}
