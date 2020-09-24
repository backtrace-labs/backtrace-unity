#import <Foundation/Foundation.h>
#import <CoreLocation/CoreLocation.h>
#import <mach/mach.h>
#import <mach/mach_host.h>

#import <CrashReporter/CrashReporter.h>


@interface BacktraceAttributes: NSObject
+ (BacktraceAttributes*) create;
- (void) readMemoryParameters: (float[])vmMemoryUsed;
- (void) readProcessorParameters;
- (void) startBacktraceIntegration: (NSString*)url;
- (void) superCrashInWrapper;
@end

@implementation BacktraceAttributes

static void upload(NSData *crash, NSString *url) {
    NSMutableURLRequest *req = [NSMutableURLRequest requestWithURL: [NSURL URLWithString:url]];
    NSHTTPURLResponse *res = nil;
    NSError *err = nil;
    [req setHTTPMethod: @"POST"];
    [req setHTTPBody: crash];
    [NSURLConnection sendSynchronousRequest:req returningResponse:&res error:&err];
}


+ (BacktraceAttributes*)create {
    static BacktraceAttributes* instance = nil;
    if(!instance) {
        instance = [[BacktraceAttributes alloc] init];
    }
    return instance;
}

- (void) startBacktraceIntegration: (NSString*) url {
    [PLCrashReporter.sharedReporter enableCrashReporter];
    if ([PLCrashReporter.sharedReporter hasPendingCrashReport]) {
        NSData *data = [PLCrashReporter.sharedReporter loadPendingCrashReportData];
        PLCrashReport *report = [[PLCrashReport alloc] initWithData: data error: NULL];
        if (!report)
            return;
        upload(data, url);
    }
}

- (void) superCrashInWrapper {
    NSArray *array = @[];
    NSObject *o = array[1];
}
- (void) readMemoryParameters:  (float[])vmMemoryUsed {
    mach_port_t host_port;
    mach_msg_type_number_t host_size;
    vm_size_t pagesize;

    host_port = mach_host_self();
    host_size = sizeof(vm_statistics_data_t) / sizeof(integer_t);
    host_page_size(host_port, &pagesize);

    vm_statistics_data_t vm_stat;

    if (host_statistics(host_port, HOST_VM_INFO, (host_info_t)&vm_stat, &host_size) != KERN_SUCCESS) {
        NSLog(@"Failed to fetch vm statistics");
    }
    
    vmMemoryUsed[0] = vm_stat.active_count;
    NSLog(@"Active memory: %f", vmMemoryUsed[0]);

}
    
- (void) readProcessorParameters {
    NSLog(@"Reading processor parameters");
}
@end

void GetAttributes(float memoryUsed[]) {
    [[BacktraceAttributes create] readMemoryParameters:memoryUsed];
}

void StartBacktraceIntegration(const char* rawUrl) {
    NSString* url = [NSString stringWithUTF8String: rawUrl];
    [[BacktraceAttributes create] startBacktraceIntegration:url];
}
void SuperCrashInWrapper() {
    [[BacktraceAttributes create] superCrashInWrapper];
}
