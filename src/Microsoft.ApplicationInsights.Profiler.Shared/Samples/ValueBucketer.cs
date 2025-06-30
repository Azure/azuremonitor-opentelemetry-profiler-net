//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

#nullable disable

using System;
using System.Diagnostics;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Samples;

/// <summary>
/// Logically ValueBucketer looks up at T based on a floating point value, however it buckets the
/// floating point values by precision, so there are pretty finite number of distinct values (even if the
/// values span several orders of magnitude)
/// </summary>
internal class ValueBucketer<T, S> where T : ValueBucket<S>, new()
{
    private readonly object _lock = new();

    /// <summary>
    /// Create a new Histogram with 0 samples in it.  Precision is 'uncertainly' you wish to bucket by.
    /// 0.1 (10%) is the default.  For example at 10%,  all values between .95 and 1.05 are all put in the 1.0
    /// bucket.    Making this number small means a large number of buckets.  You should use as large a value
    /// as possible.  Values between 10% and 1% are reasonable. in most cases.
    /// </summary>
    public ValueBucketer(double precision = 0.1, double minimumValue = 1.0)
    {
        Debug.Assert(0 < precision && precision < 1.0);     // Anything outside .01 and .1 is frankly wacky.
        Debug.Assert(0 < minimumValue);                     // The smallest number we can handle.
        m_precision = precision;
        m_minimumValue = minimumValue;
        m_logOnePlusPrecision = Math.Log(1 + m_precision);          // Precompute this.
    }

    /// <summary>
    /// Get the T associated with the bucket by index.
    /// </summary>
    public T GetByIndex(int index)
    {
        int toUse = index;
        T[] values;

        // Block concurrent code execution before bucket expansion finished.
        // Otherwise, IndexOutOfRangeException might happen on line 53 to access the array.
        lock (_lock)
        {
            // This is how you get the actual index in the internal array, this is an implementation detail
            toUse -= m_smallestNonEmptyBucket;

            // Take a ref to m_values.
            values = m_values;
            if (toUse < 0 || values == null || values.Length <= toUse)
            {
                toUse = ExpandBuckets(toUse + m_smallestNonEmptyBucket);
                // m_values is mutable.
                values = m_values;
            }
        }

        T ret = values[toUse];
        if (ret == null)
        {
            values[toUse] = ret = new T();
        }

        return ret;
    }

    /// <summary>
    /// Get the T associated with the bucket which 'value' gets put in.   Will do a new T() if it the entry does not exist.
    /// </summary>
    public T Get(double value)
    {
        int index = GetBucketIndex(value);
        var bucket = GetByIndex(index);
        bucket.BucketIndex = index;

        return bucket;
    }

    /// <summary>
    /// Performs 'action' for every bucket in the histogram. action is passed the canonical value
    /// for the bucket and the T associated with the bucket.  Only buckets that have had 'Get'
    /// called on them are returned.
    /// </summary>
    public void ForEach(Func<double, T, bool> action)
    {
        T[] values;
        double value;
        lock (_lock)
        {
            values = m_values;
            if (values is null)
            {
                return;
            }

            value = Math.Exp(m_logOnePlusPrecision * m_smallestNonEmptyBucket) * m_minimumValue * (1 + Precision / 2);
        }

        double onePlusPrecision = 1 + Precision;
        for (int i = 0; i < values.Length; i++)
        {
            if (!action(value, values[i])) { return; }

            value *= onePlusPrecision;
        }
    }

    // lesser used things
    public double Precision { get { return m_precision; } }

    public double MinimumValue { get { return m_minimumValue; } }

    /// <summary>
    /// Given value, find the minimum bucket value for it.
    /// </summary>
    public double RoundToBucketMinimum(double value)
    {
        // This is just more logarithm math.   take the inverse log.
        return Math.Exp(m_logOnePlusPrecision * GetBucketIndex(value));
    }

    /// <summary>
    /// RoundToBucketValue takes a 'value' and returns the canonical
    /// value (the value that minimizes the error between this value and all the values in the bucket.
    /// </summary>
    public double RoundToBucketValue(double value)
    {
        // This is 'half way' to the next bucket value
        return RoundToBucketMinimum(value) * (1 + Precision / 2);
    }

    /// <summary>
    /// The difference between the start of the next bucket and the start of this bucket
    /// (Thus the size of the bucket).  for the bucket containing 'value'
    /// </summary>
    public double BucketSize(double value)
    {
        // A bucket's size is always (1 + precision) as big as its starting value.
        return RoundToBucketMinimum(value) * Precision;
    }

    /// <summary>
    /// Get bucket value from bucket index.
    /// </summary>
    /// <remarks>
    /// This is the reverse function of 'GetBucketIndex(double value)'
    /// </remarks>
    public double GetBucketValue(int bucketIndex)
    {
        double value = Math.Exp(m_logOnePlusPrecision * m_smallestNonEmptyBucket) * m_minimumValue * (1 + Precision / 2);
        double onePlusPrecision = 1 + Precision;
        return value * Math.Pow(onePlusPrecision, bucketIndex);
    }

    /// <summary>
    /// The bucket number is largest number N such that minimumValue*pow(1+precision, N) < value.
    /// This always returns a value >= 0 because it round value up to minimum value if it is not already.
    ///
    /// Imagine minimum value == 1 and precision == .1 (10% error)
    /// numbers between 1 and 1.1  have a bucket index of 0
    /// numbers between 1.1 and 1.21 (1.1 squared) have a bucket index of 1
    /// numbers between 1.21 and 1.331 (1.1 cubed) have a bucket index of 2
    /// and so on.
    /// </summary>
    public int GetBucketIndex(double value)
    {
        if (value <= m_minimumValue)
        {
            // Values smaller than the minimum all go in bucket 0
            return 0;
        }

        // This is just basic logarithm math.
        return (int)Math.Floor((Math.Log(value / m_minimumValue) / m_logOnePlusPrecision));
    }

    #region private
    /// <summary>
    /// Called when 'bucketIndex' is outside the range that we can store (see m_smallestNonZeroBucket)
    /// returns the index into the m_values array that can be used for that bucket index.
    /// </summary>
    private int ExpandBuckets(int bucketIndex)
    {
        // Special case, we are expanding from nothing, we don't need (and in fact can't copy old values).
        if (m_values == null)
        {
            m_smallestNonEmptyBucket = Math.Max(0, bucketIndex - 8);       // we allocate 8 entries before the bucket we hit
            m_values = new T[16];                                          // and 8 after (for a total of 16)
            return bucketIndex - m_smallestNonEmptyBucket;
        }
        // Case 1, bucket index is before the array of values => m_smallestNonEmptyBucket should be made smaller.
        int arrayIndex = bucketIndex - m_smallestNonEmptyBucket;
        if (arrayIndex < 0)
        {
            int newSmallestNonEmptyBucket = Math.Min(0, bucketIndex - 8);       // we expand by a bit more than we need to avoid doing this too much.
            arrayIndex = bucketIndex - newSmallestNonEmptyBucket;
            int delta = m_smallestNonEmptyBucket - newSmallestNonEmptyBucket;
            int newArrayLen = m_values.Length + delta;
            T[] newValues = new T[newArrayLen];
            Array.Copy(m_values, 0, newValues, delta, m_values.Length);
            m_values = newValues;
            m_smallestNonEmptyBucket = newSmallestNonEmptyBucket;
        }
        else
        {
            if (arrayIndex < m_values.Length)
                return arrayIndex;      // No expansion needed.  We really should not have called ExpandBuckets.

            // Case 2 bucket index is off the end of the array of values => make array bigger.  by at least 8 or 125%
            int newArrayLen = Math.Max(arrayIndex + 8, m_values.Length * 5 / 4);
            T[] newValues = new T[newArrayLen];
            Array.Copy(m_values, 0, newValues, 0, m_values.Length);
            m_values = newValues;
        }

        return arrayIndex;
    }

    // From the constructor
    readonly double m_precision;
    readonly double m_minimumValue;
    // computed from m_precision, and is constant, used in GetBucketIndex.
    readonly double m_logOnePlusPrecision;

    // conceptually we just have an array of values indexed by the bucket index.   However many early
    // values may be null.  To save space, we only allocate storage for non-empty contiguous range.
    int m_smallestNonEmptyBucket;
    T[] m_values;
    #endregion
}
