﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonic853.TotpGen
{
    public class HashLib
    {
        private static readonly ulong[] sha1_init = {
            0x67452301, 0xefcdab89, 0x98badcfe, 0x10325476, 0xc3d2e1f0,
        };
        public static byte[] SHA1(byte[] data)
        {
            ulong[] working_variables = new ulong[5];
            sha1_init.CopyTo(working_variables, 0);
            ulong size_mask = 0xFFFFFFFFul;
            int word_size = 32;
            int chunk_modulo = 64;
            int appended_length = 8;
            int left_rotations = 1;
            int round_count = 80;
            int output_segments = 5;

            int word_bytes = word_size / 8;

            byte[] input = new byte[chunk_modulo];
            ulong[] message_schedule = new ulong[round_count];

            for (int chunk_index = 0; chunk_index < data.Length + appended_length + 1; chunk_index += chunk_modulo)
            {
                int chunk_size = Math.Min(chunk_modulo, data.Length - chunk_index);
                int schedule_index = 0;

                for (; schedule_index < chunk_size; ++schedule_index)
                {
                    input[schedule_index] = data[chunk_index + schedule_index];
                }
                if (schedule_index < chunk_modulo && chunk_size >= 0)
                {
                    input[schedule_index++] = 0b10000000;
                }
                for (; schedule_index < chunk_modulo; ++schedule_index)
                {
                    input[schedule_index] = 0x00;
                }
                if (chunk_size < chunk_modulo - appended_length)
                {
                    ulong bit_size = (ulong)data.Length * 8ul;
                    input[chunk_modulo - 1] = Convert.ToByte((bit_size >> 0x00) & 0xFFul);
                    input[chunk_modulo - 2] = Convert.ToByte((bit_size >> 0x08) & 0xFFul);
                    input[chunk_modulo - 3] = Convert.ToByte((bit_size >> 0x10) & 0xFFul);
                    input[chunk_modulo - 4] = Convert.ToByte((bit_size >> 0x18) & 0xFFul);
                    input[chunk_modulo - 5] = Convert.ToByte((bit_size >> 0x20) & 0xFFul);
                    input[chunk_modulo - 6] = Convert.ToByte((bit_size >> 0x28) & 0xFFul);
                    input[chunk_modulo - 7] = Convert.ToByte((bit_size >> 0x30) & 0xFFul);
                    input[chunk_modulo - 8] = Convert.ToByte((bit_size >> 0x38) & 0xFFul);
                }

                // Copy into w[0..15]
                int copy_index = 0;
                for (; copy_index < 16; copy_index++)
                {
                    message_schedule[copy_index] = 0ul;
                    for (int i = 0; i < word_bytes; i++)
                    {
                        message_schedule[copy_index] = (message_schedule[copy_index] << 8) | input[(copy_index * word_bytes) + i];
                    }

                    message_schedule[copy_index] = message_schedule[copy_index] & size_mask;
                }
                // Extend
                for (; copy_index < round_count; copy_index++)
                {
                    ulong w = message_schedule[copy_index - 3] ^ message_schedule[copy_index - 8] ^ message_schedule[copy_index - 14] ^ message_schedule[copy_index - 16];
                    message_schedule[copy_index] = (
                        (w << left_rotations) | (w >> word_size - left_rotations)
                    ) & size_mask;
                }

                // temp vars
                ulong temp, k, f;
                // work is equivalent to a, b, c, d, e
                // This copies work from h0, h1, h2, h3, h4
                ulong[] work = new ulong[5];
                working_variables.CopyTo(work, 0);

                // Compression function main loop
                for (copy_index = 0; copy_index < round_count; copy_index++)
                {
                    if (copy_index < 20)
                    {
                        f = ((work[1] & work[2]) | ((size_mask ^ work[1]) & work[3])) & size_mask;
                        k = 0x5A827999;
                    }
                    else if (copy_index < 40)
                    {
                        f = work[1] ^ work[2] ^ work[3];
                        k = 0x6ED9EBA1;
                    }
                    else if (copy_index < 60)
                    {
                        f = (work[1] & work[2]) ^ (work[1] & work[3]) ^ (work[2] & work[3]);
                        k = 0x8F1BBCDC;
                    }
                    else
                    {
                        f = work[1] ^ work[2] ^ work[3];
                        k = 0xCA62C1D6;
                    }

                    temp = (((work[0] << 5) | (work[0] >> word_size - 5)) + f + work[4] + k + message_schedule[copy_index]) & size_mask;
                    work[4] = work[3];
                    work[3] = work[2];
                    work[2] = ((work[1] << 30) | (work[1] >> word_size - 30)) & size_mask;
                    work[1] = work[0];
                    work[0] = temp;
                }

                for (copy_index = 0; copy_index < 5; copy_index++)
                    working_variables[copy_index] = (working_variables[copy_index] + work[copy_index]) & size_mask;
            }

            byte[] output = new byte[output_segments * word_bytes];
            for (int i = 0; i < output_segments; i++)
            {
                for (int j = 0; j < word_bytes; j++)
                {
                    output[(i * word_bytes) + j] = Convert.ToByte((working_variables[i] >> (word_bytes - j - 1) * 8) & 0xFFul);
                }
            }
            return output;
        }
        public static byte[] HMACSHA1(byte[] data, byte[] key)
        {
            if (key.Length > 64)
            {
                key = SHA1(key);
            }

            byte[] ipad = new byte[64];
            byte[] opad = new byte[64];

            for (int i = 0; i < 64; i++)
            {
                ipad[i] = 0x36;
                opad[i] = 0x5c;
            }

            for (int i = 0; i < key.Length; i++)
            {
                ipad[i] ^= key[i];
                opad[i] ^= key[i];
            }

            byte[] inner = new byte[64 + data.Length];
            for (int i = 0; i < 64; i++)
            {
                inner[i] = ipad[i];
            }

            for (int i = 0; i < data.Length; i++)
            {
                inner[i + 64] = data[i];
            }

            byte[] innerHash = SHA1(inner);

            byte[] outer = new byte[64 + innerHash.Length];
            for (int i = 0; i < 64; i++)
            {
                outer[i] = opad[i];
            }

            for (int i = 0; i < innerHash.Length; i++)
            {
                outer[i + 64] = innerHash[i];
            }

            return SHA1(outer);
        }
        private static readonly ulong[] sha256_init = {
            0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a, 0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19,
        };
        private static readonly ulong[] sha256_constants = {
            0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
            0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
            0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
            0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
            0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
            0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
            0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
            0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2,
        };
        private static readonly int[] sha256_sums =
        {
            7, 18, 3,  // s0
            17, 19, 10,  // s1
        };

        private static readonly int[] sha256_sigmas =
        {
            2, 13, 22,  // S0
            6, 11, 25,  // S1
        };
        private static readonly ulong[] sha512_init = {
            0x6a09e667f3bcc908, 0xbb67ae8584caa73b, 0x3c6ef372fe94f82b, 0xa54ff53a5f1d36f1, 0x510e527fade682d1,
            0x9b05688c2b3e6c1f, 0x1f83d9abfb41bd6b, 0x5be0cd19137e2179,
        };

        private static readonly ulong[] sha512_constants = {
            0x428a2f98d728ae22, 0x7137449123ef65cd, 0xb5c0fbcfec4d3b2f, 0xe9b5dba58189dbbc, 0x3956c25bf348b538,
            0x59f111f1b605d019, 0x923f82a4af194f9b, 0xab1c5ed5da6d8118, 0xd807aa98a3030242, 0x12835b0145706fbe,
            0x243185be4ee4b28c, 0x550c7dc3d5ffb4e2, 0x72be5d74f27b896f, 0x80deb1fe3b1696b1, 0x9bdc06a725c71235,
            0xc19bf174cf692694, 0xe49b69c19ef14ad2, 0xefbe4786384f25e3, 0x0fc19dc68b8cd5b5, 0x240ca1cc77ac9c65,
            0x2de92c6f592b0275, 0x4a7484aa6ea6e483, 0x5cb0a9dcbd41fbd4, 0x76f988da831153b5, 0x983e5152ee66dfab,
            0xa831c66d2db43210, 0xb00327c898fb213f, 0xbf597fc7beef0ee4, 0xc6e00bf33da88fc2, 0xd5a79147930aa725,
            0x06ca6351e003826f, 0x142929670a0e6e70, 0x27b70a8546d22ffc, 0x2e1b21385c26c926, 0x4d2c6dfc5ac42aed,
            0x53380d139d95b3df, 0x650a73548baf63de, 0x766a0abb3c77b2a8, 0x81c2c92e47edaee6, 0x92722c851482353b,
            0xa2bfe8a14cf10364, 0xa81a664bbc423001, 0xc24b8b70d0f89791, 0xc76c51a30654be30, 0xd192e819d6ef5218,
            0xd69906245565a910, 0xf40e35855771202a, 0x106aa07032bbd1b8, 0x19a4c116b8d2d0c8, 0x1e376c085141ab53,
            0x2748774cdf8eeb99, 0x34b0bcb5e19b48a8, 0x391c0cb3c5c95a63, 0x4ed8aa4ae3418acb, 0x5b9cca4f7763e373,
            0x682e6ff3d6b2b8a3, 0x748f82ee5defb2fc, 0x78a5636f43172f60, 0x84c87814a1f0ab72, 0x8cc702081a6439ec,
            0x90befffa23631e28, 0xa4506cebde82bde9, 0xbef9a3f7b2c67915, 0xc67178f2e372532b, 0xca273eceea26619c,
            0xd186b8c721c0c207, 0xeada7dd6cde0eb1e, 0xf57d4f7fee6ed178, 0x06f067aa72176fba, 0x0a637dc5a2c898a6,
            0x113f9804bef90dae, 0x1b710b35131c471b, 0x28db77f523047d84, 0x32caab7b40c72493, 0x3c9ebe0a15c9bebc,
            0x431d67c49c100d4c, 0x4cc5d4becb3e42b6, 0x597f299cfc657e2a, 0x5fcb6fab3ad6faec, 0x6c44198c4a475817,
        };

        private static readonly int[] sha512_sums =
        {
            1, 8, 7,  // s0
            19, 61, 6,  // s1
        };

        private static readonly int[] sha512_sigmas =
        {
            28, 34, 39,  // S0
            14, 18, 41,  // S1
        };
        public static byte[] SHA2_Core(byte[] payload_bytes, ulong[] init, ulong[] constants, int[] sums, int[] sigmas, ulong size_mask, int word_size, int chunk_modulo, int appended_length, int round_count, int output_segments)
        {
            int word_bytes = word_size / 8;

            // Working variables h0->h7
            ulong[] working_variables = new ulong[8];
            init.CopyTo(working_variables, 0);

            byte[] input = new byte[chunk_modulo];
            ulong[] message_schedule = new ulong[round_count];

            // Each 64-byte/512-bit chunk
            // 64 bits/8 bytes are required at the end for the bit size
            for (int chunk_index = 0; chunk_index < payload_bytes.Length + appended_length + 1; chunk_index += chunk_modulo)
            {
                int chunk_size = Mathf.Min(chunk_modulo, payload_bytes.Length - chunk_index);
                int schedule_index = 0;

                // Buffer message
                for (; schedule_index < chunk_size; ++schedule_index)
                    input[schedule_index] = payload_bytes[chunk_index + schedule_index];
                // Append a 1-bit if not an even chunk
                if (schedule_index < chunk_modulo && chunk_size >= 0)
                    input[schedule_index++] = 0b10000000;
                // Pad with zeros until the end
                for (; schedule_index < chunk_modulo; ++schedule_index)
                    input[schedule_index] = 0x00;
                // If the chunk is less than 56 bytes, this will be the final chunk containing the data size in bits
                if (chunk_size < chunk_modulo - appended_length)
                {
                    ulong bit_size = (ulong)payload_bytes.Length * 8ul;
                    input[chunk_modulo - 1] = Convert.ToByte((bit_size >> 0x00) & 0xFFul);
                    input[chunk_modulo - 2] = Convert.ToByte((bit_size >> 0x08) & 0xFFul);
                    input[chunk_modulo - 3] = Convert.ToByte((bit_size >> 0x10) & 0xFFul);
                    input[chunk_modulo - 4] = Convert.ToByte((bit_size >> 0x18) & 0xFFul);
                    input[chunk_modulo - 5] = Convert.ToByte((bit_size >> 0x20) & 0xFFul);
                    input[chunk_modulo - 6] = Convert.ToByte((bit_size >> 0x28) & 0xFFul);
                    input[chunk_modulo - 7] = Convert.ToByte((bit_size >> 0x30) & 0xFFul);
                    input[chunk_modulo - 8] = Convert.ToByte((bit_size >> 0x38) & 0xFFul);
                }

                // Copy into w[0..15]
                int copy_index = 0;
                for (; copy_index < 16; copy_index++)
                {
                    message_schedule[copy_index] = 0ul;
                    for (int i = 0; i < word_bytes; i++)
                    {
                        message_schedule[copy_index] = (message_schedule[copy_index] << 8) | input[(copy_index * word_bytes) + i];
                    }

                    message_schedule[copy_index] = message_schedule[copy_index] & size_mask;
                }
                // Extend
                for (; copy_index < round_count; copy_index++)
                {
                    ulong s0_read = message_schedule[copy_index - 15];
                    ulong s1_read = message_schedule[copy_index - 2];

                    message_schedule[copy_index] = (
                        message_schedule[copy_index - 16] +
                        (((s0_read >> sums[0]) | (s0_read << word_size - sums[0])) ^ ((s0_read >> sums[1]) | (s0_read << word_size - sums[1])) ^ (s0_read >> sums[2])) + // s0
                        message_schedule[copy_index - 7] +
                        (((s1_read >> sums[3]) | (s1_read << word_size - sums[3])) ^ ((s1_read >> sums[4]) | (s1_read << word_size - sums[4])) ^ (s1_read >> sums[5])) // s1
                    ) & size_mask;
                }

                // temp vars
                ulong temp1, temp2;
                // work is equivalent to a, b, c, d, e, f, g, h
                // This copies work from h0, h1, h2, h3, h4, h5, h6, h7
                ulong[] work = new ulong[8];
                working_variables.CopyTo(work, 0);

                // Compression function main loop
                for (copy_index = 0; copy_index < round_count; copy_index++)
                {
                    ulong ep1 = ((work[4] >> sigmas[3]) | (work[4] << word_size - sigmas[3])) ^ ((work[4] >> sigmas[4]) | (work[4] << word_size - sigmas[4])) ^ ((work[4] >> sigmas[5]) | (work[4] << word_size - sigmas[5]));
                    ulong ch = (work[4] & work[5]) ^ ((size_mask ^ work[4]) & work[6]);
                    ulong ep0 = ((work[0] >> sigmas[0]) | (work[0] << word_size - sigmas[0])) ^ ((work[0] >> sigmas[1]) | (work[0] << word_size - sigmas[1])) ^ ((work[0] >> sigmas[2]) | (work[0] << word_size - sigmas[2]));
                    ulong maj = (work[0] & work[1]) ^ (work[0] & work[2]) ^ (work[1] & work[2]);
                    temp1 = work[7] + ep1 + ch + constants[copy_index] + message_schedule[copy_index];
                    temp2 = ep0 + maj;
                    work[7] = work[6];
                    work[6] = work[5];
                    work[5] = work[4];
                    work[4] = (work[3] + temp1) & size_mask;
                    work[3] = work[2];
                    work[2] = work[1];
                    work[1] = work[0];
                    work[0] = (temp1 + temp2) & size_mask;
                }

                for (copy_index = 0; copy_index < 8; copy_index++)
                    working_variables[copy_index] = (working_variables[copy_index] + work[copy_index]) & size_mask;
            }

            // Finalization
            byte[] output = new byte[output_segments * word_bytes];
            for (int i = 0; i < output_segments; i++)
            {
                for (int j = 0; j < word_bytes; j++)
                {
                    output[(i * word_bytes) + j] = Convert.ToByte((working_variables[i] >> (word_bytes - j - 1) * 8) & 0xFFul);
                }
            }
            return output;
        }
        public static byte[] SHA256(byte[] data)
        {
            return SHA2_Core(data, sha256_init, sha256_constants, sha256_sums, sha256_sigmas, 0xFFFFFFFFul, 32, 64, 8, 64, 8);
        }
        public static byte[] HMACSHA256(byte[] data, byte[] key)
        {
            if (key.Length > 64)
                key = SHA256(key);
            byte[] key_pad = new byte[64];
            byte[] key_pad_xor = new byte[64];
            for (int i = 0; i < 64; i++)
            {
                key_pad[i] = 0x36;
                key_pad_xor[i] = 0x5C;
            }
            for (int i = 0; i < key.Length; i++)
            {
                key_pad[i] ^= key[i];
                key_pad_xor[i] ^= key[i];
            }
            byte[] data1 = new byte[key_pad.Length + data.Length];
            key_pad.CopyTo(data1, 0);
            data.CopyTo(data1, key_pad.Length);
            byte[] data2 = new byte[key_pad_xor.Length + SHA256(data1).Length];
            key_pad_xor.CopyTo(data2, 0);
            SHA256(data1).CopyTo(data2, key_pad_xor.Length);
            return SHA256(data2);
        }
        public static byte[] SHA512(byte[] data)
        {
            return SHA2_Core(data, sha512_init, sha512_constants, sha512_sums, sha512_sigmas, 0xFFFFFFFFFFFFFFFFul, 64, 128, 16, 80, 8);
        }
        public static byte[] HMACSHA512(byte[] data, byte[] key)
        {
            if (key.Length > 128)
                key = SHA512(key);
            byte[] key_pad = new byte[128];
            byte[] key_pad_xor = new byte[128];
            for (int i = 0; i < 128; i++)
            {
                key_pad[i] = 0x36;
                key_pad_xor[i] = 0x5C;
            }
            for (int i = 0; i < key.Length; i++)
            {
                key_pad[i] ^= key[i];
                key_pad_xor[i] ^= key[i];
            }
            byte[] data1 = new byte[key_pad.Length + data.Length];
            key_pad.CopyTo(data1, 0);
            data.CopyTo(data1, key_pad.Length);
            byte[] data2 = new byte[key_pad_xor.Length + SHA512(data1).Length];
            key_pad_xor.CopyTo(data2, 0);
            SHA512(data1).CopyTo(data2, key_pad_xor.Length);
            return SHA512(data2);
        }
    }
}