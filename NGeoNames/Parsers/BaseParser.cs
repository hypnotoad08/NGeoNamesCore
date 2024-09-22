using System.Globalization;
using System.Text;
using System;

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

			if (int.TryParse(value, out var result))
				return result;

			throw new FormatException($"The value '{value}' is not a valid integer.");
		}

		protected long StringToLong(string value)
		{
			if (long.TryParse(value, out var result))
				return result;

			throw new FormatException($"The value '{value}' is not a valid long integer.");
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
			if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				return result;

			throw new FormatException($"The value '{value}' is not a valid float.");
		}

		protected double StringToDouble(string value)
		{
			if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				return result;

			throw new FormatException($"The value '{value}' is not a valid double.");
		}

		protected DateTime StringToDateTime(string value, string format = "yyyy-MM-dd")
		{
			if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
				return result;

			throw new FormatException($"The value '{value}' is not a valid DateTime in the format '{format}'.");
		}

		protected string StringToTimeZone(string value)
		{
			return value.Replace("_", " ");
		}
	}
}