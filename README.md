# Backtrace Integration with Unity

[![openupm](https://img.shields.io/npm/v/io.backtrace.unity?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.backtrace.unity/)

[Backtrace](http://backtrace.io/)'s integration with Unity allows you to capture and report log errors, handled and unhandled Unity exceptions, and native crashes so you can prioritize and debug software errors.

## Installation
```
# Install openupm-cli
npm install -g openupm-cli

# Go to your Unity project directory
cd YOUR_UNITY_PROJECT_DIR

# Install the latest io.backtrace.unity package
openupm add io.backtrace.unity
```

## Usage

```csharp

 //Read from manager BacktraceClient instance
var backtraceClient = GameObject.Find("_Manager").GetComponent<BacktraceClient>();
try
{
    //throw exception here
}
catch(Exception exception)
{
    var report = new BacktraceReport(exception);
    backtraceClient.Send(report);
}
```

## Documentation
For more information about the Unity integration, including installation, usage, and configuration options, see the [Unity Integration guide](https://docs.saucelabs.com/error-reporting/platform-integrations/unity/setup/) in the Sauce Labs documentation.