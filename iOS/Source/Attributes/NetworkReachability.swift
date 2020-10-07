import Foundation
import SystemConfiguration

// based on: https://github.com/Alamofire/Alamofire/blob/master/Source/NetworkReachabilityManager.swift

final class NetworkReachability {
    
    var flags: SCNetworkReachabilityFlags? {
        guard let reachability = reachability else { return nil }
        var flags = SCNetworkReachabilityFlags()
        
        if SCNetworkReachabilityGetFlags(reachability, &flags) {
            return flags
        }
        
        return nil
    }
    
    private let reachability: SCNetworkReachability?
    
    init() {
        var address = sockaddr_in()
        address.sin_len = UInt8(MemoryLayout<sockaddr_in>.size)
        address.sin_family = sa_family_t(AF_INET)
        
        self.reachability = withUnsafePointer(to: &address, { pointer in
            return pointer.withMemoryRebound(to: sockaddr.self, capacity: MemoryLayout<sockaddr>.size) {
                return SCNetworkReachabilityCreateWithAddress(nil, $0)
            }
        })
    }
}

extension NetworkReachability {
    
    var isReachable: Bool {
        guard let flags = flags else { return false }
        return isNetworkReachable(with: flags)
    }
    
    var statusName: String {
        guard let flags = flags else { return "unknown" }
        guard isNetworkReachable(with: flags) else { return "notReachable" }
        
        #if os(iOS) || os(tvOS)
        if flags.contains(.isWWAN) { return "reachableViaWWAN" }
        #endif
        
        return "reachableViaEthernetOrWiFi"
    }
    
    private func isNetworkReachable(with flags: SCNetworkReachabilityFlags) -> Bool {
        let isReachable = flags.contains(.reachable)
        let needsConnection = flags.contains(.connectionRequired)
        let canConnectAutomatically = flags.contains(.connectionOnDemand) || flags.contains(.connectionOnTraffic)
        let canConnectWithoutUserInteraction = canConnectAutomatically && !flags.contains(.interventionRequired)
        
        return isReachable && (!needsConnection || canConnectWithoutUserInteraction)
    }
}
