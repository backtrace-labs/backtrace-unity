// swiftlint:disable type_name
import Foundation
import CoreLocation

final class FaultInfo: AttributesSource {
    var faultMessage: String?
    var immutable: [String: Any?] {
        return ["error.message": faultMessage]
    }
}

struct ProcessorInfo: AttributesSource {
    
    var mutable: [String: Any?] {
        let processor = try? Processor()
        let processInfo = ProcessInfo.processInfo
        let systemVmMemory = try? MemoryInfo.System()
        let systemSwapMemory = try? MemoryInfo.Swap()
        let processVmMemory = try? MemoryInfo.Process()
        
        return [
            // cpu
            "cpu.idle": processor?.cpuTicks.idle,
            "cpu.nice": processor?.cpuTicks.nice,
            "cpu.user": processor?.cpuTicks.user,
            "cpu.system": processor?.cpuTicks.system,
            "cpu.process.count": processor?.processorSetLoadInfo.task_count,
            "cpu.thread.count": processor?.processorSetLoadInfo.thread_count,
            "cpu.uptime": try? System.uptime(),
            "cpu.count": processInfo.processorCount,
            "cpu.count.active": processInfo.activeProcessorCount,
            "cpu.context": processor?.taskEventsInfo.csw,
            // process
            "process.thread.count": try? ProcessInfo.numberOfThreads(),
            "process.age": try? ProcessInfo.age(),
            // system
            "system.memory.active": systemVmMemory?.active,
            "system.memory.inactive": systemVmMemory?.inactive,
            "system.memory.free": systemVmMemory?.free,
            "system.memory.used": systemVmMemory?.used,
            "system.memory.total": systemVmMemory?.total,
            "system.memory.wired": systemVmMemory?.wired,
            "system.memory.swapins": systemVmMemory?.swapins,
            "system.memory.swapouts": systemVmMemory?.swapouts,
            "system.memory.swap.total": systemSwapMemory?.total,
            "system.memory.swap.used": systemSwapMemory?.used,
            "system.memory.swap.free": systemSwapMemory?.free,
            // vm
            "process.vm.rss.size": processVmMemory?.resident,
            "process.vm.rss.peak": processVmMemory?.residentPeak,
            "process.vm.vma.size": processVmMemory?.virtual
        ]
    }
    
    var immutable: [String: Any?] {
        return [
            "cpu.boottime": try? System.boottime(),
            // hostanme
            "hostname": ProcessInfo.processInfo.hostName,
            // descriptor
            "descriptor.count": getdtablesize(),
            "process.starttime": try? ProcessInfo.startTime()
        ]
    }
}

struct Device: AttributesSource {
    
    var mutable: [String: Any?] {
        #if os(iOS)
        let device = UIDevice.current
        var attributes: [String: Any?] = ["device.orientation": device.orientation.name]
        if device.isBatteryMonitoringEnabled {
            attributes["battery.state"] = device.batteryState.name
            attributes["battery.level"] = device.batteryLevel
        }
        if #available(iOS 11.0, *) {
            attributes["device.nfc.supported"] = true
        } else {
            attributes["device.nfc.supported"] = false
        }
        return attributes
        #else
        return [:]
        #endif
    }

    var immutable: [String: Any?] {
        return [
            "device.machine": try? System.machine(),
            "device.model": try? System.model()
        ]
    }
}

struct ScreenInfo: AttributesSource {
    
    private enum Key: String {
        case count = "screens.count"
        #if os(iOS) || os(tvOS)
        case scale = "screen.scale"
        case width = "screen.width"
        case height = "screen.height"
        case nativeScale = "screen.scale.native"
        case nativeWidth = "screen.width.native"
        case nativeHeight = "screen.height.native"
        case brightness = "screen.brightness"
        #elseif os(macOS)
        case mainScreenWidth = "screen.main.width"
        case mainScreenHeight = "screen.main.height"
        case mainScreenScale = "screen.main.scale"
        #endif
    }
    
    var immutable: [String: Any?] {
        var screenAttributes: Attributes = [:]
        #if os(iOS) || os(tvOS)
        let mainScreen = UIScreen.main
        screenAttributes[Key.scale.rawValue] = mainScreen.scale
        screenAttributes[Key.width.rawValue] = mainScreen.bounds.width
        screenAttributes[Key.height.rawValue] = mainScreen.bounds.height
        screenAttributes[Key.nativeScale.rawValue] = mainScreen.nativeScale
        screenAttributes[Key.nativeWidth.rawValue] = mainScreen.nativeBounds.width
        screenAttributes[Key.nativeHeight.rawValue] = mainScreen.nativeBounds.height
        screenAttributes[Key.count.rawValue] = UIScreen.screens.count
        #elseif os(macOS)
        screenAttributes[Key.count.rawValue] = NSScreen.screens.count
        if let mainScreen = NSScreen.main {
            screenAttributes[Key.mainScreenWidth.rawValue] = mainScreen.frame.width
            screenAttributes[Key.mainScreenHeight.rawValue] = mainScreen.frame.height
            screenAttributes[Key.mainScreenScale.rawValue] = mainScreen.backingScaleFactor
        }
        #endif
        
        #if os(iOS)
        screenAttributes[Key.brightness.rawValue] = UIScreen.main.brightness
        #endif
        return screenAttributes
    }
}

struct LocaleInfo: AttributesSource {
    
   var immutable: [String: Any?] {
        var localeAttributes: Attributes = [:]
        if let languageCode = Locale.current.languageCode {
            localeAttributes["device.lang.code"] = languageCode
            if let language = Locale.current.localizedString(forLanguageCode: languageCode) {
                localeAttributes["device.lang"] = language
            }
        }
        if let regionCode = Locale.current.regionCode {
            localeAttributes["device.region.code"] = regionCode
            if let region = Locale.current.localizedString(forRegionCode: regionCode) {
                localeAttributes["device.region"] = region
            }
        }
        return localeAttributes
    }
}

struct NetworkInfo: AttributesSource {
    
     var mutable: [String: Any?] {
        return ["network.status": NetworkReachability().statusName]
    }
}

struct LocationInfo: AttributesSource {

    var mutable: [String: Any?] {
        return [
            "location.enabled": CLLocationManager.locationServicesEnabled(),
            "location.authorization.status": CLLocationManager.authorizationStatus().name
        ]
    }
}

// swiftlint:enable type_name

private extension CLAuthorizationStatus {
    
    var name: String {
        switch self {
        case .authorizedAlways: return "Always"
        case .authorizedWhenInUse: return "WhenInUse"
        case .denied: return "Denied"
        case .notDetermined: return "notDetermined"
        case .restricted: return "restricted"
        }
    }
}

#if os(iOS)
private extension UIDeviceOrientation {
    
    var name: String {
        switch self {
        case .faceDown: return "FaceDown"
        case .faceUp: return "FaceUp"
        case .landscapeLeft: return "LandscapeLeft"
        case .landscapeRight: return "LandscapeRight"
        case .portrait: return "Portrait"
        case .portraitUpsideDown: return "PortraitUpsideDown"
        case .unknown: return "Unknown"
        }
    }
}
#endif

#if os(iOS)
private extension UIDevice.BatteryState {
    
    var name: String {
        switch self {
        case .charging: return "Charging"
        case .full: return "Full"
        case .unknown: return "Unknown"
        case .unplugged: return "Unplugged"
        }
    }
}
#endif

#if os(iOS)
private extension UIApplication.State {
    
    var name: String {
        switch self {
        case .active: return "Active"
        case .background: return "Background"
        case .inactive: return "Inactive"
        }
    }
}
#endif
