#import <Foundation/Foundation.h>
#include "UnityFramework/UnityFramework-Swift.h"

struct Entry {
    const char* key;
    const char* value;
};


bool initialized = false;
BacktraceCrashReporter *reporter;
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

void GetAttibutes(struct Entry** entries, int* size) {
    if(initialized == false) {
        return;
    }
    NSDictionary* dictionary = [reporter getAttributes];
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
    printf("Handling ANR: Unsupported operation.");
    
}
