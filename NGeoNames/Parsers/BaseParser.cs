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

			// If the string ends with ".0", remove the ".0"
			if (value.EndsWith(".0"))
				value = value[..^2];

			return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		}

		protected long StringToLong(string value)
		{
			return long.Parse(value);
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
			return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
		}

		protected double StringToDouble(string value)
		{
			return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
		}

		protected DateTime StringToDateTime(string value, string format = "yyyy-MM-dd")
		{
			return DateTime.ParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None);
		}

		protected string StringToTimeZone(string value)
		{
			return value.Replace("_", " ");
		}
	}
}