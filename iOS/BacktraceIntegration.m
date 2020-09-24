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
@end


void StartBacktraceIntegration(const char* rawUrl) {
    if(!rawUrl){
        return;
    }
    [Backtrace create:rawUrl];
}
void Crash() {
     NSArray *array = @[];
     NSObject *o = array[1];
}
