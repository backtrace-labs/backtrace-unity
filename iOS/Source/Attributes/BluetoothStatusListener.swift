import Foundation
import CoreBluetooth

final class BluetoothStatusListener: NSObject {
    
    private var bluetoothCentralManager: CBCentralManager
    private(set) var currentState: String = "unknown"
    
    override init() {
        let options = [CBCentralManagerOptionShowPowerAlertKey: 0] // magic bit!
        let queue = DispatchQueue(label: "backtrace.bluetooth.listener", qos: DispatchQoS.background)
        bluetoothCentralManager = CBCentralManager(delegate: nil, queue: queue, options: options)
        super.init()
        bluetoothCentralManager.delegate = self
    }
}

// MARK: - CBCentralManagerDelegate
extension BluetoothStatusListener: CBCentralManagerDelegate {
    
    func centralManagerDidUpdateState(_ central: CBCentralManager) {
        switch central.state {
        case .poweredOn: currentState = "poweredOn"
        case .poweredOff: currentState = "poweredOff"
        case .resetting: currentState = "resetting"
        case .unauthorized: currentState = "unauthorized"
        case .unsupported: currentState = "unsupported"
        case .unknown: currentState = "unknown"
        }
    }
}

extension BluetoothStatusListener: AttributesSource {
    var mutable: [String: Any?] {
        return ["bluetooth.state": currentState]
    }
}
