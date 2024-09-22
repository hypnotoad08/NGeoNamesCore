﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGeoNames;
using NGeoNames.Parsers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGeoNamesTests
{
	internal class FileUtil
	{
		// Compares two data files using a GenericEntity to easily compare actual values without bothering with newline differences,
		// comments etc. nor trying to "understand" what they mean
		public static void EnsureFilesAreFunctionallyEqual(string src, string dst, int expectedfields, int skiplines, char[] fieldseparators, Encoding encoding, bool hascomments)
		{
			var parser_in = new GenericParser(expectedfields, skiplines, fieldseparators, encoding, hascomments);
			var parser_out = new GenericParser(expectedfields, 0, fieldseparators, encoding, false);

			var expected = new GeoFileReader().ReadRecordsAsync(src, FileType.Plain, parser_in).ToListAsync().Result.ToArray();
			var actual = new GeoFileReader().ReadRecordsAsync(dst, FileType.Plain, parser_out).ToListAsync().Result.ToArray();

			CollectionAssert.AreEqual(expected, actual, new GenericEntityComparer());
		}

		private class GenericParser : IParser<GenericEntity>
		{
			public bool HasComments { get; private set; }
			public int SkipLines { get; private set; }
			public int ExpectedNumberOfFields { get; private set; }
			public Encoding Encoding { get; private set; }
			public char[] FieldSeparators { get; private set; }

			public GenericParser(int expectedfields, int skiplines, char[] fieldseparators, Encoding encoding, bool hascomments)
			{
				SkipLines = skiplines;
				ExpectedNumberOfFields = expectedfields;
				FieldSeparators = fieldseparators;
				Encoding = encoding;
				HasComments = hascomments;
			}

			public GenericEntity Parse(string[] fields)
			{
				Assert.AreEqual(ExpectedNumberOfFields, fields.Length);
				return new GenericEntity { Data = fields };
			}
		}

		private class GenericEntity
		{
			public string[] Data { get; set; }
		}

		private class GenericEntityComparer : IComparer<GenericEntity>, IComparer
		{
			public int Compare(GenericEntity x, GenericEntity y)
			{
				int r = x.Data.Length.CompareTo(y.Data.Length);
				if (r != 0)
					return r;

				for (int i = 0; i < x.Data.Length && r == 0; i++)
					r = x.Data[i].CompareTo(y.Data[i]);
				return r;
			}

			public int Compare(object x, object y)
			{
				return Compare(x as GenericEntity, y as GenericEntity);
			}
		}
	}
}