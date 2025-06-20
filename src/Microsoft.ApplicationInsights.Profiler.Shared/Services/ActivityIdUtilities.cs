//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal static class ActivityIdUtilities
{
    /// <summary>
    /// Calculate the activity id path like "//1/1/2" from a guid.
    /// </summary>
    /// <param name="guid">The activity id in format of a guid.</param>
    /// <returns>Returns the calculated result.</returns>
    public static string GetActivityPath(this Guid guid)
    {
        return CreateActivityPathString(guid);
    }

    /// <summary>
    /// Assuming guid is an Activity Path, extract the process ID from it.   
    /// </summary>
    private static unsafe int ActivityPathProcessID(Guid guid)
    {
        uint* uintPtr = (uint*)&guid;
        uint sum = uintPtr[0] + uintPtr[1] + uintPtr[2] + 0x599D99AD;
        return (int)(sum ^ uintPtr[3]);
    }

    private static unsafe string CreateActivityPathString(Guid guid)
    {
        var processID = ActivityPathProcessID(guid);
        StringBuilder sb = new StringBuilder();
        if (processID != 0)
        {
            sb.Append("/#");    // Use /# to mark the fact that the first number is a process ID.   
            sb.Append(processID);
        }
        else
        {
            sb.Append('/'); // Use // to start to make it easy to anchor
        }

        byte* bytePtr = (byte*)&guid;
        byte* endPtr = bytePtr + 12;
        char separator = '/';
        while (bytePtr < endPtr)
        {
            uint nibble = (uint)(*bytePtr >> 4);
            bool secondNibble = false;              // are we reading the second nibble (low order bits) of the byte.
        NextNibble:
            if (nibble == (uint)NumberListCodes.End)
                break;
            if (nibble <= (uint)NumberListCodes.LastImmediateValue)
            {
                sb.Append('/').Append(nibble);
                if (!secondNibble)
                {
                    nibble = (uint)(*bytePtr & 0xF);
                    secondNibble = true;
                    goto NextNibble;
                }
                // We read the second nibble so we move on to the next byte. 
                bytePtr++;
                continue;
            }
            else if (nibble == (uint)NumberListCodes.PrefixCode)
            {
                // This are the prefix codes.   If the next nibble is MultiByte, then this is an overflow ID.  
                // we we denote with a $ instead of a / separator.  

                // Read the next nibble.  
                if (!secondNibble)
                    nibble = (uint)(*bytePtr & 0xF);
                else
                {
                    bytePtr++;
                    if (endPtr <= bytePtr)
                        break;
                    nibble = (uint)(*bytePtr >> 4);
                }

                if (nibble < (uint)NumberListCodes.MultiByte1)
                {
                    // If the nibble is less than MultiByte we have not defined what that means 
                    // For now we simply give up, and stop parsing.  We could add more cases here...
                    return guid.ToString();
                }
                // If we get here we have a overflow ID, which is just like a normal ID but the separator is $
                separator = '$';
                // Fall into the Multi-byte decode case.  
            }

            Debug.Assert((uint)NumberListCodes.MultiByte1 <= nibble);
            // At this point we are decoding a multi-byte number, either a normal number or a 
            // At this point we are byte oriented, we are fetching the number as a stream of bytes. 
            uint numBytes = nibble - (uint)NumberListCodes.MultiByte1;

            uint value = 0;
            if (!secondNibble)
                value = (uint)(*bytePtr & 0xF);
            bytePtr++;       // Advance to the value bytes

            numBytes++;     // Now numBytes is 1-4 and represents the number of bytes to read.  
            if (endPtr < bytePtr + numBytes)
                break;

            // Compute the number (little endian) (thus backwards).  
            for (int i = (int)numBytes - 1; 0 <= i; --i)
                value = (value << 8) + bytePtr[i];

            // Print the value
            sb.Append(separator).Append(value);

            bytePtr += numBytes;        // Advance past the bytes.
        }

        sb.Append('/');
        return sb.ToString();
    }

    /// <summary>
    /// The encoding for a list of numbers used to make Activity  Guids.   Basically
    /// we operate on nibbles (which are nice because they show up as hex digits).  The
    /// list is ended with a end nibble (0) and depending on the nibble value (Below)
    /// the value is either encoded into nibble itself or it can spill over into the
    /// bytes that follow.   
    /// </summary>
    private enum NumberListCodes : byte
    {
        End = 0x0,             // ends the list.   No valid value has this prefix.   
        LastImmediateValue = 0xA,
        PrefixCode = 0xB,
        MultiByte1 = 0xC,   // 1 byte follows.  If this Nibble is in the high bits, it the high bits of the number are stored in the low nibble.   
                            // commented out because the code does not explicitly reference the names (but they are logically defined).  
                            // MultiByte2 = 0xD,   // 2 bytes follow (we don't bother with the nibble optimization
                            // MultiByte3 = 0xE,   // 3 bytes follow (we don't bother with the nibble optimization
                            // MultiByte4 = 0xF,   // 4 bytes follow (we don't bother with the nibble optimization
    }
}
