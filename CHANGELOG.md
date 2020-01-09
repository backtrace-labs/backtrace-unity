# Backtrace Unity Release Notes

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
