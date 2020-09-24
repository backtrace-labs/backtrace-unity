#import <Foundation/Foundation.h>
#import <CoreLocation/CoreLocation.h>
#import <mach/mach.h>
#import <mach/mach_host.h>

#import <CrashReporter/CrashReporter.h>


@interface Backtrace: NSObject
+ (Backtrace*) create: (const char*)url;
- (void) uploadPendingReports;
- (void) upload: (NSData*) data;
@end

@implementation Backtrace
/**
PL Crash reporter instance
 */
PLCrashReporter *_crashReporter;

/**
 Backtrace URL
 */
const char *_backtraceUrl;

- (void) upload: (NSData*) crash {
    NSString* uploadUrl = [NSString stringWithUTF8String: _backtraceUrl];
    NSMutableURLRequest *req = [NSMutableURLRequest requestWithURL: [NSURL URLWithString:uploadUrl]];
    [req setHTTPMethod: @"POST"];
    [req setHTTPBody: crash];
    NSURLSession *session = [NSURLSession sharedSession];
    [[session dataTaskWithRequest:req
              completionHandler:^(NSData *data,
                                  NSURLResponse *response,
                                  NSError *error) {

                 NSHTTPURLResponse *httpResponse = (NSHTTPURLResponse *) response;
                NSLog(@"response status code: %ld", (long)[httpResponse statusCode]);
                if((long)[httpResponse statusCode] == 200) {
                        [_crashReporter purgePendingCrashReport];
                }

      }] resume];
}


+ (Backtrace*)create: (const char*) url {
    static Backtrace* instance = nil;
    if(!instance) {
        instance = [[Backtrace alloc] init];
    }
    _backtraceUrl = url;
    PLCrashReporterConfig *config = [PLCrashReporterConfig defaultConfiguration];

    _crashReporter = [[PLCrashReporter alloc] initWithConfiguration:config];

    if([_crashReporter hasPendingCrashReport])
    {
           NSData *data = [_crashReporter loadPendingCrashReportData];
           PLCrashReport *report = [[PLCrashReport alloc] initWithData: data error: NULL];
            if (report){
               [instance upload:data];
            }
    }
    [_crashReporter enableCrashReporter];
    
    return instance;
}

- (void) uploadPendingReports {
    if ([_crashReporter hasPendingCrashReport]) {
        NSData *data = [_crashReporter loadPendingCrashReportData];
        PLCrashReport *report = [[PLCrashReport alloc] initWithData: data error: NULL];
        if (!report)
            return;
        [self upload:data];
    }
}
//- (void) readMemoryParameters:  (float[])vmMemoryUsed {
//    mach_port_t host_port;
//    mach_msg_type_number_t host_size;
//    vm_size_t pagesize;
//
//    host_port = mach_host_self();
//    host_size = sizeof(vm_statistics_data_t) / sizeof(integer_t);
//    host_page_size(host_port, &pagesize);
//
//    vm_statistics_data_t vm_stat;
//
//    if (host_statistics(host_port, HOST_VM_INFO, (host_info_t)&vm_stat, &host_size) != KERN_SUCCESS) {
//        NSLog(@"Failed to fetch vm statistics");
//    }
//
//    vmMemoryUsed[0] = vm_stat.active_count;
//    NSLog(@"Active memory: %f", vmMemoryUsed[0]);
//
//}
//
//- (void) readProcessorParameters {
//    NSLog(@"Reading processor parameters");
//}
@end


void StartBacktraceIntegration(const char* rawUrl) {
    if(!rawUrl){
        return;
    }
    [Backtrace create:rawUrl];
//    [client uploadPendingReports];
}
void Crash() {
     NSArray *array = @[];
     NSObject *o = array[1];
}
