using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonic853.TotpGen
{
    public class TOTP
    {
        public static string key = "";
        public static int period = 30;
        public static int digits = 6;
        public static int tolerance = 1;
        public static int mode = 0;
        static long unixEpochTicks = 621355968000000000L;
        static long ticksToSeconds = 10000000L;
        public static string ComputeTotp()
        {
            return ComputeTotpWithTime(DateTime.UtcNow);
        }
        public static string ComputeTotpWithTime(DateTime time)
        {
            long unixTimestamp = (time.Ticks - unixEpochTicks) / ticksToSeconds;
            var counter = unixTimestamp / (long)period;
            byte[] hmacComputedHash;
            switch (mode)
            {
                case 0:
                    {
                        hmacComputedHash = HashLib.HMACSHA1(
                            GetBigEndianBytes(counter),
                            GetUTF8Bytes(key)
                        );
                    }
                    break;
                case 1:
                    {
                        hmacComputedHash = HashLib.HMACSHA256(
                            GetBigEndianBytes(counter),
                            GetUTF8Bytes(key)
                        );
                    }
                    break;
                case 2:
                    {
                        hmacComputedHash = HashLib.HMACSHA512(
                            GetBigEndianBytes(counter),
                            GetUTF8Bytes(key)
                        );
                    }
                    break;
                default:
                    return null;
            }
            int offset = hmacComputedHash[hmacComputedHash.Length - 1] & 0xf;
            long binary =
                ((hmacComputedHash[offset] & 0x7f) << 24) |
                ((hmacComputedHash[offset + 1] & 0xff) << 16) |
                ((hmacComputedHash[offset + 2] & 0xff) << 8) |
                (hmacComputedHash[offset + 3] & 0xff) % 1000000;
            return Digits(binary, digits);
        }
        public static bool VerifyTotp(string code)
        {
            if (code == null || code.Length == 0 || code.Length != digits)
            {
                return false;
            }
            for (int i = -tolerance; i <= tolerance; i++)
            {
                if (ComputeTotpWithTime(DateTime.UtcNow.AddSeconds(i * period)) == code)
                {
                    return true;
                }
            }
            return false;
        }
        static byte[] GetUTF8Bytes(string input)
        {
            char[] characters = input.ToCharArray();
            byte[] buffer = new byte[characters.Length * 4];

            int writeIndex = 0;
            for (int i = 0; i < characters.Length; i++)
            {
                uint character = characters[i];

                if (character < 0x80)
                {
                    buffer[writeIndex++] = (byte)character;
                }
                else if (character < 0x800)
                {
                    buffer[writeIndex++] = (byte)(0b11000000 | ((character >> 6) & 0b11111));
                    buffer[writeIndex++] = (byte)(0b10000000 | (character & 0b111111));
                }
                else if (character < 0x10000)
                {
                    buffer[writeIndex++] = (byte)(0b11100000 | ((character >> 12) & 0b1111));
                    buffer[writeIndex++] = (byte)(0b10000000 | ((character >> 6) & 0b111111));
                    buffer[writeIndex++] = (byte)(0b10000000 | (character & 0b111111));
                }
                else
                {
                    buffer[writeIndex++] = (byte)(0b11110000 | ((character >> 18) & 0b111));
                    buffer[writeIndex++] = (byte)(0b10000000 | ((character >> 12) & 0b111111));
                    buffer[writeIndex++] = (byte)(0b10000000 | ((character >> 6) & 0b111111));
                    buffer[writeIndex++] = (byte)(0b10000000 | (character & 0b111111));
                }
            }

            // We do this to truncate off the end of the array
            // This would be a lot easier with Array.Resize, but Udon once again does not allow access to it.
            byte[] output = new byte[writeIndex];

            for (int i = 0; i < writeIndex; i++)
                output[i] = buffer[i];

            return output;
        }
        static byte[] GetBigEndianBytes(long input)
        {
            byte[] output = new byte[8];
            for (int i = 7; i >= 0; i--)
            {
                output[i] = (byte)(input & 0xff);
                input >>= 8;
            }
            return output;
        }
        static string Digits(long input, int digitCount)
        {
            var truncatedValue = ((int)input % (int)Math.Pow(10, digitCount));
            return truncatedValue.ToString().PadLeft(digitCount, '0');
        }
    }
}
