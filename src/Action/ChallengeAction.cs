using System;
using Sufficit.Asterisk.Manager.Response;

namespace Sufficit.Asterisk.Manager.Action
{
    /// <summary>
    /// The ChallengeAction requests a challenge from the Asterisk server for secure authentication.
    /// This action is used for challenge/response authentication instead of plaintext passwords.
    /// </summary>
    /// <remarks>
    /// Asterisk Manager Action: Challenge
    /// Purpose: Request authentication challenge for secure login
    /// Privilege Required: None (pre-authentication)
    /// Available since: Asterisk 1.0
    /// 
    /// Required Parameters:
    /// - AuthType: Authentication algorithm (Required)
    /// 
    /// Optional Parameters:
    /// - ActionID: Unique identifier for correlation (Optional)
    /// 
    /// Supported Algorithms:
    /// - "MD5": MD5 hash authentication (most common)
    /// - Future versions may support additional algorithms
    /// 
    /// Authentication Flow:
    /// 1. Send ChallengeAction with desired AuthType
    /// 2. Receive ChallengeResponse with random challenge string
    /// 3. Combine challenge + password and hash with specified algorithm
    /// 4. Send LoginAction with hashed credentials
    /// 
    /// Security Benefits:
    /// - Password never transmitted in plaintext
    /// - Challenge prevents replay attacks
    /// - Random challenge ensures unique authentication
    /// - Protects against network eavesdropping
    /// 
    /// MD5 Authentication Process:
    /// 1. Request: ChallengeAction(AuthType="MD5")
    /// 2. Response: ChallengeResponse(Challenge="randomstring")
    /// 3. Compute: MD5(challenge + password)
    /// 4. Login: LoginAction(Key=hashedvalue, AuthType="MD5")
    /// 
    /// Usage Scenarios:
    /// - Secure authentication over untrusted networks
    /// - Applications requiring enhanced security
    /// - Compliance with security policies
    /// - Protection against credential theft
    /// - Multi-factor authentication systems
    /// 
    /// Network Security:
    /// - Safe for transmission over unencrypted connections
    /// - Prevents password interception
    /// - Challenge is single-use only
    /// - Time-sensitive authentication window
    /// 
    /// Asterisk Versions:
    /// - Available since Asterisk 1.0
    /// - MD5 consistently supported across versions
    /// - Enhanced security features in modern versions
    /// - Backward compatible implementation
    /// 
    /// Implementation Notes:
    /// This action is implemented in main/manager.c in Asterisk source code.
    /// Challenge strings are cryptographically random.
    /// Each challenge can only be used once.
    /// Challenges have a timeout period for security.
    /// 
    /// Error Conditions:
    /// - Unsupported authentication type
    /// - Challenge generation failure
    /// - Server cryptographic subsystem error
    /// - Maximum concurrent challenges exceeded
    /// 
    /// Best Practices:
    /// - Always use MD5 for current Asterisk versions
    /// - Implement proper timeout handling
    /// - Securely store and handle hash computation
    /// - Use unique ActionID for request correlation
    /// - Handle challenge expiration gracefully
    /// 
    /// Example Usage:
    /// <code>
    /// // Request MD5 challenge
    /// var challenge = new ChallengeAction("MD5");
    /// var response = await connection.SendActionAsync&lt;ChallengeResponse&gt;(challenge);
    /// 
    /// // Compute hash and login
    /// var hash = ComputeMD5(response.Challenge + password);
    /// var login = new LoginAction(username, "MD5", hash);
    /// </code>
    /// </remarks>
    /// <seealso cref="LoginAction"/>
    /// <seealso cref="ChallengeResponse"/>
    public class ChallengeAction : ManagerActionResponse
    {
        /// <summary>
        /// Creates a new ChallengeAction with MD5 authentication type.
        /// </summary>
        /// <remarks>
        /// MD5 is the default and most widely supported authentication type.
        /// This constructor provides a convenient way to request MD5 challenges
        /// without specifying the algorithm explicitly.
        /// </remarks>
        public ChallengeAction()
        {
            AuthType = "MD5";
        }

        /// <summary>
        /// Creates a new ChallengeAction for the specified authentication type.
        /// </summary>
        /// <param name="authType">The digest algorithm to use (Required)</param>
        /// <remarks>
        /// Supported Authentication Types:
        /// - "MD5": MD5 hash algorithm (recommended)
        /// - Future algorithms may be added in newer Asterisk versions
        /// 
        /// The authentication type determines:
        /// - Hash algorithm used for credential computation
        /// - Challenge format and length
        /// - Security properties of the authentication
        /// 
        /// Currently, Asterisk primarily supports MD5 authentication.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when authType is null</exception>
        /// <exception cref="ArgumentException">Thrown when authType is empty</exception>
        public ChallengeAction(string authType)
        {
            if (authType == null)
                throw new ArgumentNullException(nameof(authType));
            if (string.IsNullOrWhiteSpace(authType))
                throw new ArgumentException("AuthType cannot be empty", nameof(authType));

            AuthType = authType;
        }

        /// <summary>
        /// Creates a new ChallengeAction with authentication type and action ID.
        /// </summary>
        /// <param name="authType">The digest algorithm to use (Required)</param>
        /// <param name="actionId">Unique identifier for this action (Optional)</param>
        /// <remarks>
        /// The action ID allows correlation of the ChallengeResponse with this
        /// specific challenge request. This is particularly useful when multiple
        /// authentication attempts might be in progress simultaneously.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when authType is null</exception>
        /// <exception cref="ArgumentException">Thrown when authType is empty</exception>
        public ChallengeAction(string authType, string actionId) : this(authType)
        {
            ActionId = actionId;
        }

        /// <summary>
        /// Gets the name of this action.
        /// </summary>
        /// <value>Always returns "Challenge"</value>
        public override string Action => "Challenge";

        /// <summary>
        /// Gets or sets the digest algorithm to use for authentication.
        /// This property is required.
        /// </summary>
        /// <value>
        /// The authentication algorithm name.
        /// </value>
        /// <remarks>
        /// Supported Authentication Types:
        /// 
        /// MD5 Algorithm:
        /// - Most widely supported across all Asterisk versions
        /// - Produces 128-bit (32 hex character) hash values
        /// - Compatible with standard MD5 implementations
        /// - Recommended for general use
        /// 
        /// Algorithm Selection Guidelines:
        /// - Use "MD5" for maximum compatibility
        /// - Check Asterisk version for additional algorithm support
        /// - Consider security requirements vs. compatibility
        /// - Test authentication flow with chosen algorithm
        /// 
        /// Security Considerations:
        /// - MD5 has known cryptographic weaknesses
        /// - Suitable for basic authentication protection
        /// - Consider network security for sensitive environments
        /// - Combine with TLS for enhanced security
        /// 
        /// Implementation Notes:
        /// - Algorithm names are case-sensitive
        /// - Must match exactly what Asterisk expects
        /// - Server validates algorithm support before generating challenge
        /// - Invalid algorithms result in error responses
        /// 
        /// Future Compatibility:
        /// - Newer Asterisk versions may add SHA-256, SHA-512
        /// - Implementation should handle unknown algorithm errors
        /// - Consider fallback to MD5 for compatibility
        /// </remarks>
        public string AuthType { get; set; }

        /// <summary>
        /// Returns the response type for this action.
        /// </summary>
        /// <returns>The Type of ChallengeResponse</returns>
        public override Type ActionCompleteResponseClass()
            => typeof(ChallengeResponse);
    }
}