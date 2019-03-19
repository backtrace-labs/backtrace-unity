# Backtrace Unity Release Notes

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
