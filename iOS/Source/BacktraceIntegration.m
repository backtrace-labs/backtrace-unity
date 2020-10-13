//
//  BacktraceCrashReporterBridge.m
//  Backtrace-Unity
//
//  Created by Backtrace on 10/6/20.
//  Copyright © 2020 Backtrace. All rights reserved.
//

#import <Foundation/Foundation.h>
#include "UnityFramework/UnityFramework-Swift.h"

struct Entry {
    const char* key;
    const char* value;
};


bool initialized = false;
BacktraceCrashReporter *reporter;
void StartBacktraceIntegration(const char* rawUrl) {
    if(!rawUrl){
        return;
    }
    
    reporter = [[BacktraceCrashReporter alloc] initWithUrl: [NSString stringWithUTF8String: rawUrl]];
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
    
    printf("DICTIONARY SIZE: %d", count);
    int index = 0;
    for(id key in dictionary) {
        NSLog(@"%d key=%@ value=%@", index, key, [dictionary objectForKey:key]);
//        &(*entries)[index] = malloc(sizeof(struct Entry));
        (*entries)[index].key = [key UTF8String];
        (*entries)[index].value = [[dictionary objectForKey:key]  UTF8String];
//        NSLog(@"%d IN ENTRY: key=%s value=%s", index, entries[index]->key,  entries[index]->value);
        index += 1;
        
    }
    *size = count;
}
void Crash() {
    NSArray *array = @[];
    NSObject *o = array[1];
}

void HandleAnr(){
    printf("Handling ANR: Unsupported operation.");
    
}
