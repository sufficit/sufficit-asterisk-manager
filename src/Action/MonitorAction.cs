using System;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The MonitorAction starts monitoring (recording) a channel for call recording purposes.
    /// This action enables audio recording of active calls.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Monitor
    /// Purpose: Start call recording on a specific channel
    /// Privilege Required: call,all
    /// Implementation: res/res_monitor.c
    /// Available since: Asterisk 1.0
    /// 
    /// Required Parameters:
    /// - Channel: The channel to monitor (Required)
    /// 
    /// Optional Parameters:
    /// - File: Base filename for recordings (Optional)
    /// - Format: Audio format for recording files (Optional, default: wav)
    /// - Mix: Whether to mix both audio streams (Optional, default: false)
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Recording Behavior:
    /// - Creates separate files for input and output audio streams
    /// - Input stream: filename-in.format (audio from caller)
    /// - Output stream: filename-out.format (audio to caller)
    /// - Mix=true combines both streams into single file after call ends
    /// 
    /// File Naming:
    /// - If File not specified: Uses channel name with '/' replaced by '-'
    /// - Example: "SIP/1001-000001" becomes "SIP-1001-000001"
    /// - Files created in Asterisk monitor directory (typically /var/spool/asterisk/monitor)
    /// 
    /// Audio Formats:
    /// - "wav": WAV format (default, widely compatible)
    /// - "gsm": GSM format (compact, good for telephony)
    /// - "ulaw": ?-law format (telephony standard)
    /// - "alaw": A-law format (European telephony)
    /// - "sln": Signed linear format (uncompressed)
    /// - "g729": G.729 format (compressed, requires license)
    /// 
    /// Usage Scenarios:
    /// - Call center quality monitoring
    /// - Legal compliance recording
    /// - Training and evaluation
    /// - Dispute resolution
    /// - Customer service improvement
    /// - Regulatory compliance
    /// 
    /// Storage Considerations:
    /// - Monitor directory must have sufficient space
    /// - Consider automatic cleanup policies
    /// - WAV files are larger but more compatible
    /// - GSM files are smaller but require conversion for some uses
    /// 
    /// Legal Considerations:
    /// - Ensure compliance with local recording laws
    /// - May require caller notification
    /// - Consider data retention policies
    /// - Secure storage of sensitive recordings
    /// 
    /// Performance Impact:
    /// - CPU usage for audio processing and encoding
    /// - Disk I/O for writing audio files
    /// - Network impact if files stored remotely
    /// - Consider recording only when necessary
    /// 
    /// Integration Notes:
    /// - Works with most channel types (SIP, IAX2, DAHDI, etc.)
    /// - Can be started/stopped during active calls
    /// - Use StopMonitorAction to end recording
    /// - Use ChangeMonitorAction to modify recording parameters
    /// 
    /// Alternative Recording Methods:
    /// - MixMonitor application (newer, more flexible)
    /// - RECORD dialplan application
    /// - Channel-specific recording features
    /// 
    /// Example Usage:
    /// <code>
    /// // Basic monitoring with default settings
    /// var monitor = new MonitorAction("SIP/1001-00000001");
    /// 
    /// // Custom filename and format
    /// var monitor = new MonitorAction("SIP/1001-00000001", "call-recording-001", "wav");
    /// 
    /// // Mixed recording (single file)
    /// var monitor = new MonitorAction("SIP/1001-00000001", "mixed-call-001", "wav", true);
    /// </code>
    /// </remarks>
    /// <seealso cref="StopMonitorAction"/>
    /// <seealso cref="ChangeMonitorAction"/>
    public class MonitorAction : ManagerAction
    {
        /// <summary>
        /// Creates a new empty MonitorAction.
        /// </summary>
        /// <remarks>
        /// When using this constructor, you must set the Channel property
        /// before sending the action. Other properties are optional and will
        /// use Asterisk defaults if not specified.
        /// </remarks>
        public MonitorAction()
        {
        }

        /// <summary>
        /// Creates a new MonitorAction for the specified channel.
        /// </summary>
        /// <param name="channel">The name of the channel to monitor (Required)</param>
        /// <remarks>
        /// Channel Requirements:
        /// - Must be an active, connected channel
        /// - Channel format: "Technology/Resource-UniqueID"
        /// - Examples: "SIP/1001-00000001", "IAX2/provider-00000001"
        /// 
        /// Default Settings:
        /// - File: Auto-generated from channel name
        /// - Format: "wav"
        /// - Mix: false (separate input/output files)
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when channel is null</exception>
        /// <exception cref="ArgumentException">Thrown when channel is empty</exception>
        public MonitorAction(string channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrWhiteSpace(channel))
                throw new ArgumentException("Channel cannot be empty", nameof(channel));

            Channel = channel;
        }

        /// <summary>
        /// Creates a new MonitorAction for the specified channel with custom filename.
        /// </summary>
        /// <param name="channel">The name of the channel to monitor (Required)</param>
        /// <param name="file">The base filename for recordings (Required)</param>
        /// <remarks>
        /// File Naming:
        /// - Base filename without extension
        /// - Extension added automatically based on format
        /// - Input file: {file}-in.{format}
        /// - Output file: {file}-out.{format}
        /// - Mixed file: {file}.{format} (if Mix=true)
        /// 
        /// Path Considerations:
        /// - Relative paths: Relative to Asterisk monitor directory
        /// - Absolute paths: Must be writable by Asterisk process
        /// - Directory must exist and be writable
        /// - Consider file permissions and security
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when channel or file is null</exception>
        /// <exception cref="ArgumentException">Thrown when channel or file is empty</exception>
        public MonitorAction(string channel, string file) : this(channel)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentException("File cannot be empty", nameof(file));

            File = file;
        }

        /// <summary>
        /// Creates a new MonitorAction with channel, filename, and format.
        /// </summary>
        /// <param name="channel">The name of the channel to monitor (Required)</param>
        /// <param name="file">The base filename for recordings (Required)</param>
        /// <param name="format">The audio format for recording (Required)</param>
        /// <remarks>
        /// Supported formats depend on Asterisk configuration and loaded codecs.
        /// Common formats: wav, gsm, ulaw, alaw, sln, g729
        /// 
        /// Format Selection Guidelines:
        /// - wav: Best compatibility, larger files
        /// - gsm: Good compression, telephony quality
        /// - ulaw/alaw: Standard telephony formats
        /// - sln: Uncompressed, highest quality, largest files
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when any parameter is empty</exception>
        public MonitorAction(string channel, string file, string format) : this(channel, file)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            if (string.IsNullOrWhiteSpace(format))
                throw new ArgumentException("Format cannot be empty", nameof(format));

            Format = format;
        }

        /// <summary>
        /// Creates a new MonitorAction with all parameters specified.
        /// </summary>
        /// <param name="channel">The name of the channel to monitor (Required)</param>
        /// <param name="file">The base filename for recordings (Required)</param>
        /// <param name="format">The audio format for recording (Required)</param>
        /// <param name="mix">Whether to mix input and output streams into single file</param>
        /// <remarks>
        /// Mix Behavior:
        /// - false: Creates separate -in and -out files
        /// - true: Creates single mixed file after call completion
        /// 
        /// Mixed File Benefits:
        /// - Single file easier to manage
        /// - Both sides of conversation in chronological order
        /// - Easier playback and analysis
        /// 
        /// Separate Files Benefits:
        /// - Can analyze each party separately
        /// - Lower processing overhead during call
        /// - More flexibility in post-processing
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when any string parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when any string parameter is empty</exception>
        public MonitorAction(string channel, string file, string format, bool mix) : this(channel, file, format)
        {
            Mix = mix;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Monitor"</value>
        public override string Action => "Monitor";

        /// <summary>
        /// Gets or sets the name of the channel to monitor.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The channel name to start monitoring.
        /// </value>
        /// <remarks>
        /// Channel Format Examples:
        /// - "SIP/1001-00000001" (SIP peer channel)
        /// - "IAX2/provider/5551234567-00000001" (IAX2 trunk)
        /// - "DAHDI/1-1" (DAHDI channel)
        /// - "Local/1001@from-internal-00000001;1" (Local channel)
        /// - "PJSIP/1001-00000001" (PJSIP endpoint)
        /// 
        /// Channel Requirements:
        /// - Must be an active, connected channel
        /// - Channel must support monitoring capabilities
        /// - Must be in a state that allows recording
        /// - Cannot already be monitored (use ChangeMonitor to modify)
        /// 
        /// Channel States Suitable for Monitoring:
        /// - Up: Channel is connected and active
        /// - Ring: Can start monitoring before answer
        /// - Ringing: Outbound calls being established
        /// 
        /// Channel Technologies Supported:
        /// - SIP: Full support for monitoring
        /// - IAX2: Full support for monitoring
        /// - DAHDI: Hardware-dependent support
        /// - PJSIP: Full support for monitoring
        /// - Local: Monitors underlying channels
        /// </remarks>
        public string? Channel { get; set; }

        /// <summary>
        /// Gets or sets the base filename for the recording files.
        /// This property is optional.
        /// </summary>
        /// <value>
        /// Base filename without extension (default: derived from channel name).
        /// </value>
        /// <remarks>
        /// File Naming Rules:
        /// - If not specified: Auto-generated from channel name
        /// - Channel name transformation: '/' becomes '-'
        /// - Example: "SIP/1001-000001" ? "SIP-1001-000001"
        /// 
        /// File Path Handling:
        /// - Relative paths: Stored in Asterisk monitor directory
        /// - Absolute paths: Must be accessible to Asterisk process
        /// - Directory must exist with proper permissions
        /// - Consider disk space and cleanup policies
        /// 
        /// Filename Components:
        /// - Base: User-specified or auto-generated name
        /// - Direction: "-in" (caller audio) or "-out" (callee audio)
        /// - Extension: Based on format (.wav, .gsm, etc.)
        /// 
        /// Security Considerations:
        /// - Avoid user input in filenames without validation
        /// - Prevent directory traversal attacks
        /// - Ensure proper file permissions
        /// - Consider encryption for sensitive recordings
        /// 
        /// Example Filenames:
        /// - Base: "call-001"
        /// - Input: "call-001-in.wav"
        /// - Output: "call-001-out.wav"
        /// - Mixed: "call-001.wav" (if Mix=true)
        /// </remarks>
        public string? File { get; set; }

        /// <summary>
        /// Gets or sets the audio format for the recording files.
        /// This property is optional.
        /// </summary>
        /// <value>
        /// Audio format name (default: "wav").
        /// </value>
        /// <remarks>
        /// Supported Audio Formats:
        /// 
        /// Common Formats:
        /// - "wav": WAV format, widely compatible, larger files
        /// - "gsm": GSM format, compact, good quality for speech
        /// - "ulaw": ?-law PCM, standard in North America telephony
        /// - "alaw": A-law PCM, standard in European telephony
        /// - "sln": Signed linear, uncompressed, highest quality
        /// 
        /// Compressed Formats:
        /// - "g729": G.729 codec (requires license)
        /// - "g722": G.722 wideband codec
        /// - "g726": G.726 ADPCM codec
        /// - "ilbc": iLBC codec
        /// - "speex": Speex codec
        /// 
        /// Legacy Formats:
        /// - "au": Sun audio format
        /// - "raw": Raw audio data
        /// - "h263": H.263 video (if supported)
        /// 
        /// Format Selection Criteria:
        /// - Quality vs. Size: WAV > GSM > ?-law/A-law
        /// - Compatibility: WAV most compatible
        /// - Telephony Standard: ?-law (US), A-law (Europe)
        /// - Storage Efficiency: GSM good balance
        /// 
        /// Format Support:
        /// - Depends on loaded Asterisk modules
        /// - Check "core show file formats" in Asterisk CLI
        /// - Some formats require additional licenses
        /// - Hardware may influence available formats
        /// 
        /// Asterisk Configuration:
        /// - Format modules: format_wav, format_gsm, etc.
        /// - Codec modules: codec_ulaw, codec_alaw, etc.
        /// - License requirements for proprietary codecs
        /// </remarks>
        public string? Format { get; set; }

        /// <summary>
        /// Gets or sets whether to mix the input and output audio streams into a single file.
        /// This property is optional.
        /// </summary>
        /// <value>
        /// True to create a mixed file, false for separate files (default: false).
        /// </value>
        /// <remarks>
        /// Mixing Behavior:
        /// 
        /// Separate Files (Mix = false):
        /// - Creates two files: {filename}-in.{format} and {filename}-out.{format}
        /// - Input file: Audio from the monitored channel (what they say)
        /// - Output file: Audio to the monitored channel (what they hear)
        /// - Lower CPU usage during call
        /// - Immediate file availability
        /// - Allows independent analysis of each audio stream
        /// 
        /// Mixed File (Mix = true):
        /// - Creates single file: {filename}.{format}
        /// - Combines both audio streams chronologically
        /// - Processing occurs after call completion
        /// - Higher CPU usage for mixing process
        /// - Single file easier to manage and play
        /// - Natural conversation flow for human listeners
        /// 
        /// Use Cases for Separate Files:
        /// - Voice quality analysis per direction
        /// - Echo cancellation testing
        /// - Individual speaker identification
        /// - Automated speech recognition per speaker
        /// - Legal requirements for separate streams
        /// 
        /// Use Cases for Mixed Files:
        /// - Human review and quality monitoring
        /// - Customer service evaluation
        /// - Training purposes
        /// - General call playback
        /// - Simplified storage and management
        /// 
        /// Technical Considerations:
        /// - Mixed files created using "soxmix" command
        /// - Requires sox utility to be installed and accessible
        /// - Mixing occurs in background after call completion
        /// - Original separate files may be deleted after mixing
        /// - Failed mixing leaves separate files intact
        /// 
        /// Performance Impact:
        /// - Separate files: Minimal overhead during call
        /// - Mixed files: Additional processing after call
        /// - CPU usage depends on file format and size
        /// - Disk I/O during mixing process
        /// </remarks>
        public bool Mix { get; set; }
    }
}