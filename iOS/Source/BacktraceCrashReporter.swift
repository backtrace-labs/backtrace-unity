import Foundation
import CrashReporter


@objc public class BacktraceCrashReporter: NSObject {
    private let reporter: PLCrashReporter
    static private let crashName = "live_report"
    private(set) var attributesProvider: SignalContext
    private let backtraceUrl: URL;
    
    @objc public init(url: NSString, attributes: NSDictionary) {
        self.backtraceUrl = URL(string: url as String)!
        reporter = PLCrashReporter(configuration: PLCrashReporterConfig.defaultConfiguration())
        self.attributesProvider = AttributesProvider()
        self.attributesProvider.attributes = attributes as! Attributes;
    }
    
    @objc public func start() {
        self.signalContext(&self.attributesProvider);
        self.sendPendingCrashes();
        reporter.enable();
    }
    
    // allows to still fetch attributes for integration without crash reporter enabled
    @objc public static func getAttributes() -> NSDictionary {
        let attributesSources = [ProcessorInfo(),
            Device(),
            ScreenInfo(),
            LocaleInfo(),
            NetworkInfo(),
            LocationInfo()] as [AttributesSource];
        return  attributesSources.map(\.mutable).merging() as NSDictionary;
    }
    
    @objc public func setAttributes(key: NSString, value: NSString) {
        self.attributesProvider.attributes[key as String] = value as String;
    }
    
    private func sendPendingCrashes() {
        if(!reporter.hasPendingCrashReport()) {
            return;
        }
        do {
            let report = try reporter.loadPendingCrashReportDataAndReturnError();
            let attributes = (try? AttributesStorage.retrieve(fileName: BacktraceCrashReporter.crashName)) ?? [:];
            self.send(data: report, attributes: attributes);
        } catch {
            print("Cannot fetch pending crash report data");
        }
    }
    
    func signalContext(_ mutableContext: inout SignalContext) {
        
        
        let handler: @convention(c) (_ signalInfo: UnsafeMutablePointer<siginfo_t>?,
            _ uContext: UnsafeMutablePointer<ucontext_t>?,
            _ context: UnsafeMutableRawPointer?) -> Void = { signalInfoPointer, _, context in
                guard let attributesProvider = context?.assumingMemoryBound(to: SignalContext.self).pointee,
                let signalInfo = signalInfoPointer?.pointee else {
                        return
                }
                attributesProvider.set(faultMessage: "siginfo_t.si_signo: \(signalInfo.si_signo)");
                try? AttributesStorage.store(attributesProvider.allAttributes, fileName: BacktraceCrashReporter.crashName)
        }
        
        var callbacks = withUnsafeMutableBytes(of: &mutableContext) { rawMutablePointer in
            PLCrashReporterCallbacks(version: 0, context: rawMutablePointer.baseAddress, handleSignal: handler)
        }
        reporter.setCrash(&callbacks)
    }
    
    private func send(data: Data, attributes: Attributes) {
        var urlRequest = URLRequest(url: self.backtraceUrl)
        urlRequest.httpMethod = "POST"
        let boundary =  "Boundary-\(NSUUID().uuidString)"
        urlRequest.setValue("multipart/form-data; boundary=\(boundary)", forHTTPHeaderField: "Content-Type")
        
        let boundaryPrefix = "--\(boundary)\r\n"
        let body = NSMutableData()
        // attributes
        for attribute in attributes {
            body.appendString(boundaryPrefix)
            body.appendString("Content-Disposition: form-data; name=\"\(attribute.key)\"\r\n\r\n")
            body.appendString("\(attribute.value)\r\n")
        }
        // report file
        body.appendString(boundaryPrefix)
        body.appendString("Content-Disposition: form-data; name=\"upload_file\"; filename=\"upload_file\"\r\n")
        body.appendString("Content-Type: application/octet-stream\r\n\r\n")
        body.append(data)
        body.appendString("\r\n")
        
        body.appendString("\(boundaryPrefix)--")
        urlRequest.httpBody = body as Data
        urlRequest.setValue("\(body.length)", forHTTPHeaderField: "Content-Length")
        
        let task = URLSession.shared.dataTask(with: urlRequest) { data, response, error in
            guard let data = data,
                let response = response as? HTTPURLResponse,
                error == nil else {
                    print("error", error ?? "Unknown error")
                    return
            }
            
            guard (200 ... 299) ~= response.statusCode else {
                print("statusCode should be 2xx, but is \(response.statusCode)")
                print("response = \(response)")
                return
            }
            self.reporter.purgePendingCrashReport();
            try? AttributesStorage.remove(fileName: BacktraceCrashReporter.crashName);
        }
        
        task.resume()
    }
}

public typealias Attributes = [String: String]



private extension NSMutableData {
    
    func appendString(_ string: String) {
        guard let data = string.data(using: String.Encoding.utf8, allowLossyConversion: false) else { return }
        append(data)
    }
}

protocol SignalContext: CustomStringConvertible {
    var allAttributes: Attributes { get }
    var attributes: Attributes { get set }
    func set(faultMessage: String?)
}
