
namespace Dlrsoft.VBScript.Parser
{
	// 
	// Visual Basic .NET Parser
	// 
	// Copyright (C) 2005, Microsoft Corporation. All rights reserved.
	// 
	// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
	// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
	// MERCHANTIBILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
	// 

	/// <summary>
/// Stores the location of a span of text.
/// </summary>
/// <remarks>The end location is exclusive.</remarks>
	public struct Span
	{
		private readonly Location _Start;
		private readonly Location _Finish;

		/// <summary>
    /// The start location of the span.
    /// </summary>
		public Location Start
		{
			get
			{
				return _Start;
			}
		}

		/// <summary>
    /// The end location of the span.
    /// </summary>
		public Location Finish
		{
			get
			{
				return _Finish;
			}
		}

		/// <summary>
    /// Whether the locations in the span are valid.
    /// </summary>
		public bool IsValid
		{
			get
			{
				return Start.IsValid && Finish.IsValid;
			}
		}

		/// <summary>
    /// Constructs a new span with a specific start and end location.
    /// </summary>
    /// <param name="start">The beginning of the span.</param>
    /// <param name="finish">The end of the span.</param>
		public Span(Location start, Location finish)
		{
			_Start = start;
			_Finish = finish;
		}

		/// <summary>
    /// Compares two specified Span values to see if they are equal.
    /// </summary>
    /// <param name="left">One span to compare.</param>
    /// <param name="right">The other span to compare.</param>
    /// <returns>True if the spans are the same, False otherwise.</returns>
		public static bool operator ==(Span left, Span right)
		{
			return left.Start.Index == right.Start.Index && left.Finish.Index == right.Finish.Index;
		}

		/// <summary>
    /// Compares two specified Span values to see if they are not equal.
    /// </summary>
    /// <param name="left">One span to compare.</param>
    /// <param name="right">The other span to compare.</param>
    /// <returns>True if the spans are not the same, False otherwise.</returns>
		public static bool operator !=(Span left, Span right)
		{
			return left.Start.Index != right.Start.Index || left.Finish.Index != right.Finish.Index;
		}

		public override string ToString()
		{
			return Start.ToString() + " - " + Finish.ToString();
		}

		public override bool Equals(object obj)
		{
			if (obj is Span)
			{
				return this == (Span)obj;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return (int)((long)(Start.Index ^ Finish.Index) & int.MinValue + 0x7FFFFFFF);
		}
	}
}