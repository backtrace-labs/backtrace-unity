#import <Foundation/Foundation.h>
#include "UnityFramework/UnityFramework-Swift.h"

//attributes antry
struct Entry {
    const char* key;
    const char* value;
};


bool initialized = false;
BacktraceCrashReporter *reporter;
// Initialize native crash reporter handler and set basic Unity attributes that integration will store when exception occured.
void StartBacktraceIntegration(const char* rawUrl, const char* attributeKeys[], const char* attributeValues[], const int size) {
    if(!rawUrl){
        return;
    }
   NSMutableDictionary *attributes = [[NSMutableDictionary alloc]initWithCapacity:size];
    for (int index =0; index < size ; index++) {
        [attributes setObject:[NSString stringWithUTF8String: attributeValues[index]] forKey:[NSString stringWithUTF8String: attributeKeys[index]]];
    }
    
    reporter = [[BacktraceCrashReporter alloc] initWithUrl:[NSString stringWithUTF8String: rawUrl] attributes:attributes ];
    [reporter start];
    initialized = true;
}

// Return native iOS attributes
// Attributes support doesn't require to use instance of BacktraceCrashReporter object
// we still want to provide attributes when someone doesn't want to capture native crashes
// GetAttributes function will alloc space in memory for NSDicionary and will put all attributes 
// there. In Unity layer we will reuse intPtr to get all attributes from NSDictionary.
void GetAttibutes(struct Entry** entries, int* size) {
    NSDictionary* dictionary = [BacktraceCrashReporter getAttributes];
    int count = (int) [dictionary count];
    *entries = malloc(count * sizeof(struct Entry));
    int index = 0;
    for(id key in dictionary) {
        (*entries)[index].key = [key UTF8String];
        (*entries)[index].value = [[dictionary objectForKey:key]  UTF8String];
        index += 1;
    }
    
    *size = count;
}

void AddAttribute(char* key, char* value) {
    // there is no reason to store attribuets when integration is disabled
    if(initialized == false) {
        return;
    }
    
    [reporter setAttributesWithKey:[NSString stringWithUTF8String: key] value:[NSString stringWithUTF8String: value]];
    
}
void Crash() {
    NSArray *array = @[];
    NSObject *o = array[1];
}

void HandleAnr(){
    printf("Handling ANR: Feature unsupported yet.");
    
}
