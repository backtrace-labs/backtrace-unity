// backtrace-unity/iOS/BacktraceSDK/BacktraceUnityBridge.mm

#import <Foundation/Foundation.h>
#import <CoreData/CoreData.h>     
#import <CrashReporter/CrashReporter.h>
// Swift-generated umbrella header from Backtrace.xcframework:
#if __has_include(<Backtrace/Backtrace-Swift.h>)
#  import <Backtrace/Backtrace-Swift.h>
#else
#  import "Backtrace-Swift.h"
#endif
#include <sys/sysctl.h>

typedef struct {
    const char* Key;
    const char* Value;
} Entry;

static inline NSString* BTStr(const char* c) { return c ? [NSString stringWithUTF8String:c] : @""; }

static inline const char* BTDup(NSString* s) {
    const char* src = s.UTF8String ?: "";
    size_t n = strlen(src) + 1;
    char* mem = (char*)malloc(n);
    if (mem) memcpy(mem, src, n);
    return mem ? mem : "";
}

static bool BTDebuggerAttached(void) {
    // Same approach as old Utils.isDebuggerAttached
    int mib[4] = { CTL_KERN, KERN_PROC, KERN_PROC_PID, getpid() };
    struct kinfo_proc info; size_t size = sizeof(info); info.kp_proc.p_flag = 0;
    if (sysctl(mib, 4, &info, &size, NULL, 0) == -1) return false;
    return ((info.kp_proc.p_flag & P_TRACED) != 0);
}

// ---------------- C API expected by Unity ----------------
extern "C" {

// Matches your old signature/order from NativeClient.cs
void StartBacktraceIntegration(const char* rawUrl,
                               const char* attributeKeys[], const char* attributeValues[], int attributesCount,
                               bool enableOomSupport,
                               const char* attachments[], const int attachmentSize,
                               bool enableClientSideUnwinding)
{
    if (!rawUrl) return;

    // Credentials from submission URL (what Unity currently passes)
    NSURL *url = [NSURL URLWithString:BTStr(rawUrl)];
    BacktraceCredentials *creds = [[BacktraceCredentials alloc] initWithSubmissionUrl:url];

    // Configure the client: iOS 13+ project should embed Swift stdlibs via post-build
    BacktraceClientConfiguration *cfg =
        [[BacktraceClientConfiguration alloc] initWithCredentials:creds
                                                       dbSettings:[BacktraceDatabaseSettings new]
                                                   reportsPerMin:30
                                         allowsAttachingDebugger:NO
                                                          oomMode:(enableOomSupport ? BacktraceOomModeFull : BacktraceOomModeNone)];

    // Configure PLCrashReporter: allow you to toggle client-side unwinding/symbolication
    PLCrashReporterConfig *plcfg = [[PLCrashReporterConfig alloc]
                                    initWithSignalHandlerType: PLCrashReporterSignalHandlerTypeBSD
                                    symbolicationStrategy: (enableClientSideUnwinding
                                                            ? PLCrashReporterSymbolicationStrategyAll
                                                            : PLCrashReporterSymbolicationStrategyNone)];
    PLCrashReporter *plcr = [[PLCrashReporter alloc] initWithConfiguration:plcfg];
    BacktraceCrashReporter *cr = [[BacktraceCrashReporter alloc] initWithReporter:plcr];

    NSError *err = nil;
    BacktraceClient *client = [[BacktraceClient alloc] initWithConfiguration:cfg crashReporter:cr error:&err];
    if (err) { NSLog(@"[Backtrace] init error: %@", err); return; }
    BacktraceClient.shared = client;

    // Initial attributes
    NSMutableDictionary *attrs = [NSMutableDictionary dictionaryWithCapacity:(NSUInteger)MAX(0, attributesCount)];
    for (int i = 0; i < attributesCount; ++i) {
        const char* ck = attributeKeys ? attributeKeys[i] : NULL;
        const char* cv = attributeValues ? attributeValues[i] : NULL;
        if (ck) attrs[BTStr(ck)] = BTStr(cv);
    }
    client.attributes = attrs;

    // Initial attachments
    NSMutableArray<NSURL*> *urls = [NSMutableArray arrayWithCapacity:(NSUInteger)MAX(0, attachmentSize)];
    for (int i = 0; i < attachmentSize; ++i) {
        const char* p = attachments ? attachments[i] : NULL;
        if (p) [urls addObject:[NSURL fileURLWithPath:BTStr(p)]];
    }
    client.attachments = urls;
}

void GetAttributes(Entry** entriesOut, int* sizeOut)
{
    if (!entriesOut || !sizeOut) return;
    NSDictionary *attrs = BacktraceClient.shared ? BacktraceClient.shared.attributes : @{};
    NSArray *keys = attrs.allKeys;
    int count = (int)keys.count;
    *sizeOut = count;

    if (count <= 0) { *entriesOut = NULL; return; }
    Entry* entries = (Entry*)malloc(sizeof(Entry) * (size_t)count);
    for (int i = 0; i < count; ++i) {
        NSString *k = [keys objectAtIndex:(NSUInteger)i];
        NSString *v = [NSString stringWithFormat:@"%@", attrs[k] ?: @""];
        entries[i].Key   = BTDup(k);
        entries[i].Value = BTDup(v);
    }
    *entriesOut = entries; // Your C# currently frees only the array; see note below.
}

// Optional exact free helper (not currently called by your C#)
void FreeAttributes(Entry* entries, int size)
{
    if (!entries) return;
    for (int i = 0; i < size; ++i) {
        if (entries[i].Key)   free((void*)entries[i].Key);
        if (entries[i].Value) free((void*)entries[i].Value);
    }
    free(entries);
}

void NativeReport(const char* message, bool /*setMainThreadAsFaultingThread*/, bool ignoreIfDebugger)
{
    if (!BacktraceClient.shared) return;
    if (ignoreIfDebugger && BTDebuggerAttached()) return;

    NSString *msg = BTStr(message);
    // Empty attachmentPaths: rely on client.attachments set in StartBacktraceIntegration
    [BacktraceClient.shared sendWithMessage:msg
                           attachmentPaths:@[]
                                completion:^ (BacktraceResult * _Nonnull r) {}];
}

void AddAttribute(char* key, char* value)
{
    if (!BacktraceClient.shared || !key) return;
    NSMutableDictionary *attrs = [[BacktraceClient.shared.attributes mutableCopy] ?: [NSMutableDictionary new] mutableCopy];
    attrs[BTStr(key)] = BTStr(value);
    BacktraceClient.shared.attributes = attrs;
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
