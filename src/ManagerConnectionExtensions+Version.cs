using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Action;
using Sufficit.Asterisk.Manager.Connection;
using Sufficit.Asterisk.Manager.Response;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.Manager
{
    public static partial class ManagerConnectionExtensions
    {

        public static async Task<AsteriskVersion> GetAsteriskVersion(this ManagerConnection source, CancellationToken cancellationToken)
        {
            var asteriskVersion = await source.GetVersionByCoreShowVersion(cancellationToken);
            if (asteriskVersion == AsteriskVersion.Unknown)
                asteriskVersion = await source.GetVersionByShowVersionFiles(cancellationToken);

            if (asteriskVersion == AsteriskVersion.Unknown)
                _logger.LogWarning("could not determine Asterisk version.");

            _logger.LogDebug("final determined Asterisk Version: {VersionEnum}", asteriskVersion);
            return asteriskVersion;
        }

        private static async Task<AsteriskVersion> GetVersionByCoreShowVersion(this ManagerConnection source, CancellationToken cancellationToken)
        {
            var outAstVersion = AsteriskVersion.Unknown;
            string? outVersionString = null;
            var command = new CommandAction("core show version");
            try
            {
                var response = await source.SendActionAsync<CommandResponse>(command, cancellationToken);
                foreach (string line in response.GetResponse())
                {
                    foreach (Match m in Common.ASTERISK_VERSION.Matches(line))
                    {
                        if (m.Groups.Count >= 2)
                        {
                            outVersionString = m.Groups[1].Value;

                            // The 'if/else if' block is now a single 'switch' expression.
                            var version = outVersionString switch
                            {
                                string v when v.StartsWith("1.4.") => AsteriskVersion.ASTERISK_1_4,
                                string v when v.StartsWith("1.6.") => AsteriskVersion.ASTERISK_1_6,
                                string v when v.StartsWith("1.8.") => AsteriskVersion.ASTERISK_1_8,
                                string v when v.StartsWith("10.") => AsteriskVersion.ASTERISK_10,
                                string v when v.StartsWith("11.") => AsteriskVersion.ASTERISK_11,
                                string v when v.StartsWith("12.") => AsteriskVersion.ASTERISK_12,
                                string v when v.StartsWith("13.") => AsteriskVersion.ASTERISK_13,
                                string v when v.StartsWith("14.") => AsteriskVersion.ASTERISK_14,
                                string v when v.StartsWith("15.") => AsteriskVersion.ASTERISK_15,
                                string v when v.StartsWith("16.") => AsteriskVersion.ASTERISK_16,
                                string v when v.StartsWith("17.") => AsteriskVersion.ASTERISK_17,
                                string v when v.StartsWith("18.") => AsteriskVersion.ASTERISK_18,
                                string v when v.StartsWith("19.") => AsteriskVersion.ASTERISK_19,
                                string v when v.StartsWith("20.") => AsteriskVersion.ASTERISK_20,
                                string v when v.StartsWith("21.") => AsteriskVersion.ASTERISK_21,
                                string v when v.StartsWith("22.") => AsteriskVersion.ASTERISK_22,
                                string v when v.IndexOf('.') >= 2 => AsteriskVersion.ASTERISK_Newer,
                                _ => AsteriskVersion.Unknown // Default case
                            };

                            if (version != AsteriskVersion.Unknown)
                            {
                                // If a known version was found, return it immediately.
                                return version;
                            }
                            else
                            {
                                // Otherwise, log the unrecognized version string and continue looping.
                                _logger.LogInformation("unknown Asterisk version pattern from 'core show version': {VersionPattern}", outVersionString);
                            }
                        }
                    }
                }
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "exception during 'core show version' command for version determination.");
            }
            return outAstVersion;
        }

        private static readonly char[] s_pipeDelimiter = new char[] { '|' };
        private static readonly char[] s_commaDelimiter = new char[] { ',' };

        /// <summary>
        /// Gets the primary delimiter character for a specific Asterisk version.
        /// </summary>
        /// <param name="version">The Asterisk version.</param>
        /// <returns>The delimiter character.</returns>
        public static char[] GetDelimiters (AsteriskVersion? version)
        {
            return version switch
            {
                AsteriskVersion.ASTERISK_1_0 or
                AsteriskVersion.ASTERISK_1_2 or
                AsteriskVersion.ASTERISK_1_4 or 
                AsteriskVersion.ASTERISK_1_6 or
                AsteriskVersion.ASTERISK_1_8 or
                AsteriskVersion.ASTERISK_10 or
                AsteriskVersion.ASTERISK_Older
                => s_pipeDelimiter,
                _ => s_commaDelimiter
            };
        }

        public static char[] GetDelimiters(this ManagerConnection source)
            => GetDelimiters(source.AsteriskVersion);

        private static async Task<AsteriskVersion> GetVersionByShowVersionFiles(this ManagerConnection source, CancellationToken cancellationToken)
        {
            var command = new CommandAction("show version files");
            try
            {
                var response = await source.SendActionAsync<CommandResponse>(command, cancellationToken);

                if (response.Result is IList<string> showVersionFilesResult && showVersionFilesResult.Count > 0)
                {
                    if (showVersionFilesResult[0] is string line1 && line1.StartsWith("File", StringComparison.OrdinalIgnoreCase))
                    {
                        return AsteriskVersion.ASTERISK_1_2;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "exception during 'show version files' command for version determination.");
            }
            return AsteriskVersion.Unknown;
        }
    }
}
