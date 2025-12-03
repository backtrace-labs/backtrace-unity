//
// BacktraceUnityBridge.mm
// iOS native bridge for Backtrace Unity integration
//
// Exposes a stable C ABI for Unity P/Invoke while delegating to the Backtrace Swift SDK.
//
// Exported symbols:
//   void StartBacktraceIntegration(const char* rawUrl,
//                                  const char* attributeKeys[],
//                                  const char* attributeValues[],
//                                  int attributesCount,
//                                  bool enableOomSupport,
//                                  const char* attachments[],
//                                  const int attachmentSize,
//                                  bool enableClientSideUnwinding);
//   void GetAttributes(Entry** entriesOut, int* sizeOut);
//   void FreeAttributes(Entry* entries, int size);
//   void NativeReport(const char* message, bool setMainThreadAsFaultingThread, bool ignoreIfDebugger);
//   void AddAttribute(const char* key, const char* value);
//   const char* BtCrash(void);
//   void Disable(void);
//

#import <Foundation/Foundation.h>
#import <CoreData/CoreData.h>
#import <CrashReporter/CrashReporter.h>

#if __has_include(<Backtrace/Backtrace-Swift.h>)
#  import <Backtrace/Backtrace-Swift.h>
#else
#  import "Backtrace-Swift.h"
#endif

#include <sys/sysctl.h>

/// C layout used by GetAttributes/FreeAttributes.
typedef struct {
    const char* Key;
    const char* Value;
} Entry;

#pragma mark - Utilities

static inline NSString* BTStr(const char* c) {
    return c ? [NSString stringWithUTF8String:c] : @"";
}

static inline const char* BTDup(NSString* s) {
    const char* src = s.UTF8String ?: "";
    size_t n = strlen(src) + 1;
    char* mem = (char*)malloc(n);
    if (!mem) {
        return "";
    }
    memcpy(mem, src, n);
    return mem;
}

static bool BTDebuggerAttached(void) {
    int mib[4] = { CTL_KERN, KERN_PROC, KERN_PROC_PID, getpid() };
    struct kinfo_proc info; size_t size = sizeof(info); info.kp_proc.p_flag = 0;
    if (sysctl(mib, 4, &info, &size, NULL, 0) == -1) {
        return false;
    }
    return ((info.kp_proc.p_flag & P_TRACED) != 0);
}

#pragma mark - C API (Unity P/Invoke)

extern "C" {

void StartBacktraceIntegration(const char* rawUrl,
                               const char* attributeKeys[],
                               const char* attributeValues[],
                               int attributesCount,
                               bool enableOomSupport,
                               const char* attachments[],
                               const int attachmentSize,
                               bool enableClientSideUnwinding)
{
    @autoreleasepool {
        if (!rawUrl || rawUrl[0] == '\0') {
            return;
        }

        NSURL *url = [NSURL URLWithString:BTStr(rawUrl)];
        if (!url) {
            return;
        }

        BacktraceCredentials *creds = [[BacktraceCredentials alloc] initWithSubmissionUrl:url];

        BacktraceClientConfiguration *cfg = [[BacktraceClientConfiguration alloc] initWithCredentials:creds
                                                           dbSettings:[BacktraceDatabaseSettings new]
                                                           reportsPerMin:30
                                                           allowsAttachingDebugger:NO
                                                           oomMode:(enableOomSupport ? BacktraceOomModeLight: BacktraceOomModeNone)];

        PLCrashReporterConfig *plcfg = [[PLCrashReporterConfig alloc]
                                        initWithSignalHandlerType:PLCrashReporterSignalHandlerTypeBSD
                                        symbolicationStrategy:(enableClientSideUnwinding
                                                              ? PLCrashReporterSymbolicationStrategyAll
                                                              : PLCrashReporterSymbolicationStrategyNone)];
        PLCrashReporter *plcr = [[PLCrashReporter alloc] initWithConfiguration:plcfg];
        BacktraceCrashReporter *cr = [[BacktraceCrashReporter alloc] initWithReporter:plcr];

        NSError *err = nil;
        BacktraceClient *client = [[BacktraceClient alloc] initWithConfiguration:cfg crashReporter:cr error:&err];
        if (err) {
            NSLog(@"[Backtrace] init error: %@", err);
            return;
        }
        BacktraceClient.shared = client;

        // Initial attributes
        NSMutableDictionary *attrs = [NSMutableDictionary dictionaryWithCapacity:(NSUInteger)MAX(0, attributesCount)];
        for (int i = 0; i < attributesCount; ++i) {
            const char* ck = attributeKeys ? attributeKeys[i] : NULL;
            const char* cv = attributeValues ? attributeValues[i] : NULL;
            if (ck) {
                attrs[BTStr(ck)] = BTStr(cv);
            }
        }
        client.attributes = attrs;

        // Initial attachments
        NSMutableArray<NSURL*> *urls = [NSMutableArray arrayWithCapacity:(NSUInteger)MAX(0, attachmentSize)];
        for (int i = 0; i < attachmentSize; ++i) {
            const char* p = attachments ? attachments[i] : NULL;
            if (p && p[0] != '\0') {
                [urls addObject:[NSURL fileURLWithPath:BTStr(p)]];
            }
        }
        client.attachments = urls;
    }
}

void GetAttributes(Entry** entriesOut, int* sizeOut)
{
    @autoreleasepool {
        if (!entriesOut || !sizeOut) {
            return;
        }

        NSDictionary *attrs = BacktraceClient.shared ? BacktraceClient.shared.attributes : @{};
        NSArray *keys = attrs.allKeys;
        int count = (int)keys.count;
        *sizeOut = count;

        if (count <= 0) {
            *entriesOut = NULL;
            return;
        }

        Entry* entries = (Entry*)malloc(sizeof(Entry) * (size_t)count);
        if (!entries) {
            *entriesOut = NULL;
            *sizeOut = 0;
            return;
        }

        for (int i = 0; i < count; ++i) {
            NSString *k = [keys objectAtIndex:(NSUInteger)i];
            NSString *v = [NSString stringWithFormat:@"%@", attrs[k] ?: @""];
            entries[i].Key   = BTDup(k);
            entries[i].Value = BTDup(v);
        }
        // Managed code can call FreeAttributes(entries, count)
        *entriesOut = entries;
    }
}

void FreeAttributes(Entry* entries, int size)
{
    if (!entries) {
        return;
    }
    for (int i = 0; i < size; ++i) {
        if (entries[i].Key) {
            free((void*)entries[i].Key);
        }
        if (entries[i].Value) {
            free((void*)entries[i].Value);
        }
    }
    free(entries);
}

void NativeReport(const char* message, bool /*setMainThreadAsFaultingThread*/, bool ignoreIfDebugger)
{
    @autoreleasepool {
        if (!BacktraceClient.shared) {
            return;
        }
        if (ignoreIfDebugger && BTDebuggerAttached()) {
            return;
        }

        NSString *msg = BTStr(message);
        [BacktraceClient.shared sendWithMessage:msg
                               attachmentPaths:@[]
                                    completion:^ (BacktraceResult * _Nonnull r) {}];
    }
}

void AddAttribute(const char* key, const char* value)
{
    @autoreleasepool {
        if (!BacktraceClient.shared || !key) {
            return;
        }
        NSMutableDictionary *attrs = [[BacktraceClient.shared.attributes mutableCopy] ?: [NSMutableDictionary new] mutableCopy];
        attrs[BTStr(key)] = BTStr(value);
        BacktraceClient.shared.attributes = attrs;
    }
}

const char* BtCrash(void)
{
    return "ok";
}

void Disable(void)
{
    BacktraceClient.shared = nil;
}

} // extern "C"
