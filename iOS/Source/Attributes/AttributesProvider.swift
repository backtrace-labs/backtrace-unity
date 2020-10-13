import Foundation

protocol AttributesSource {
    var immutable: [String: String?] { get }
    var mutable: [String: String?] { get }
}

extension AttributesSource {
    var immutable: [String: String?] { return [:] }
    var mutable: [String: String?] { return [:] }
}

final class AttributesProvider {
    
    // attributes can be modified on runtime
    var attributes: Attributes = [:]
    let attributesSources: [AttributesSource]
    private let faultInfo: FaultInfo
    
    lazy var immutable: Attributes = {
        return attributesSources.map(\.immutable).merging()
    }()
    
    init() {
        faultInfo = FaultInfo()
        attributesSources = [ProcessorInfo(),
            Device(),
            ScreenInfo(),
            LocaleInfo(),
            NetworkInfo(),
            LocationInfo(),
            faultInfo]
    }
}

extension AttributesProvider: SignalContext {
    func set(faultMessage: String?) {
        self.faultInfo.faultMessage = faultMessage
    }
    
    var allAttributes: Attributes {
        return attributes + defaultAttributes
    }
    
    var defaultAttributes: Attributes {
        return immutable + attributesSources.map(\.mutable).merging()
    }
}

extension AttributesProvider: CustomStringConvertible, CustomDebugStringConvertible {
    var description: String {
        return allAttributes.compactMap { "\($0.key): \($0.value)"}.joined(separator: "\n")
    }
    
    var debugDescription: String {
        return description
    }
}

extension Array where Element == [String: String?] {
    func merging() -> [String: String] {
        let keyValuePairs = reduce([:], +).compactMap({ (key: String, value: String?) -> (key: String, value: String)? in
            guard let value = value else {
                return nil
            }
            return (key, value)
        })
        return Dictionary(keyValuePairs) { (lhs, _) in lhs }
    }
}

extension Dictionary {
    
    static func + (lhs: Dictionary, rhs: Dictionary) -> Dictionary {
        return lhs.merging(rhs, uniquingKeysWith: {_, new in new})
    }
}
