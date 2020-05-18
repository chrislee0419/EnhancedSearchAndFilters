using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace EnhancedSearchAndFilters.Utilities
{
    public static class AudioFileUtilities
    {
        // technically a bit less than 2^16, but whatever
        private const int OGGMaxPageLength = 1 << 16;
        private const int OGGHeaderMinByteLength = 26;
        private const byte OGGStreamStructureVersion = 0x00;
        private const byte OGGLastPageHeaderType = 0x04;

        private const int VorbisIdentificationHeaderByteLength = 22;

        // 12 for chunk descriptor, max of 48 for format subchunk, 8 for data subchunk
        // and another 12 just in case or something
        private const int WAVHeaderMaxByteLength = 80;
        private const int WAVHeaderFormatSubchunkSizeOffset = 16;
        private const int WAVHeaderDataSubchunkSizeOffsetBase = 24;

        /// <summary>
        /// Get the length of the audio in a OGG Vorbis audio file asynchronously.
        /// </summary>
        /// <param name="fs">A <see cref="FileStream"/> of an existing OGG Vorbis file.</param>
        /// <returns>A task that returns the length of the audio file in seconds or -1 if an error was encountered.</returns>
        public static async Task<float> GetLengthOfOGGVorbisAudioFileAsync(FileStream fs)
        {
            if (!fs.CanSeek)
                return -1;

            long previousSeekPosition = fs.Position;
            if (fs.Length < OGGHeaderMinByteLength)
                return -1;

            // verify that this actually is an ogg file
            byte[] byteArray = new byte[OGGMaxPageLength];
            fs.Seek(0, SeekOrigin.Begin);
            await fs.ReadAsync(byteArray, 0, 4);

            if (!VerifyOGGHeader(byteArray))
                goto OnError;

            // get sample rate from vorbis identification header
            uint sampleRate = 0;
            await fs.ReadAsync(byteArray, 0, OGGMaxPageLength);
            for (int i = 0; i < OGGMaxPageLength - VorbisIdentificationHeaderByteLength; ++i)
            {
                // not gonna bother searching for the actual start of the identification header properly
                if (!VerifyVorbisIdentificationHeader(byteArray, i))
                    continue;

                // sample rate located at byte index 12 in the header
                sampleRate = BitConverter.ToUInt32(byteArray, i + 12);
                break;
            }

            if (sampleRate == 0)
                goto OnError;

            // get the last OGG page to find the total number of samples
            fs.Seek(-OGGMaxPageLength, SeekOrigin.End);
            await fs.ReadAsync(byteArray, 0, OGGMaxPageLength);

            ulong numOfSamples = 0;
            for (int i = 0; i < OGGMaxPageLength - OGGHeaderMinByteLength; ++i)
            {
                if (!VerifyOGGHeader(byteArray, i) ||
                    byteArray[i + 4] != OGGStreamStructureVersion ||
                    (byteArray[i + 5] & OGGLastPageHeaderType) == OGGLastPageHeaderType)
                    continue;

                // granule position holds the number of samples of a vorbis bitstream
                numOfSamples = ConvertBytesToUnsignedLong(byteArray, i + 6);
                break;
            }

            if (numOfSamples != 0)
            {
                fs.Position = previousSeekPosition;
                return (float)numOfSamples / sampleRate;
            }

        OnError:
            fs.Position = previousSeekPosition;
            return -1;
        }

        /// <summary>
        /// Get the length of the audio in a WAVE audio file asynchronously.
        /// </summary>
        /// <param name="fs">A <see cref="FileStream"/> of an existing WAVE file.</param>
        /// <returns>A task that returns the length of the audio file in seconds or -1 if an error was encountered.</returns>
        public static async Task<float> GetLengthOfWAVAudioFileAsync(FileStream fs)
        {
            if (!fs.CanSeek)
                return -1;

            long previousSeekPosition = fs.Position;
            if (fs.Length <= WAVHeaderMaxByteLength)
                return -1;

            // verify that this is a PCM WAV file
            byte[] byteArray = new byte[WAVHeaderMaxByteLength];
            fs.Seek(0, SeekOrigin.Begin);
            await fs.ReadAsync(byteArray, 0, WAVHeaderMaxByteLength);
            if (byteArray[0] != 'R' ||
                byteArray[1] != 'I' ||
                byteArray[2] != 'F' ||
                byteArray[3] != 'F' ||
                byteArray[8] != 'W' ||
                byteArray[9] != 'A' ||
                byteArray[10] != 'V' ||
                byteArray[11] != 'E' ||
                byteArray[12] != 'f' ||
                byteArray[13] != 'm' ||
                byteArray[14] != 't' ||
                byteArray[15] != ' ')
                goto OnError;

            uint byteRate = ConvertBytesToUnsignedInt(byteArray, 28);

            // get data size
            uint formatSubchunkSize = ConvertBytesToUnsignedInt(byteArray, WAVHeaderFormatSubchunkSizeOffset);
            uint dataSubchunkSize = ConvertBytesToUnsignedInt(byteArray, (int)formatSubchunkSize + WAVHeaderDataSubchunkSizeOffsetBase);

            fs.Position = previousSeekPosition;
            return ((float)dataSubchunkSize) / byteRate;

        OnError:
            fs.Position = previousSeekPosition;
            return -1;
        }

        private static bool VerifyOGGHeader(byte[] byteArray, int offset = 0)
        {
            if (byteArray.Length - offset < 5)
                return false;

            return byteArray[offset] == 'O' &&
                byteArray[offset + 1] == 'g' &&
                byteArray[offset + 2] == 'g' &&
                byteArray[offset + 3] == 'S' &&
                byteArray[offset + 4] == 0x0;
        }

        private static bool VerifyVorbisIdentificationHeader(byte[] byteArray, int offset = 0)
        {
            if (byteArray.Length - offset < 8)
                return false;

            return byteArray[offset] == 0x01 &&
                byteArray[offset + 1] == 'v' &&
                byteArray[offset + 2] == 'o' &&
                byteArray[offset + 3] == 'r' &&
                byteArray[offset + 4] == 'b' &&
                byteArray[offset + 5] == 'i' &&
                byteArray[offset + 6] == 's';
        }

        private static ulong ConvertBytesToUnsignedLong(byte[] byteArray, int offset = 0)
        {
            if (BitConverter.IsLittleEndian)
                return BitConverter.ToUInt64(byteArray, offset);
            else
                return BitConverter.ToUInt64(byteArray.Skip(offset).Take(8).Reverse().ToArray(), 0);
        }

        private static uint ConvertBytesToUnsignedInt(byte[] byteArray, int offset = 0)
        {
            if (BitConverter.IsLittleEndian)
                return BitConverter.ToUInt32(byteArray, offset);
            else
                return BitConverter.ToUInt32(byteArray.Skip(offset).Take(8).Reverse().ToArray(), 0);
        }
    }
}
