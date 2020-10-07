//
//  BacktraceCrashReporterBridge.m
//  Backtrace-Unity
//
//  Created by Backtrace on 10/6/20.
//  Copyright © 2020 Backtrace. All rights reserved.
//

#import <Foundation/Foundation.h>
#include "UnityFramework/UnityFramework-Swift.h"


void StartBacktraceIntegration(const char* rawUrl) {
    if(!rawUrl){
        return;
    }
    
    BacktraceCrashReporter *reporter = [[BacktraceCrashReporter alloc] initWithUrl: [NSString stringWithUTF8String: rawUrl]];
    [reporter start];
}

void GetAttibutes() {
    
}
void Crash() {
    NSArray *array = @[];
    NSObject *o = array[1];
}

void HandleAnr(){
    printf("Handling ANR: Unsupported operation.");
    
}
