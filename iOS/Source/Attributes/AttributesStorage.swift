import Foundation

final class AttributesStorage {
    struct Config {
        let cacheUrl: URL
        let directoryUrl: URL
        let fileUrl: URL
        
        init(fileName: String) throws {
            guard let cacheDirectoryURL =
                FileManager.default.urls(for: .cachesDirectory, in: .userDomainMask).first else {
                    throw FileError.noCacheDirectory
            }
            self.cacheUrl = cacheDirectoryURL
            self.directoryUrl = cacheDirectoryURL.appendingPathComponent(directoryName)
            self.fileUrl = directoryUrl.appendingPathComponent("\(fileName).plist")
        }
    }
    
    private static let directoryName = Bundle(for: AttributesStorage.self).bundleIdentifier ?? "BacktraceCache"
    
    static func store(_ attributes: Attributes, fileName: String) throws {
        let config = try Config(fileName: fileName)
        
        if !FileManager.default.fileExists(atPath: config.directoryUrl.path) {
            try FileManager.default.createDirectory(atPath: config.directoryUrl.path,
                                                    withIntermediateDirectories: false,
                                                    attributes: nil)
        }
        if #available(iOS 11.0, tvOS 11.0, macOS 10.13, *) {
            try (attributes as NSDictionary).write(to: config.fileUrl)
        } else {
            guard (attributes as NSDictionary).write(to: config.fileUrl, atomically: true) else {
                throw FileError.fileNotWritten
            }
        }
        print("Stored attributes at path: \(config.fileUrl)")
    }
    
    static func retrieve(fileName: String) throws -> Attributes {
        let config = try Config(fileName: fileName)
        guard FileManager.default.fileExists(atPath: config.fileUrl.path) else {
            print("Attribuets don't exist");
            throw FileError.fileNotExists
        }
        // load file to NSDictionary
        let dictionary: NSDictionary
        if #available(iOS 11.0, tvOS 11.0, macOS 10.13, *) {
            dictionary = try NSDictionary(contentsOf: config.fileUrl, error: ())
        } else {
            guard let dictionaryFromFile = NSDictionary(contentsOf: config.fileUrl) else {
                throw FileError.invalidPropertyList
            }
            dictionary = dictionaryFromFile
        }
        // cast safety to AttributesType
        guard let attributes: Attributes = dictionary as? Attributes else {
            throw FileError.invalidPropertyList
        }
        print("Retrieved attributes from path: \(config.fileUrl)")
        return attributes
    }
    
    static func remove(fileName: String) throws {
        let config = try Config(fileName: fileName)
        // check file exists
        guard FileManager.default.fileExists(atPath: config.fileUrl.path) else {
            throw FileError.fileNotExists
        }
        // remove file
        try FileManager.default.removeItem(at: config.fileUrl)
        print("Removed attributes at path: \(config.fileUrl)")
    }
}


extension FileError: Error {}

enum FileError {
    case unsupportedScheme
    case fileNotExists
    case resourceValueUnavailable
    case noCacheDirectory
    case fileNotWritten
    case invalidPropertyList
}
