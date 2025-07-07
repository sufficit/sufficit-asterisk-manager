<h1>
  Asterisk Call Monitoring Service Usage
  <a href="https://github.com/sufficit"><img src="https://avatars.githubusercontent.com/u/66928451?s=200&v=4" alt="Sufficit Logo" width="80" align="right"></a>
</h1>

## ?? About Call Monitoring Service

The `AsteriskCallMonitorService` is a background service that provides real-time monitoring of telephony call events through the **Asterisk Manager Interface (AMI)**. It captures, processes, and distributes information about the complete call lifecycle, enabling advanced telephony applications and integrations.

### ? Key Features

* **Real-time call monitoring** with live event processing
* **Intelligent caching** for call state management
* **Notification system** integration for event distribution
* **Queue system** integration for asynchronous processing
* **Call recording support** via MIXMONITOR integration
* **Complete call lifecycle tracking** from channel creation to hangup

### ?? Monitored Events

* **NewChannelEvent** - New call channel creation
* **HangupEvent** - Call termination and cleanup
* **NewAccountCodeEvent** - Account code updates for billing
* **CdrEvent** - Call Detail Records (optional)

## ?? Getting Started

### ?? Prerequisites

* .NET 8.0 or higher project
* Configured AMI service
* Notification system setup
* Exchange/queue system configured

### ??? Implementation Examples

**1. Service registration in DI container:**
using Sufficit.AMIEvents.Services.AsteriskCallMonitor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// In Program.cs or Startup.cs
services.AddSingleton<AsteriskCallMonitorService>();
services.AddHostedService<AsteriskCallMonitorService>(provider => 
    provider.GetRequiredService<AsteriskCallMonitorService>());
**2. Basic implementation with custom event handling:**
using Sufficit.AMIEvents.Services.AsteriskCallMonitor;
using Sufficit.Telephony.Call;
using Microsoft.Extensions.Logging;

public class CallProcessingService
{
    private readonly AsteriskCallMonitorService _monitor;
    private readonly ILogger<CallProcessingService> _logger;

    public CallProcessingService(
        AsteriskCallMonitorService monitor,
        ILogger<CallProcessingService> logger)
    {
        _monitor = monitor;
        _logger = logger;

        // Subscribe to call change events
        _monitor.OnChange += HandleCallStateChange;
    }

    private void HandleCallStateChange(object? sender, TelephonyCall call)
    {
        _logger.LogInformation("Call state changed: {CallId} - {State}", 
            call.Key, call.State);

        switch (call.State)
        {
            case TelephonyCallState.DIALING:
                ProcessDialingCall(call);
                break;
            
            case TelephonyCallState.DOWN:
                ProcessCompletedCall(call);
                break;
        }
    }

    private void ProcessDialingCall(TelephonyCall call)
    {
        _logger.LogInformation("New call initiated: {Source} -> {Destination}", 
            call.Source, call.Destination);
        
        // Business logic for active calls
        // Example: Update real-time dashboard
        UpdateLiveDashboard(call);
    }

    private void ProcessCompletedCall(TelephonyCall call)
    {
        _logger.LogInformation("Call completed: Duration {Duration}s, Disposition: {Disposition}", 
            call.Duration, call.Disposition);
        
        // Business logic for completed calls
        // Example: Generate reports, save to database
        if (call.Hangup != null)
        {
            _logger.LogDebug("Hangup details: Cause {Cause} - {Text}", 
                call.Hangup.Cause, call.Hangup.Text);
            
            AnalyzeCallQuality(call);
        }
    }

    private void UpdateLiveDashboard(TelephonyCall call)
    {
        // Implementation for real-time dashboard updates
        // Could use SignalR for browser updates
    }

    private void AnalyzeCallQuality(TelephonyCall call)
    {
        // Implementation for call quality analysis
        // Check for abnormal hangup causes, short duration calls, etc.
    }
}
**3. Advanced monitoring with filtering and analytics:**
public class AdvancedCallAnalytics
{
    private readonly AsteriskCallMonitorService _monitor;
    private readonly Dictionary<string, CallMetrics> _callMetrics = new();

    public AdvancedCallAnalytics(AsteriskCallMonitorService monitor)
    {
        _monitor = monitor;
        _monitor.OnChange += AnalyzeCallData;
    }

    private void AnalyzeCallData(object? sender, TelephonyCall call)
    {
        // Filter incoming calls only
        if (IsIncomingCall(call))
        {
            ProcessIncomingCall(call);
        }

        // Monitor long duration calls
        if (call.Duration > 300) // 5 minutes threshold
        {
            HandleLongDurationCall(call);
        }

        // Detect problematic calls
        if (call.State == TelephonyCallState.DOWN && 
            call.Hangup?.Cause != 16) // 16 = Normal clearing
        {
            HandleProblematicCall(call);
        }

        // Track call recording availability
        if (!string.IsNullOrEmpty(call.Recording))
        {
            ProcessCallRecording(call);
        }
    }

    private bool IsIncomingCall(TelephonyCall call)
    {
        // Implementation to determine call direction
        return call.Direction == CallDirection.INCOMING;
    }

    private void ProcessIncomingCall(TelephonyCall call)
    {
        Console.WriteLine($"Incoming call from {call.Source} to {call.Destination}");
        
        // Example: CRM integration for caller identification
        var callerInfo = LookupCallerInformation(call.Source);
        if (callerInfo != null)
        {
            Console.WriteLine($"Known caller: {callerInfo.Name} - {callerInfo.Company}");
        }
    }

    private void HandleLongDurationCall(TelephonyCall call)
    {
        Console.WriteLine($"Extended call detected: {call.Key} - Duration: {call.Duration}s");
        
        // Could trigger supervisor notifications for quality monitoring
        TriggerSupervisorAlert(call, "Long duration call");
    }

    private void HandleProblematicCall(TelephonyCall call)
    {
        Console.WriteLine($"Call ended abnormally: Cause {call.Hangup?.Cause} - {call.Hangup?.Text}");
        
        // Log for quality analysis and potential system issues
        LogCallIssue(call);
    }

    private void ProcessCallRecording(TelephonyCall call)
    {
        Console.WriteLine($"Recording available: {call.Recording}");
        
        // Could trigger automatic transcription or quality scoring
        ScheduleRecordingProcessing(call.Recording);
    }

    private CallerInfo? LookupCallerInformation(string phoneNumber)
    {
        // Implementation for CRM lookup
        return null; // Placeholder
    }

    private void TriggerSupervisorAlert(TelephonyCall call, string reason)
    {
        // Implementation for supervisor notifications
    }

    private void LogCallIssue(TelephonyCall call)
    {
        // Implementation for issue logging and tracking
    }

    private void ScheduleRecordingProcessing(string recordingFile)
    {
        // Implementation for recording post-processing
    }
}

public class CallerInfo
{
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
}

public class CallMetrics
{
    public int TotalCalls { get; set; }
    public double AverageDuration { get; set; }
    public int AnsweredCalls { get; set; }
}
**4. Integration with reporting and business intelligence:**
public class CallReportingService
{
    private readonly AsteriskCallMonitorService _monitor;
    private readonly List<TelephonyCall> _completedCalls = new();
    private readonly Timer _reportTimer;

    public CallReportingService(AsteriskCallMonitorService monitor)
    {
        _monitor = monitor;
        _monitor.OnChange += CollectCallData;
        
        // Generate reports every hour
        _reportTimer = new Timer(GenerateHourlyReport, null, 
            TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }

    private void CollectCallData(object? sender, TelephonyCall call)
    {
        // Collect only completed calls for reporting
        if (call.State == TelephonyCallState.DOWN)
        {
            lock (_completedCalls)
            {
                _completedCalls.Add(call);
            }
            
            // Immediate processing for critical metrics
            ProcessImmediateMetrics(call);
        }
    }

    private void ProcessImmediateMetrics(TelephonyCall call)
    {
        // Calculate real-time KPIs
        var averageCallDuration = CalculateAverageCallDuration();
        var answerRate = CalculateAnswerRate();
        
        Console.WriteLine($"Current metrics - Avg Duration: {averageCallDuration:F1}s, Answer Rate: {answerRate:P}");
    }

    private void GenerateHourlyReport(object? state)
    {
        List<TelephonyCall> callsToProcess;
        
        lock (_completedCalls)
        {
            callsToProcess = new List<TelephonyCall>(_completedCalls);
            _completedCalls.Clear();
        }

        if (callsToProcess.Count == 0) return;

        var report = new CallSummaryReport
        {
            Period = DateTime.Now.AddHours(-1),
            TotalCalls = callsToProcess.Count,
            AverageDuration = callsToProcess.Average(c => c.Duration),
            AnsweredCalls = callsToProcess.Count(c => 
                c.Disposition == TelephonyCallDisposition.ANSWERED),
            IncomingCalls = callsToProcess.Count(c => 
                c.Direction == CallDirection.INCOMING),
            OutgoingCalls = callsToProcess.Count(c => 
                c.Direction == CallDirection.OUTGOING)
        };

        ProcessReport(report);
    }

    private double CalculateAverageCallDuration()
    {
        lock (_completedCalls)
        {
            return _completedCalls.Count > 0 ? _completedCalls.Average(c => c.Duration) : 0;
        }
    }

    private double CalculateAnswerRate()
    {
        lock (_completedCalls)
        {
            if (_completedCalls.Count == 0) return 0;
            
            var answered = _completedCalls.Count(c => 
                c.Disposition == TelephonyCallDisposition.ANSWERED);
            return (double)answered / _completedCalls.Count;
        }
    }

    private void ProcessReport(CallSummaryReport report)
    {
        Console.WriteLine($"Hourly Report - {report.Period:yyyy-MM-dd HH:mm}:");
        Console.WriteLine($"  Total Calls: {report.TotalCalls}");
        Console.WriteLine($"  Average Duration: {report.AverageDuration:F1}s");
        Console.WriteLine($"  Answer Rate: {(double)report.AnsweredCalls / report.TotalCalls:P}");
        Console.WriteLine($"  Incoming: {report.IncomingCalls}, Outgoing: {report.OutgoingCalls}");
        
        // Could save to database, send to BI system, etc.
    }
}

public class CallSummaryReport
{
    public DateTime Period { get; set; }
    public int TotalCalls { get; set; }
    public double AverageDuration { get; set; }
    public int AnsweredCalls { get; set; }
    public int IncomingCalls { get; set; }
    public int OutgoingCalls { get; set; }
}
**5. Blazor WebAssembly integration for real-time dashboard:**
// CallMonitorHub.cs - SignalR Hub
using Microsoft.AspNetCore.SignalR;

public class CallMonitor
