# Backtrace Unity support

[Backtrace](http://backtrace.io/)'s integration with Unity allows developers to capture and report handled and unhandled Unity exceptions and crashes to their Backtrace instance, instantly offering the ability to prioritize and debug software errors.

[github release]: (https://github.com/backtrace-labs/backtrace-labs/)

## Usage

```csharp

 //Read from manager BacktraceClient instance
var backtraceClient = GameObject.Find("_Manager").GetComponent<BacktraceClient>();
try{
    //throw exception here
}
catch(Exception exception){
    var report = new BacktraceReport(exception);
    backtraceClient.Send(report);
}
```

# Features Summary <a name="features-summary"></a>

- Light-weight Unity client that quickly submits crashed generated in Unity environment to your Backtrace dashboard
  - Can include callstack, system metadata, custom metadata, custom attributes and file attachments if needed
- Supports a wide range of unity version and environments
- Supports .NET 2.0/3.5/4.5/Standard 2.0 Backend, IL2CPP and Mono environments
- Supports offline database for error report storage and re-submission in case of network outage
- Fully customizable and extendable event handlers
- Custom IDE integrations

# Prerequisites

- Unity environment 2017.4.x
- .NET 2.0/3.5/4.5/Standard 2.0 scripting runtime version
- Mono or IL2CPP scripting backend

# Setup <a name="installation"></a>

List of steps necessary to setup full Backtrace Unity integration.

## Installation guide

- Download the backtrace-unity zip file. Unzip it and keep the folder in a known location. It can be downloaded from https://github.com/backtrace-labs/backtrace-unity/releases
- Open your Unity project
- Use the Unity Package Manager to install the backtrace-unity library (Window -> Package Manager -> Add Package From Disk)

## Integrating into your project

- Under the Assets Menu "Create" option, there is now a Backtrace -> Configuration option. Choose that option (or Right click on empty space and select from the menu box) to have a Backtrace Configuration is generated in the Assets folder. You can drag and drop generated asset file into Backtrace Client configuration window.
  ![Backtrace menu dialog box](./Documentation~/images/dialog-box.PNG)
- Next, select an object from the Scene Hierarchy to associate the Backtrace reporting client to. In the example below, we use the Manager object., Using the Inspector panel, click the Add Component button and search for the Backtrace Client object.
- Within the Backtrace Client panel, there is a Backtrace Configuration field. Drag and drop the Backtrace Configuration from the Assets folder to that field. More fields will appear for you to fill in to configure the Backtrace Client and Offline Database options.
  ![Backtrace configuration window](./Documentation~/images/unity-basic-configuration.PNG)
- Provide valid Backtrace client configuration and start using library!
  ![Full Backtrace configuration](./Documentation~/images/client-setup.PNG)

## Plugin best practices

Plugin allows you to define maximum depth of game objects. By default its disabled (Game object depth is equal to -1). If you will use 0 as maximum depth of game object we will use default game object limit - 16. If you would like to specify game object depth size to n, please insert n in Backtrace configuration text box. If you require game obejct depth to be above 30, please contact support.

## Backtrace Client and Offline Database Settings

The following is a reference guide to the Backtrace Client fields:

- Server Address: This field is required to submit exceptions from your Unity project to your Backtrace instance. More information about how to retrieve this value for your instance is our docs at What is a submission URL and What is a submission token? NOTE: the backtrace-unity plugin will expect full URL with token to your Backtrace instance,
- Reports per minute: Limits the number of reports the client will send per minutes. If set to 0, there is no limit. If set to a higher value and the value is reached, the client will not send any reports until the next minute. Further, the BacktraceClient.Send/BacktraceClient.SendAsync method will return false.
- Destroy client on new scene load - Backtrace-client by default will be available on each scene. Once you initialize Backtrace integration, you can fetch Backtrace game object from every scene. In case if you don't want to have Backtrace-unity integration available by default in each scene, please set this value to true.
- Use normalized exception message: If exception does not have a stack trace, use a normalized exception message to generate fingerprint.
- Filter reports: Configure Backtrace plugin to filter reports based on report type - Message, Exception, Unhandled Exception, Hang. By default this option is disabled (None).
- Send unhandled native game crashes on startup: Try to find game native crashes and send them on Game startup.
- Handle unhandled exceptions: Toggle this on or off to set the library to handle unhandled exceptions that are not captured by try-catch blocks.
- Game Object Depth Limit: Allows developer to filter number of game object childrens in Backtrace report.
- Collect last n game logs: Collect last n number of logs generated by game. 
- Ignore SSL validation: Unity by default will validate ssl certificates. By using this option you can avoid ssl certificates validation. However, if you don't need to ignore ssl validation, please set this option to false.
- Handle ANR (Application not responding) - this options is available only in Android build. It allows to catch ANR (application not responding) events happened to your game in Android devices. In this release, ANR is set to detect after 5 seconds. This will be configurable in a future release.
- Enable Database: When this setting is toggled, the backtrace-unity plugin will configure an offline database that will store reports if they can't be submitted do to being offline or not finding a network. When toggled on, there are a number of Database settings to configure.
- Backtrace Database path: This is the path to directory where the Backtrace database will store reports on your game. You can use interpolated strings SUCH AS 
`${Application.persistentDataPath}/backtrace/database` to dynamically look up a known directory structure to use. NOTE: Backtrace database will remove all existing files in the database directory upion first initialization.  
- Create database directory toggle: If toggled, the library will create the offline database directory if the provided path doesn't exists,
- Client-side deduplication: Backtrace-unity plugin allows you to combine the same reports. By using deduplication rules, you can tell backtrace-unity plugin how we should merge reports.
- Minidump type: Type of minidump that will be attached to Backtrace report in the report generated on Windows machine.
- Attach Unity Player.log: Add Unity player log file to Backtrace report. NOTE: This feature is available only on desktop - Windows/MacOS/Linux.
- Attach screenshot: Generate and attach screenshot of frame as exception occurs.
- Auto Send Mode: When toggled on, the database will send automatically reports to Backtrace server based on the Retry Settings below. When toggled off, the developer will need to use the Flush method to attempt to send and clear. Recommend that this is toggled on.
- Maximum number of records: This is one of two limits you can impose for controlling the growth of the offline store. This setting is the maximum number of stored reports in database. If value is equal to zero, then limit not exists, When the limit is reached, the database will remove the oldest entries.
- Maximum database size: This is the second limit you can impose for controlling the growth of the offline store. This setting is the maximum database size in MB. If value is equal to zero, then size is unlimited, When the limit is reached, the database will remove the oldest entries.
- Retry interval: If the database is unable to send its record, this setting specifies how many seconds the library should wait between retries.
- Maximum retries: If the database is unable to send its record, this setting specifies the maximum number of retries before the system gives up.
- Retry order: This specifies in which order records are sent to the Backtrace server.

# API Overview

You can further configure your game to submit crashes by making further changes in the C# code for your game.

## Basic configuration

If you setup `Backtrace client` and `Backtrace database` configuration you can retrieve database and client instances by using `GameObject`. When you retrieve client instance you can start sending reports from try/catch block in your game!

```csharp

 //Read from manager BacktraceClient instance
var backtraceClient = GameObject.Find("manager name").GetComponent<BacktraceClient>();

 //Read from manager BacktraceClient instance
var database = GameObject.Find("manager name").GetComponent<BacktraceDatabase>();


try{
    //throw exception here
}
catch(Exception exception){
    var report = new BacktraceReport(exception);
    backtraceClient.Send(report);
}
```

If you would like to change Backtrace client/database options, we recommend to change these values on the Unity UI via Backtrace Configuration file. However, if you would like to change these values dynamically, please use method `Refresh` to apply new configuration changes.

For example:

```csharp
 //Read from manager BacktraceClient instance
var backtraceClient = GameObject.Find("manager name").GetComponent<BacktraceClient>();

//Change configuration value
backtraceClient.Configuration.DeduplicationStrategy = deduplicationStrategy;
//Refresh configuraiton
backtraceClient.Refresh();

```

## Sending an error report <a name="documentation-sending-report"></a>

`BacktraceClient.Send` method will send an error report to the Backtrace endpoint specified. There `Send` method is overloaded, see examples below:

### Using BacktraceReport

The `BacktraceReport` class represents a single error report. (Optional) You can also submit custom attributes using the `attributes` parameter, or attach files by supplying an array of file paths in the `attachmentPaths` parameter.

```csharp
try
{
  //throw exception here
}
catch (Exception exception)
{
    var report = new BacktraceReport(
        exception: exception,
        attributes: new Dictionary<string, string>() { { "key", "value" } },
        attachmentPaths: new List<string>() { @"file_path_1", @"file_path_2" }
    );
    backtraceClient.Send(report);
}
```

Notes:

- if you setup `BacktraceClient` with `BacktraceDatabase` and your application is offline or you pass invalid credentials to `Backtrace server`, reports will be stored in database directory path,
- `BacktraceReport` allows you to change default Fingerprint generation algorithm. You can use `Fingerprint` property if you want to change Fingerprint value. Keep in mind - Fingerprint should be valid sha256 string. By setting `Fingerprint` you are instructing the client reporting library to only write a single report for the exception as it is encountered, and maintain a counter for every additional time it is encountered, instead of creating a new report. This will allow better control over the volume of reports being generated and sent to Backtrace. The counter is reset when the offline database is cleared (usually when the reports are sent to the server). A new single report will be created the next time the error is encountered.
- `BacktraceReport` allows you to change grouping strategy in Backtrace server. If you want to change how algorithm group your reports in Backtrace server please override `Factor` property.

If you want to use `Fingerprint` and `Factor` property you have to override default property values. See example below to check how to use these properties:

```csharp
try
{
  //throw exception here
}
catch (Exception exception)
{
    var report = new BacktraceReport(...){
        Fingerprint = "sha256 string",
        Factor = exception.GetType().Name
    };
    ....
}

```

## Attaching custom event handlers <a name="documentation-events"></a>

`BacktraceClient` allows you to attach your custom event handlers. For example, you can trigger actions before the `Send` method:

```csharp

 //Add your own handler to client API

backtraceClient.BeforeSend =
    (Model.BacktraceData model) =>
    {
        var data = model;
        //do something with data for example:
        data.Attributes.Add("eventAttribute", "EventAttributeValue");
        if(data.Classifier == null || !data.Classifier.Any())
        {
            data.Attachments.Add("path to attachment");
        }

        return data;
    };
```

`BacktraceClient` currently supports the following events:

- `BeforeSend`
- `OnClientReportLimitReached`
- `OnServerResponse`
- `OnServerError`

## Reporting unhandled application exceptions

`BacktraceClient` supports reporting of unhandled application exceptions not captured by your try-catch blocks. To enable reporting of unhandled exceptions even if you don't set this option in `Backtrace configuration window` please use code below:

```csharp
backtraceClient.HandleApplicationException();
```

## Filtering a report 
`BacktraceClient` allows you to filter report by using `Filter reports` option available in the Backtrace configuration UI. In case if you need to have better control over creating new reports, you can use `FilterReport` delegate. delegate require to pass a function that will accept:
* `ReportFilterType` enum - type of a report that BacktraceClient will creat. Available types: Message, Exception, UnhandledException and Hang,
* Exception - exception handled by BacktraceClient. 
* Message - report message.

FilterReport delegate should report boolean value. If you would like to skip report - please return true, otherwise if you will return false, `BacktraceClient` will continue processing data.

In case if you would like to filter only specific type of exception, please use `Filter report` option in the UI and select what type of reports, `BacktraceClient` should filter.

```csharp
 BacktraceClient.FilterReport = (ReportFilterType type, Exception exception, string message) =>
            {
                // to recognize filter type us ReportFilterType flag
                // available options None,  Message, Exception, UnhandledException, Hang
                // in case if you would like to skip all message reports you can check 
                // if type is ReportFilterType.Message

                // to learn more about exception object or report message please check exception/message properties

                // return true if you would like to filter report
                // otherwise return false and let Backtrace handle a report
                return true;
            };
```


## Flush database

When your application starts, database can send stored offline reports. If you want to do make it manually you can use `Flush` method that allows you to send report to server and then remove it from hard drive. If `Send` method fails, database will no longer store data.

```csharp
backtraceDatabase.Flush();
```

## Send database
This method will try to send all objects from the database respecting the client side deduplication and retry setting. This can be used as an alternative to the `Flush` method which will try to send all objects from the database ignoring any client side deduplication and retry settings.

```csharp
backtraceDatabase.Send();
```

## Clearing database

You can clear all data from database without sending it to server by using `Clear` method. `BacktraceDatabase` will remove all files and won't send it to server.

```csharp
backtraceDatabase.Clear();
```

#### Client side Deduplication

Backtrace unity integration allows you to aggregate the same reports and send only one message to Backtrace Api. As a developer you can choose deduplication options. Please use `DeduplicationStrategy` enum to setup possible deduplication rules in Unity UI:
![Backtrace deduplicaiton setup](./Documentation~/images/deduplication-setup.PNG)

Deduplication strategy types:

- Ignore - ignore deduplication strategy,
- Default - deduplication strategy will only use current strack trace to find duplicated reports,
- Classifier - deduplication strategy will use stack trace and exception type to find duplicated reports,
- Message - deduplication strategy will use stack trace and exception message to find duplicated reports,

Notes:

- When you aggregate reports via Backtrace C# library, `BacktraceDatabase` will increase number of reports in BacktraceDatabaseRecord counter property.
- Deduplication algorithm will include `BacktraceReport` `Fingerprint` and `Factor` properties. `Fingerprint` property will overwrite deduplication algorithm result. `Factor` property will change hash generated by deduplication algorithm.
- If Backtrace unity integration combine multiple reports and user will close a game before plugin will send data to Backtrace, you will lose coutner information.
- `BacktraceDatabase` methods allows you to use aggregated diagnostic data together. You can check `Hash` property of `BacktraceDatabaseRecord` to check generated hash for diagnostic data and `Counter` to check how much the same records we detect.
- `BacktraceDatabase` `Count` method will return number of all records stored in database (included deduplicated records),
- `BacktarceDatabase` `Delete` method will remove record (with multiple deduplicated records) at the same time.

# Android Specific information

The backtrace-unity library includes support for capturing additional Android Native information, from underlying Android OS (Memory and process related), JNI, and NDK layers.

## Native process and memory related information

system.memory usage related information including memfree, swapfree, and vmalloc.used is now available. Additional VM details and voluntary / nonvountary ctxt switches are included.

## ANR

When configuring the nacktrace-unity client for an Android deployment, programmers will have a toggle available in backtrace-unity GUI in the Unity Editor to enable or disable ANR reports. This will use the default of 5 seconds.

# Architecture description

## BacktraceReport <a name="architecture-BacktraceReport"></a>

**`BacktraceReport`** is a class that describe a single error report.

## BacktraceClient <a name="architecture-BacktraceClient"></a>

**`BacktraceClient`** is a class that allows you to send `BacktraceReport` to `Backtrace` server by using `BacktraceApi`. This class sets up connection to the Backtrace endpoint and manages error reporting behavior (for example, saving minidump files on your local hard drive and limiting the number of error reports per minute). `BacktraceClient` inherits from `Mono behavior`.

`BacktraceClient` requires from a `Backtrace configuration window`

- `Sever URL` - URL to `Backtrace` server,
- `Token` - token to `Backtrace` project,
- `ReportPerMin` - A cap on the number of reports that can be sent per minute. If `ReportPerMin` is equal to zero then there is no cap.
- `HandleUnhandledExceptions` - flag that allows `BacktraceClient` handling unhandled exception by default.

## BacktraceData <a name="architecture-BacktraceData"></a>

**`BacktraceData`** is a serializable class that holds the data to create a diagnostic JSON to be sent to the Backtrace endpoint via `BacktraceApi`. You can add additional pre-processors for `BacktraceData` by attaching an event handler to the `BacktraceClient.BeforeSend` event. `BacktraceData` require `BacktraceReport` and `BacktraceClient` client attributes.

## BacktraceApi <a name="architecture-BacktraceApi"></a>

**`BacktraceApi`** is a class that sends diagnostic JSON to the Backtrace endpoint. `BacktraceApi` is instantiated when the `BacktraceClient` awake method is called. `BacktraceApi` can asynchronous reports to the Backtrace endpoint.

## BacktraceDatabase <a name="architecture-BacktraceDatabase"></a>

**`BacktraceDatabase`** is a class that stores error report data in your local hard drive. `BacktraceDatabase` stores error reports that were not sent successfully due to network outage or server unavailability. `BacktraceDatabase` periodically tries to resend reports
cached in the database. In `BacktraceDatabaseSettings` you can set the maximum number of entries (`Maximum retries`) to be stored in the database. The database will retry sending
stored reports every `Retry interval` seconds up to `Retry limit` times, both customizable in the `Backtrace database configuration`.

`Backtrace database` has the following properties:

- `Database path` - the local directory path where `BacktraceDatabase` stores error report data when reports fail to send,
- `MaxRecordCount` - Maximum number of stored reports in Database. If value is equal to `0`, then there is no limit.
- `MaxDatabaseSize` - Maximum database size in MB. If value is equal to `0`, there is no limit.
- `AutoSendMode` - if the value is `true`, `BacktraceDatabase` will automatically try to resend stored reports. Default is `false`.
- `RetryBehavior` - - `RetryBehavior.ByInterval` - Default. `BacktraceDatabase` will try to resend the reports every time interval specified by `RetryInterval`. - `RetryBehavior.NoRetry` - Will not attempt to resend reports
- `RetryInterval` - the time interval between retries, in seconds.
- `RetryLimit` - the maximum number of times `BacktraceDatabase` will attempt to resend error report before removing it from the database.

If you want to clear your database or remove all reports after send method you can use `Clear` and `Flush`.

## ReportWatcher <a name="architecture-ReportWatcher"></a>

**`ReportWatcher`** is a class that validate send requests to the Backtrace endpoint. If `reportPerMin` is set in the `BacktraceClient` constructor call, `ReportWatcher` will drop error reports that go over the limit. `BacktraceClient` check rate limit before `BacktraceApi` generate diagnostic json.

# Investigating an Error in Backtrace

Once errors are being reported to your Backtrace instance, you should see them in your Triage and Web Debugger view. See below for a screenshot of the Triage view with some Unity exceptions reported.
![Backtrace search](https://downloads.intercomcdn.com/i/o/85088367/8579259bd9e72a9c5f429f27/Screen+Shot+2018-11-10+at+11.59.33+AM.png)

The developer who is debugging the error may find it useful to view more details of Exception. They choose the 'View Latest Trace' action to see more details in the Backtrace Web Debugger. Below we can see a list of all attributes submitted with a report. (Note the yield signs are just an indicator that this value is not indexed in Backtrace). We can also see the call stack and details of the selected frame.
![Backtrace web debugger](https://downloads.intercomcdn.com/i/o/85088529/3785366e044e4e69c4b23abd/Screen+Shot+2018-11-10+at+12.22.41+PM.png)

Below we see more details above the Environment Variables from the Web Debugger to further assist with investigation.
![Backtrace attributes](https://downloads.intercomcdn.com/i/o/85088535/c84ebd2b96f1d5423b36482d/Screen+Shot+2018-11-10+at+12.22.56+PM.png)
