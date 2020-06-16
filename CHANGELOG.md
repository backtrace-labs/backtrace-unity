# Backtrace Unity Release Notes

## Version 3.0.0

*New Features*
- The backtrace-unity library (Backtrace) now allows detection of ANR (Application not responding) events on Android devices.
- Unhandled exception output from the Unity runtime and executables is now prominently displayed in the Debugger.
- Backtrace will try to guess unhandled exception classifier based on exception message/stack trace.
- Backtrace now allows you to add Unity player.log file as an attachment.
- Backtrace now allows you to add a screenshot as an attachment when an exception occured.
- Backtrace now allows you to capture last n lines of game logs. You can define how many lines of logs Backtrace should store by settings `Collect last n number of logs` property in the Unity editor.
- Backtrace will capture any native Unity Engine crash dumps on Windows OS.
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
