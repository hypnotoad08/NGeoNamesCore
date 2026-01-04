using System;
using System.Globalization;
using System.Text;

namespace NGeoNames.Parsers
{
	public abstract class BaseParser<T> : IParser<T>
	{
		private static readonly char[] csv = { ',' };
		public static readonly char[] DEFAULTFIELDSEPARATORS = { '\t' };
		public static readonly Encoding DEFAULTENCODING = Encoding.UTF8;

		public Encoding Encoding { get; set; }
		public char[] FieldSeparators { get; set; }

		public abstract bool HasComments { get; }
		public abstract int SkipLines { get; }
		public abstract int ExpectedNumberOfFields { get; }

		public abstract T Parse(string[] fields);

		public BaseParser() : this(DEFAULTENCODING, DEFAULTFIELDSEPARATORS)
		{
		}

		public BaseParser(Encoding encoding, char[] fieldseparators)
		{
			Encoding = encoding;
			FieldSeparators = fieldseparators;
		}

		protected int StringToInt(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentNullException(nameof(value), "Value cannot be null or empty.");

			return StringToInt(value.AsSpan());
		}

		/// <summary>
		/// Parses a span of characters to an integer, optimized for performance using Span&lt;T&gt;.
		/// </summary>
		/// <param name="value">The span of characters to parse.</param>
		/// <returns>The parsed integer value.</returns>
		protected int StringToInt(ReadOnlySpan<char> value)
		{
			if (value.IsEmpty || value.IsWhiteSpace())
				throw new ArgumentException("Value cannot be null or empty.", nameof(value));

			// If the span ends with ".0", trim it
			if (value.Length >= 2 && value[^2] == '.' && value[^1] == '0')
				value = value[..^2];

			return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		}

		protected long StringToLong(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentNullException(nameof(value), "Value cannot be null or empty.");

			return StringToLong(value.AsSpan());
		}

		/// <summary>
		/// Parses a span of characters to a long integer, optimized for performance using Span&lt;T&gt;.
		/// </summary>
		/// <param name="value">The span of characters to parse.</param>
		/// <returns>The parsed long integer value.</returns>
		protected long StringToLong(ReadOnlySpan<char> value)
		{
			if (value.IsEmpty || value.IsWhiteSpace())
				throw new ArgumentException("Value cannot be null or empty.", nameof(value));

			return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		}

		protected string[] StringToArray(string value, bool removeEmptyEntries = true)
		{
			return StringToArray(value, csv, removeEmptyEntries);
		}

		protected string[] StringToArray(string value, char[] delimiter, bool removeEmptyEntries = true)
		{
			var options = removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;
			return value.Split(delimiter, options);
		}

		protected float StringToFloat(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentNullException(nameof(value), "Value cannot be null or empty.");

			return StringToFloat(value.AsSpan());
		}

		/// <summary>
		/// Parses a span of characters to a float, optimized for performance using Span&lt;T&gt;.
		/// </summary>
		/// <param name="value">The span of characters to parse.</param>
		/// <returns>The parsed float value.</returns>
		protected float StringToFloat(ReadOnlySpan<char> value)
		{
			if (value.IsEmpty || value.IsWhiteSpace())
				throw new ArgumentException("Value cannot be null or empty.", nameof(value));

			return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
		}

		protected double StringToDouble(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentNullException(nameof(value), "Value cannot be null or empty.");

			return StringToDouble(value.AsSpan());
		}

		/// <summary>
		/// Parses a span of characters to a double, optimized for performance using Span&lt;T&gt;.
		/// </summary>
		/// <param name="value">The span of characters to parse.</param>
		/// <returns>The parsed double value.</returns>
		protected double StringToDouble(ReadOnlySpan<char> value)
		{
			if (value.IsEmpty || value.IsWhiteSpace())
				throw new ArgumentException("Value cannot be null or empty.", nameof(value));

			return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
		}

		protected DateTime StringToDateTime(string value, string format = "yyyy-MM-dd")
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentNullException(nameof(value), "Value cannot be null or empty.");

			return StringToDateTime(value.AsSpan(), format);
		}

		/// <summary>
		/// Parses a span of characters to a DateTime, optimized for performance using Span&lt;T&gt;.
		/// </summary>
		/// <param name="value">The span of characters to parse.</param>
		/// <param name="format">The expected date format.</param>
		/// <returns>The parsed DateTime value.</returns>
		protected DateTime StringToDateTime(ReadOnlySpan<char> value, string format = "yyyy-MM-dd")
		{
			if (value.IsEmpty || value.IsWhiteSpace())
				throw new ArgumentException("Value cannot be null or empty.", nameof(value));

			return DateTime.ParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None);
		}

		protected string StringToTimeZone(string value)
		{
			return value.Replace("_", " ");
		}
	}
}