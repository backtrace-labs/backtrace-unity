import Foundation

struct Statistics {
    
    private static func taskInfo<T>(_ taskInfoType: inout T, _ taskFlavor: Int32) throws {
        var count = mach_msg_type_number_t(MemoryLayout<T>.stride / MemoryLayout<natural_t>.stride)
        
        let kern: kern_return_t = withUnsafeMutablePointer(to: &taskInfoType) { (pointer) -> kern_return_t in
            task_info(mach_task_self_,
                      task_flavor_t(taskFlavor),
                      pointer.withMemoryRebound(to: Int32.self, capacity: 1) { task_info_t($0) },
                      &count)
        }
        guard kern == KERN_SUCCESS else {
            throw KernError.code(kern)
        }
    }
    
    static func taskVmInfo() throws -> task_vm_info {
        var taskVmInfo = task_vm_info()
        try taskInfo(&taskVmInfo, TASK_VM_INFO)
        return taskVmInfo
    }
    
    static func machTaskBasicInfo() throws -> mach_task_basic_info {
        var machTaskBasicInfo = mach_task_basic_info()
        try taskInfo(&machTaskBasicInfo, MACH_TASK_BASIC_INFO)
        return machTaskBasicInfo
    }
    
    static func taskEventsInfo() throws -> task_events_info {
        var taskEventsInfo = task_events_info()
        try taskInfo(&taskEventsInfo, TASK_EVENTS_INFO)
        return taskEventsInfo
    }
    
    static func processorSetLoadInfo() throws -> processor_set_load_info {
        var processorSetName = processor_set_name_t()
        var result = processor_set_default(mach_host_self(), &processorSetName)
        guard result == KERN_SUCCESS else {
            throw KernError.code(result)
        }
        var count = mach_msg_type_number_t(MemoryLayout<processor_set_load_info>.stride/MemoryLayout<natural_t>.stride)
        var processorSetLoadInfo = processor_set_load_info()
        result = withUnsafeMutablePointer(to: &processorSetLoadInfo) {
            $0.withMemoryRebound(to: integer_t.self, capacity: Int(count)) {
                processor_set_statistics(processorSetName, PROCESSOR_SET_LOAD_INFO, $0, &count)
            }
        }
        guard result == KERN_SUCCESS else {
            throw KernError.code(result)
        }
        return processorSetLoadInfo
    }
    
    static func hostCpuLoadInfo() throws -> host_cpu_load_info {
        var count = mach_msg_type_number_t(MemoryLayout<host_cpu_load_info>.size / MemoryLayout<integer_t>.size)
        var info = host_cpu_load_info()
        let result: kern_return_t = withUnsafeMutablePointer(to: &info) {
            $0.withMemoryRebound(to: integer_t.self, capacity: Int(count)) {
                host_statistics64(mach_host_self(), HOST_CPU_LOAD_INFO, $0, &count)
            }
        }
        guard result == KERN_SUCCESS else {
            throw KernError.code(result)
        }
        
        return info
    }
    
    static func vmStatistics64() throws -> vm_statistics64 {
        var vmStatInfo = vm_statistics64()
        var size: mach_msg_type_number_t =
            UInt32(MemoryLayout<vm_statistics64_data_t>.size / MemoryLayout<integer_t>.size)
        
        let kernReturn = withUnsafeMutablePointer(to: &vmStatInfo) {
            $0.withMemoryRebound(to: integer_t.self, capacity: 1) {
                host_statistics64(mach_host_self(), host_flavor_t(HOST_VM_INFO64), $0, &size)
            }
        }
        guard kernReturn == KERN_SUCCESS else {
            throw KernError.code(kernReturn)
        }
        return vmStatInfo
    }
    
    static func toKb(_ bytes: UInt64) -> UInt64 {
        return bytes / 1024
    }
}

struct SystemControl {
    
    static func bytes(mib: [Int32]) throws -> [Int8] {
        var mib: [Int32] = mib
        var size: Int = 0
        var result: Int32 = 0
        
        // Get correct size
        result = sysctl(&mib, u_int(mib.count), nil, &size, nil, 0)
        
        guard result == KERN_SUCCESS else { throw KernError.code(result) }
        guard size != 0 else { throw KernError.unexpected }
        
        let data = [Int8](repeating: 0, count: size)
        result = data.withUnsafeBufferPointer { (buffer) -> Int32 in
            sysctl(&mib, u_int(mib.count), UnsafeMutableRawPointer(mutating: buffer.baseAddress), &size, nil, 0)
        }
        guard result == KERN_SUCCESS else { throw KernError.code(result) }
        return data
    }
    
    static func string(mib: [Int32]) throws -> String {
        guard let string = try bytes(mib: mib).withUnsafeBufferPointer({ dataPointer -> String? in
            dataPointer.baseAddress.flatMap { String(validatingUTF8: $0) }
        }) else {
            throw "CodingError.encodingFailed"
        }
        return string
    }
    
    static func value<T>(mib: [Int32]) throws -> T {
        return try bytes(mib: mib).withUnsafeBufferPointer({ (buffer) throws -> T in
            guard let baseAddress = buffer.baseAddress else { throw KernError.unexpected }
            return baseAddress.withMemoryRebound(to: T.self, capacity: 1, { $0.pointee })
        })
    }
}

struct MemoryInfo {
    
    struct System {
        let active: UInt64
        let free: UInt64
        let inactive: UInt64
        let wired: UInt64
        let compressed: UInt64
        let used: UInt64
        let total: UInt64
        let swapins: UInt64
        let swapouts: UInt64
        
        init() throws {
            let vmStatistics64 = try Statistics.vmStatistics64()
            let pageSize = UInt64(getpagesize())
            let pageSizeKb = Statistics.toKb(pageSize)
            self.active = UInt64(vmStatistics64.active_count) * pageSizeKb
            self.free = UInt64(vmStatistics64.free_count) * pageSizeKb
            self.inactive = UInt64(vmStatistics64.inactive_count) * pageSizeKb
            self.wired = UInt64(vmStatistics64.wire_count) * pageSizeKb
            self.compressed = UInt64(vmStatistics64.compressor_page_count) * pageSizeKb
            self.used = self.active + self.inactive + self.wired
            self.total = self.used + self.free
            
            self.swapins = vmStatistics64.swapins
            self.swapouts = vmStatistics64.swapouts
        }
    }
    
    struct Process {
        let resident: UInt64
        let residentPeak: UInt64
        let virtual: UInt64
        
        init() throws {
            let taskVmInfo = try Statistics.taskVmInfo()
            
            self.resident = Statistics.toKb(UInt64(taskVmInfo.resident_size))
            self.residentPeak = Statistics.toKb(UInt64(taskVmInfo.resident_size_peak))
            self.virtual = Statistics.toKb(UInt64(taskVmInfo.virtual_size))
        }
    }
    
    struct Swap {
        let total: UInt64
        let used: UInt64
        let free: UInt64
        
        init() throws {
            let usage: xsw_usage = try SystemControl.value(mib: [CTL_VM, VM_SWAPUSAGE])
            self.total = Statistics.toKb(UInt64(usage.xsu_total))
            self.free = Statistics.toKb(UInt64(usage.xsu_avail))
            self.used = Statistics.toKb(UInt64(usage.xsu_used))
        }
    }
}

extension ProcessInfo {
    
    static func startTime() throws -> Int {
        let kinfo: kinfo_proc = try SystemControl.value(mib: [CTL_KERN, KERN_PROC, KERN_PROC_PID, getpid()])
        
        return kinfo.kp_proc.p_starttime.tv_sec
    }
    
    static func age() throws -> Int {
        let startTime = try self.startTime()
        var currentTime = timeval()
        let result: kern_return_t = gettimeofday(&currentTime, nil)
        guard result == KERN_SUCCESS else {
            throw KernError.code(result)
        }
        return currentTime.tv_sec - startTime
    }
    
    static func numberOfThreads() throws -> UInt {
        var threads: thread_act_array_t?
        var count = mach_msg_type_number_t()
        
        let result: kern_return_t = task_threads(mach_task_self_, &threads, &count)
        guard result == KERN_SUCCESS else {
            throw KernError.code(result)
        }
        guard let threadsActArray = threads else {
            throw KernError.unexpected
        }
        defer {
            let vmSize = vm_size_t(count * natural_t(MemoryLayout<thread_t>.size))
            vm_deallocate(mach_task_self_, vm_address_t(threadsActArray.pointee), vmSize)
        }
        return UInt(count)
    }
}

struct System {
    
    static func boottime() throws -> time_t {
        let bootTime: timeval = try SystemControl.value(mib: [CTL_KERN, KERN_BOOTTIME])
        return bootTime.tv_sec
    }
    
    static func uptime() throws -> Int {
        let bootTime = try self.boottime()
        var currentTime = timeval()
        let result: kern_return_t = gettimeofday(&currentTime, nil)
        guard result == KERN_SUCCESS else {
            throw KernError.code(result)
        }
        return currentTime.tv_sec - bootTime
    }
    
    static func machine() throws -> String {
        return try SystemControl.string(mib: [CTL_HW, HW_MACHINE])
    }
    
    static func model() throws -> String {
        return try SystemControl.string(mib: [CTL_HW, HW_MODEL])
    }
}

// MARK: - Processor statistics
struct Processor {
    let cpuTicks: CpuTicks
    let taskBasicInfo: mach_task_basic_info
    let taskEventsInfo: task_events_info
    let processorSetLoadInfo: processor_set_load_info
    
    init() throws {
        self.cpuTicks = try CpuTicks(cpu_tick: Statistics.hostCpuLoadInfo().cpu_ticks)
        self.taskBasicInfo = try Statistics.machTaskBasicInfo()
        self.taskEventsInfo = try Statistics.taskEventsInfo()
        self.processorSetLoadInfo = try Statistics.processorSetLoadInfo()
    }
}

// MARK: - Error type
enum KernError: Error {
    case code(_ value: Int32)
    case unexpected
}

// MARK: - CPU ticks
extension Processor {
    struct CpuTicks {
        let user: Double
        let system: Double
        let idle: Double
        let nice: Double
        
        init(user: UInt = 0, system: UInt = 0, idle: UInt = 0, nice: UInt = 0) {
            let total = Double(user + system + idle + nice)
            self.user = Double(user) / total
            self.system = Double(system) / total
            self.idle = Double(idle) / total
            self.nice = Double(nice) / total
        }
        
        //swiftlint:disable identifier_name large_tuple
        init(cpu_tick: (UInt32, UInt32, UInt32, UInt32)) {
            //swiftlint:enable identifier_name large_tuple
            self.init(user: UInt(cpu_tick.0),
                      system: UInt(cpu_tick.1),
                      idle: UInt(cpu_tick.2),
                      nice: UInt(cpu_tick.3))
        }
    }
}

extension Array where Element == Int8 {
    func stringValue() throws -> String {
        guard let string = String(validatingUTF8: self) else {
            throw "CodingError.encodingFailed"
        }
        return string
    }
}



extension String: Error {}
