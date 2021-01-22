# Backtrace Unity Release Notes

## Version 3.3.0
- `BacktraceReport` stack trace now includes the file name of the stack frame.
- Performance improvements:
  - JSON algorithm performance improvements - avoid analyzing data types.
  - improved library attributes management.
  - improved Unity logs management.
- Support for Low Memory error reports on Anrdoid and iOS (these are sometimes referred to as OOM or Out Of Memory errors). If a low memory situation is detected, backtrace-unity will attempt to generate and submit a native error report to the Backtrace instance. The report will have the `error.type` value of `Low Memory`.
- New support for hang detection on Android and iOS. If a game experiences non responsiviness after 5 seconds, backtrace-unity will generate an error report to the Backtrace instance. The report will have the `error.type` value of `Hang`.  


## Version 3.2.6
- `BacktraceClient` will apply sampling only to errors lacking exception information.
- Fixed annotations nullable value.
- Renamed `BacktraceUnhandledException` classifier, which was generated from a Debug.LogError call, to `error`.
- Fixed nullable environment annotation value.

## Version 3.2.5
- Added `BacktraceClient` Initialization method that allows developer to intialize Backtrace integration without adding game object to game scene.
- Fixed invalid `meta` file for iOS integration for Unity 2019.2.13f1.
- HTTP communication messages improvements - right now Backtrace-Unity plugin will print only one error message when network failure happen. Backtrace-Unity will stop printing failures until next successfull report upload.
- Sampling skip fraction - Enables a new random sampling mechanism for BacktraceUnhandledExceptions (errors from Debug.LogError), by setting default sampling  equal to 0.01 - which means only 1% of randomly sampled Debug.LogError reports will be send to Backtrace. If you would like to send all Debug.LogError to Backtrace - please replace 0.01 value with 1. 

**Be aware**

By default Backtrace library will send only 1% of your Debug.LogError reports - please change this value if you would like to send more Debug.LogErrors to server.



## Version 3.2.4
- Fixed Backtrace-Unity NDK integration database path.

## Version 3.2.3
- Backtrace offline database will now store 8 reports by default. Previously this was not set by default.  
- HTTP client communication improvements
- Improvements in UPM
- Updated symbolication strategy on iOS crashes

## Version 3.2.2
- Fixed native iOS attributes

## Version 3.2.1
- Android stack trace parser improvements,
- Fixed Android NDK initialization when database directory doesn't exist,
- Added Privacy section to Readme


## Version 3.2.0
- This release adds the ability to capture native iOS crashes from Unity games deployed to iOS. The Backtrace Configuration now exposes a setting for games being prepared for iOS to choose `Capture native crashes`. When enabled, the backtrace-unity client will capture and submit native iOS crashes to the configured Backtrace instance. To generate human readable callstacks, game programmers will need to generate and upload appropriate debug symbols.
- Added default uname.sysname attributes for some platforms. The following is the list of uname.sysname platforms that can be populated. list "Android, IOS, Linux, Mac OS, ps3, ps4, Samsung TV, tvOS, WebGL, WiiU, Switch, Xbox". Note 'Switch' had previously been reported as 'switch'
- Added a new attribute 'error.type' that allows developers to quickly filter error reports based on the type of error - The list includes "Crash, Message, Hang, Unhandled Exception, Exception".
- Updated Android NDK libraries used by Unity plugin.

## Version 3.1.2
- `BacktraceData` allows to edit list of environment variables collected by `BacktraceAnnotations`
- `SourceCode` object description for PII purpose
- `Annotations` class exposes EnvironmentVariableCache dictionary - dictionary that stores environment variables collected by library. For example - to replace `USERNAME` environment variable collected by Backtrace library with random string you can easily edit annotations environment varaible and Backtrace-Untiy will reuse them on report creation.

```csharp
Annotations.EnvironmentVariablesCache["USERNAME"] = "%USERNAME%";
```

Also you can still use BeforeSend event to edit collected diagnostic data:
```csharp
  client.BeforeSend = (BacktraceData data) =>
  {
      data.Annotation.EnvironmentVariables["USERNAME"] = "%USERNAME%";
      return data;
  }
```

## Version 3.1.1
- Prevent erroneously extending backtraceClient attributes with backtraceReport attributes.
- Removed randomly generated path to assembly from callstacks.
- Prevent client from multi initialization.

## Version 3.1.0
This release adds an ability to capture native NDK crashes from Unity games deployed on Android. The Backtrace Configuration now exposes a setting for games being prepared for Android OS to choose `Capture native crashes`. When enabled, Backtrace will capture and symbolicate native stack traces from crashes impacting the Unity Engine or any Unity Engine Plugin.

When develoing for Andriod, Unity users who want to debug native NDK crash report can specify a Backtrace Symbols Server Token to support the optional uploading of debug symbols from Unity Editor to Backtrace during build. Uploaded symbols are needed to generate human readable stack trace with proper function names for identifying issues. 

Backtrace library now allows to set client attributes, that will be included in every report. In addition to that, Backtrace client attributes will be available in the native crashes generated by Android games.

To setup client attributes you can simply type
```csharp
BacktraceClient["name-of-attribute"] = "value-of-attribute";
```
If you already have dictionary of attributes you can use `SetAttributes` method. 

## Version 3.0.4
Preliminary Nintendo Switch support has been introduced. The offline database is not currently supported in this version, but will be included in an upcoming release.

## Version 3.0.3
This release includes significant improvements to performance by way of report filtering as well as improved performance diagnostics. Learn more below.
 
- `BacktraceClient` now supports report filtering. Report filtering is enabled by using the `Filter reports` option in the user interface or for more advanced use-cases, the `SkipReport` delegate available in the BacktraceClient.
 
Sample code: 
```csharp
  // Return true to ignore a report, return false to handle the report
  // and generate one for the error.
  BacktraceClient.SkipReport = (ReportFilterType type, Exception e, string msg) =>
  {
    // ReportFilterType is one of None, Message, Exception,
    // UnhandledException or Hang. It is also possible to
    // to filter based on the exception and exception message.

    // Report hangs and crashes only.
    return type != ReportFilterType.Hang && type != ReportFilterType.UnhandledException;
  };
```
 
For example, to only get error reporting for hangs or crashes then only return false for Hang or UnhandledException or set the corresponding options in the user interface as shown below.

![Sample report filter](./Documentation~/images/report-filter.PNG)
- Support for backtrace-unity timing observability. To enable sending performance information to Backtrace set the`Enable performance statistics` option in the UI. Attributes are created under the performance.* namespace, time unit is microseconds: 
  * Report creation time (`performance.report`),
  * JSON serialization time (`performance.json`),
  * Database add operation time (`performance.database`),
  * Database single send method time (`performance.send`),
  * Database single flush method time (`performance.flush`)
- Improvements to JIT stack frame parsing.

## Version 3.0.2
- `BacktraceDatabase` now provides a new `Send` method. This method will try to send all objects from the database respecting the client side deduplication and retry setting. This can be used as an alternative to the `Flush` method which will try to send all objects from the database ignoring any client side deduplication and retry settings.
- `BacktraceClient` has been optimized to only serialize data as needed.
- `BacktraceDatabase` `AutoSend` function has been optimized for performance improvements.
- `BacktraceClient` by default will generate configuration file with client rate limit equal to 50.
- Fixed invalid meta file.

## Version 3.0.1
- The `BacktraceDatabase` class will now create database directory before final database validation. Previously, when directory didn't exist, BacktraceDatabase was disabled.
- The `BacktraceDatabase` field now allows users to pass interpolated string in Database options. Developer can use `${Application.dataPath}` or `${Application.persistentDataPath}` to set path to database. 
- The backtrace-unity library will generate screenshot image files in .jpg format. 
- Optimizations in ANR watchdog and `BacktraceLogManager` initialization - The Backtrace client will now create a class instance of `BacktraceAnrWatchdog` class, instead of creating instance via static class method. The `BacktraceLogManager` (class responsible for storing log data) will be initialized in `CaptureUnityMessages`. 

## Version 3.0.0

*New Features*
- The backtrace-unity library (Backtrace) now allows detection of ANR (Application not responding) events on Android devices.
- Unhandled exception output from the Unity runtime and executables is now prominently displayed in the Debugger.
- Backtrace will try to guess unhandled exception classifier based on exception message/stack trace.
- Backtrace now allows you to add Unity player.log file as an attachment.
- Backtrace now allows you to add a screenshot as an attachment when an exception occured.
- Backtrace now allows you to capture last n lines of game logs. You can define how many lines of logs Backtrace should store by settings `Collect last n number of logs` property in the Unity editor.
- Backtrace will capture any native Unity Engine crash dumps on Windows OS.
- Backtrace will capture and index native metadata from the Android OS as Backtrace Attribuces, including vm, system.memory, and device details. 
- Backtrace allows you control whether or not a report should send via the BeforeSend event. If you return a null value from a BeforeSend event, Backtrace will discard the report and not send.

*General Improvements*
- `BacktraceClient`, `BacktraceReport`, `BacktraceData`, `BacktraceData` and `BacktraceDatabase` now allow users to pass attributes in dictionary form with string key and string values. Attributes must now be provided with the Dictionary<string, string> data structure to allow the serializer to be as fast as possible. 
- Removed dependancy on 3rd party JSON.NET library to reduce the size of the package. Backtrace-unity now provides it's own serializer for BacktraceReport usage.
- Further reduction in size of Backtrace.Unity assembly with BacktraceJObject.
- `BacktraceDatabase` won't try to deserialize `BacktraceReport` anymore - because of that, callback api won't return `BacktraceReport` object in `BacktraceResult`.
- `BacktraceDatabase` won't use anymore `Add` method with BacktraceReport parameter. Instead, `BacktraceDaatabase` will use `Add` method with BacktraceData parameter. Previous `Add` method is deprecated and will be removed in next major release.
- Support has been improved for parsing unhandled exception output from the Unity runtime and Unity executables.

**NOTE:** When migrating from previous releases, there is an API change that developers will want to uptake for attribute submission. Specifically, attribute definitions previously used a signature
attributes: `new Dictionary<string, object>() { { "key", "value" } }`, 
In this release, we made a change to require a string for the value instead of an object for faster performance.
attributes: `new Dictionary<string, string>() { { "key", "value" } }`, 


*Bug Fixes*
- Annotation name typo - updated `children` from `childrens`

## Version 2.1.6

- Handling special case for string reports fingerprint when stack trace is empty.

## Version 2.1.5

- Backtrace Unity plugin UI improvements - added tooltips, headers and collapsible menu for advanced options.
- Changed Client-side deduplication menu,
- `BacktraceClient` now allows you to choose what type of fingerprint Backtrace should generate for reports without stack trace. `Use normalized exception message` allows you to use a normalized exception message to generate fingerprint, instead of stack trace.
- Added exception source code information to exception and message type of reports.

## Version 2.1.4

- `EnvironmentVariable` class now will handle correctly nullable key/values,
- `BacktraceAttributes` handle correctly nullable values.

## Version 2.1.3

- `BacktraceUnhandledException` will generate environment stack trace if Unity stack trace is empty. BacktraceReport will still generate normalized fingerprint for unhandled exception without stack trace.
- `BacktraceUnhandledException` will provide information from Unity Error logger in source code property, which should improve error analysis in web debugger.
- `BacktraceAttributes` won't try to collect `Annotations` anymore.
- `Annotations` won't use ComplexAttributes property anymore.

## Version 2.1.2

- `BacktraceReport` will generate report fingerprint for exceptions without stack trace.
- Changed game object depth default property value.
- Added Exception information to the Annotation object.

## Version 2.1.1

- UPM modifications - fixed editor assembly definition,
- Hiding Documentation and Scripts folders
- Added Mac and Rider files to .gitignore
- Moved Backtrace Configuration create menu deeper into the hierarchy

## Version 2.1.0

- UPM support - changed project structure and divide Backtrae-unity plugin into assemblies.

## Version 2.0.5

- Unity compatibility patch - .NET2.0, .NET 3.5 support (https://github.com/backtrace-labs/backtrace-unity/pull/10).
- Untiy .NET Standard 2.0 support.
- Expose minidump type option to Backtrace Client configuration in the UI.
- Changed values of LangVersion to Mono or IL2CPP, depending on which is deployed.
- Changed `Game object depth` property - default to `-1`, which means not to include Game Objects Hierarchy as an Annotation in the error report. Set the value to `1` to collect one level deep of Gane Object hierarchy, `2` to collect two levels deep, and so on. Setting the value to `0` will collect the full depth, which may be rather large if you have a lot of children.

## Version 2.0.4

- Added Game object depth property that allows developer to filter game object childrens in Backtrace report
- Changed "Destroy client on new scene load" label. Now: "Destroy client on new scene load (false - Backtrace managed),
- added namespaces to `XmlNodeConverter` class,
- Added correct path to source file in `BacktraceUnhnandledException`,
- Changed line endings in `BacktraceDatabase`, `ReportLimitWatcher`, `BacktraceClient` files,
- Changed `ReactTransform` casting to `Component` in `Annotations` class. With this change Backtrace library should correctly send all game objects to Backtrace,
- Changed a way how we guess game assets directory.

## Version 2.0.3

- Annotations object will validate game object before converting it.

## Version 2.0.2

- Fixed invalid cast for nested game objects in Backtrace Attributes,
- BacktraceClient will print message only once per report rate limit hit per 1 minute.
- `BacktraceDatabase` `Send` method will check client rate limit after each send.
- `BacktraceClient` and `BacktraceDatabase` won't generate warning on `Disabled` event.

## Version 2.0.1

- `BacktraceApi` won't print anymore Error message when Backtrace-integration cannot send data to Backtrace. Now `BacktraceApi` will print warning instead.

## Version 2.0.0

- Backtrace-Unity plugin will set `"Destroy object on new scene"` by default to false.
- Backtrace stack trace improvements,
- `BacktraceDatabase` retry method now respect correctly `BacktraceDatabase` `retryInterval` property,
- New `Backtrace Configuration` won't override existing `Backtrace Configuration` in configuration directory.
- Backtrace-Unity plugin tests now won't override some files in Backtrace-Database unit tests,
- Backtrace-Unity plugin now allows you to setup client-side deduplication rules via `Fingerprint`. By using this field you can limit reporting of an error that occurs many times over a few frames.
- Backtrace report limit watcher feature now will validate limits before BacktraceReport creation.
- `BacktraceClient` and `BacktraceDatabase` now expose `Reload` method. You can use this method do dynamically change `BacktraceClient`/`BacktraceDatabase` configurations.

## Version 1.1.5 - 09.01.2019

- Added support to DontDestroyOnLoad property. Right now users might use this property to store `BacktraceClient`/`BacktraceDatabase` instances between all game scenes.
- Added more attributes to `BacktraceReport` object,
- Added scene game objects information to `BacktraceReport` annotations.

## Version 1.1.4 - 27.08.2019

- Added support for servies under proxy (removed backtrace.sp conditions)

## Version 1.1.3 - 07.06.2019

- Removed error log when unity-plugin receive status code: 200 on attachment upload.

## Version 1.1.2 - 06.06.2019

- Changed a way how Unity-plugin upload attachments to Backtrace via `submit.backtrace.io`

## Version 1.1.1 - 28.03.2019

- Detailed log information when Unity plugin cannot send data to Backtrace,
- Unhandled exception condition that wont catch exceptions that starts with string : `[Backtrace]::`,
- Added support for system stack frames,
- Line ending fix.

## Version 1.1.0 - 06.03.2019

- Support for multiple types of Attribute types - string, char, enum, int, float, double....
- Support for submit.backtrace.io
- If you send exception, `BacktraceReport` will generate stack trace based on exception stack trace. We will no longer include environment stack trace in exception reports,
- `BacktraceDatabase` fix for `FirstOrDefault` invalid read,
- Fixed duplicated global exception handler,
- Fixed typo in debug Attribute,
- Fixed stack trace in `BacktraceUnhandledException` object,

## Version 1.0.0 - 21.11.2018

First Backtrace-Unity plugin version
