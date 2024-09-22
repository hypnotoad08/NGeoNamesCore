using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGeoNames.Parsers;
using System.Text;

namespace NGeoNamesTests
{
	/// <summary>
	/// A custom parser implementation for parsing fields into a CustomEntity object.
	/// </summary>
	internal class CustomParser : IParser<CustomEntity>
	{
		public bool HasComments { get; private set; }
		public int SkipLines { get; private set; }
		public int ExpectedNumberOfFields { get; private set; }
		public Encoding Encoding { get; private set; }
		public char[] FieldSeparators { get; private set; }

		/// <summary>
		/// Constructor to initialize the CustomParser with specific parsing rules.
		/// </summary>
		/// <param name="expectedFields">Number of expected fields in each record.</param>
		/// <param name="skipLines">Number of lines to skip, e.g., for headers.</param>
		/// <param name="fieldSeparators">Field separator characters to split the input lines.</param>
		/// <param name="encoding">Encoding of the file.</param>
		/// <param name="hasComments">Indicates whether lines starting with comments should be ignored.</param>
		public CustomParser(int expectedFields, int skipLines, char[] fieldSeparators, Encoding encoding, bool hasComments)
		{
			SkipLines = skipLines;
			ExpectedNumberOfFields = expectedFields;
			FieldSeparators = fieldSeparators;
			Encoding = encoding;
			HasComments = hasComments;
		}

		/// <summary>
		/// Parses a set of fields into a CustomEntity object.
		/// </summary>
		/// <param name="fields">The input fields from a data row.</param>
		/// <returns>A new CustomEntity containing the parsed data.</returns>
		public CustomEntity Parse(string[] fields)
		{
			// Assert that the number of fields matches the expected number
			Assert.AreEqual(ExpectedNumberOfFields, fields.Length, "Number of fields does not match the expected value.");
			return new CustomEntity { Data = fields };
		}
	}
}