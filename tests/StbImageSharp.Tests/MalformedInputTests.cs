using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace StbImageSharp.Tests
{
    /// <summary>
    /// Decoding files that are not what they claim to be.
    ///
    /// <para>Every decoder here scans for its next marker by reading bytes until end-of-file, so
    /// "am I at the end?" has to be answerable. It was not: a segment header declaring more bytes
    /// than the file contains - which is what a truncated download or a corrupt archive entry looks
    /// like - seeked the stream PAST its length, and the end-of-file test compared position to
    /// length for equality. Past the end is not equal to the end, so it reported "no" forever and
    /// the marker scan span indefinitely.</para>
    ///
    /// <para>An image that will not decode has to fail, not hang. Anything reading files it did not
    /// produce - a map archive, a user's folder - will meet one of these eventually.</para>
    /// </summary>
    public class MalformedInputTests
    {
        public static IEnumerable<object[]> MalformedInputs()
        {
            // A JPEG APP0 header claiming 16 bytes inside an 11-byte file. This is the one that hung.
            yield return ["truncated JPEG header", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 16, (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0 }];
            yield return ["bare JPEG SOI", new byte[] { 0xFF, 0xD8 }];
            yield return ["truncated PNG", new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13 }];
            yield return ["truncated BMP", new byte[] { (byte)'B', (byte)'M', 0, 0, 0, 0 }];
            yield return ["not an image at all", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }];
            yield return ["a single byte", new byte[] { 0xFF }];
            yield return ["empty", Array.Empty<byte>()];
        }

        [Theory]
        [MemberData(nameof(MalformedInputs))]
        public void MalformedInputFailsPromptlyRatherThanHanging(string description, byte[] data)
        {
            // The timeout IS the assertion. Without it a regression here does not fail the suite,
            // it stops the suite - which is exactly how this went unnoticed.
            Task<Exception?> attempt = Task.Run<Exception?>(() =>
            {
                try
                {
                    ImageResult.FromMemory(data);
                    return null;
                }
                catch (Exception e)
                {
                    return e;
                }
            });

            Assert.True(attempt.Wait(TimeSpan.FromSeconds(10)),
                $"Decoding {description} did not return within 10 seconds.");
            Assert.NotNull(attempt.Result);
        }

        /// <summary>
        /// The 16-bit path shares the same reader, so it shares the same failure mode.
        /// </summary>
        [Theory]
        [MemberData(nameof(MalformedInputs))]
        public void MalformedInputAlsoFailsPromptlyAtSixteenBits(string description, byte[] data)
        {
            Task<Exception?> attempt = Task.Run<Exception?>(() =>
            {
                try
                {
                    ImageResultFloat.FromMemory(data);
                    return null;
                }
                catch (Exception e)
                {
                    return e;
                }
            });

            Assert.True(attempt.Wait(TimeSpan.FromSeconds(10)),
                $"Decoding {description} did not return within 10 seconds.");
            Assert.NotNull(attempt.Result);
        }
    }
}
