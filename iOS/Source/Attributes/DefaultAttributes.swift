// swiftlint:disable type_name
import Foundation
import CoreLocation

final class FaultInfo: AttributesSource {
    var faultMessage: String?
    var immutable: [String: String?] {
        return ["error.message": faultMessage]
    }
}

struct ProcessorInfo: AttributesSource {
    
    var mutable: [String: String?] {
        let processor = try? Processor()
        let processInfo = ProcessInfo.processInfo
        let systemVmMemory = try? MemoryInfo.System()
        let systemSwapMemory = try? MemoryInfo.Swap()
        let processVmMemory = try? MemoryInfo.Process()
        
        return [
            // cpu
            "cpu.idle": processor?.cpuTicks.idle.description,
            "cpu.nice": processor?.cpuTicks.nice.description,
            "cpu.user": processor?.cpuTicks.user.description,
            "cpu.system": processor?.cpuTicks.system.description,
            "cpu.process.count": processor?.processorSetLoadInfo.task_count.description,
            "cpu.thread.count": processor?.processorSetLoadInfo.thread_count.description,
            "cpu.uptime": try? System.uptime().description,
            "cpu.count": processInfo.processorCount.description,
            "cpu.count.active": processInfo.activeProcessorCount.description,
            "cpu.context": processor?.taskEventsInfo.csw.description,
            // process
            "process.thread.count": try? ProcessInfo.numberOfThreads().description,
            "process.age": try? ProcessInfo.age().description,
            // system
            "system.memory.active": systemVmMemory?.active.description,
            "system.memory.inactive": systemVmMemory?.inactive.description,
            "system.memory.free": systemVmMemory?.free.description,
            "system.memory.used": systemVmMemory?.used.description,
            "system.memory.total": systemVmMemory?.total.description,
            "system.memory.wired": systemVmMemory?.wired.description,
            "system.memory.swapins": systemVmMemory?.swapins.description,
            "system.memory.swapouts": systemVmMemory?.swapouts.description,
            "system.memory.swap.total": systemSwapMemory?.total.description,
            "system.memory.swap.used": systemSwapMemory?.used.description,
            "system.memory.swap.free": systemSwapMemory?.free.description,
            // vm
            "process.vm.rss.size": processVmMemory?.resident.description,
            "process.vm.rss.peak": processVmMemory?.residentPeak.description,
            "process.vm.vma.size": processVmMemory?.virtual.description
        ]
    }
    
    var immutable: [String: String?] {
        return [
            "cpu.boottime": try? System.boottime().description,
            // hostanme
            "hostname": ProcessInfo.processInfo.hostName,
            // descriptor
            "descriptor.count": getdtablesize().description,
            "process.starttime": try? ProcessInfo.startTime().description
        ]
    }
}

struct Device: AttributesSource {
    
    var mutable: [String: String?] {
        #if os(iOS)
        let device = UIDevice.current
        var attributes: [String: String?] = ["device.orientation": device.orientation.name]
        if device.isBatteryMonitoringEnabled {
            attributes["battery.state"] = device.batteryState.name
            attributes["battery.level"] = device.batteryLevel.description
        }
        if #available(iOS 11.0, *) {
            attributes["device.nfc.supported"] = "true"
        } else {
            attributes["device.nfc.supported"] = "false"
        }
        return attributes
        #else
        return [:]
        #endif
    }

    var immutable: [String: String?] {
        return [
            "device.machine": try? System.machine().description,
            "device.model": try? System.model().description
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
    
    var immutable: [String: String?] {
        var screenAttributes: Attributes = [:]
        #if os(iOS) || os(tvOS)
        let mainScreen = UIScreen.main
        screenAttributes[Key.scale.rawValue] = mainScreen.scale.description
        screenAttributes[Key.width.rawValue] = mainScreen.bounds.width.description
        screenAttributes[Key.height.rawValue] = mainScreen.bounds.height.description
        screenAttributes[Key.nativeScale.rawValue] = mainScreen.nativeScale.description
        screenAttributes[Key.nativeWidth.rawValue] = mainScreen.nativeBounds.width.description
        screenAttributes[Key.nativeHeight.rawValue] = mainScreen.nativeBounds.height.description
        screenAttributes[Key.count.rawValue] = UIScreen.screens.count.description
        #elseif os(macOS)
        screenAttributes[Key.count.rawValue] = NSScreen.screens.count.description
        if let mainScreen = NSScreen.main {
            screenAttributes[Key.mainScreenWidth.rawValue] = mainScreen.frame.width.description
            screenAttributes[Key.mainScreenHeight.rawValue] = mainScreen.frame.height.description
            screenAttributes[Key.mainScreenScale.rawValue] = mainScreen.backingScaleFactor.description
        }
        #endif
        
        #if os(iOS)
        screenAttributes[Key.brightness.rawValue] = UIScreen.main.brightness.description
        #endif
        return screenAttributes
    }
}

struct LocaleInfo: AttributesSource {
    
   var immutable: [String: String?] {
        var localeAttributes: Attributes = [:]
        if let languageCode = Locale.current.languageCode {
            localeAttributes["device.lang.code"] = languageCode.description
            if let language = Locale.current.localizedString(forLanguageCode: languageCode) {
                localeAttributes["device.lang"] = language.description
            }
        }
        if let regionCode = Locale.current.regionCode {
            localeAttributes["device.region.code"] = regionCode.description
            if let region = Locale.current.localizedString(forRegionCode: regionCode) {
                localeAttributes["device.region"] = region.description
            }
        }
        return localeAttributes
    }
}

struct NetworkInfo: AttributesSource {
    
     var mutable: [String: String?] {
        return ["network.status": NetworkReachability().statusName]
    }
}

struct LocationInfo: AttributesSource {

    var mutable: [String: String?] {
        return [
            "location.enabled": CLLocationManager.locationServicesEnabled().description,
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
