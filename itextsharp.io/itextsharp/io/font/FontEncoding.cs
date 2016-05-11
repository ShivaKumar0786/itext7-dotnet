/*
$Id: f5b4e9d4ee338500a7b83183ad82d333cc8c561f $

This file is part of the iText (R) project.
Copyright (c) 1998-2016 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using com.itextpdf.io.util;

namespace com.itextpdf.io.font
{
	public class FontEncoding
	{
		private const long serialVersionUID = -684967385759439083L;

		private static readonly byte[] emptyBytes = new byte[0];

		public const String FONT_SPECIFIC = "FontSpecific";

		/// <summary>Base font encoding.</summary>
		protected internal String baseEncoding;

		/// <summary>
		/// <see langword="true"/>
		/// if the font must use its built in encoding. In that case
		/// the
		/// <c>encoding</c>
		/// is only used to map a char to the position inside the font, not to the expected char name.
		/// </summary>
		protected internal bool fontSpecific;

		/// <summary>Mapping map from unicode to simple code according to the encoding.</summary>
		protected internal IntHashtable unicodeToCode;

		protected internal int[] codeToUnicode;

		/// <summary>Encoding names.</summary>
		protected internal String[] differences;

		/// <summary>Encodings unicode differences</summary>
		protected internal IntHashtable unicodeDifferences;

		protected internal FontEncoding()
		{
			unicodeToCode = new IntHashtable(256);
			codeToUnicode = new int[256];
			ArrayUtil.FillWithValue(codeToUnicode, -1);
			unicodeDifferences = new IntHashtable(256);
			fontSpecific = false;
		}

		public static com.itextpdf.io.font.FontEncoding CreateFontEncoding(String baseEncoding
			)
		{
			com.itextpdf.io.font.FontEncoding encoding = new com.itextpdf.io.font.FontEncoding
				();
			encoding.baseEncoding = NormalizeEncoding(baseEncoding);
			if (encoding.baseEncoding.StartsWith("#"))
			{
				encoding.FillCustomEncoding();
			}
			else
			{
				encoding.FillNamedEncoding();
			}
			return encoding;
		}

		public static com.itextpdf.io.font.FontEncoding CreateEmptyFontEncoding()
		{
			com.itextpdf.io.font.FontEncoding encoding = new com.itextpdf.io.font.FontEncoding
				();
			encoding.baseEncoding = null;
			encoding.fontSpecific = false;
			encoding.differences = new String[256];
			for (int ch = 0; ch < 256; ch++)
			{
				encoding.unicodeDifferences.Put(ch, ch);
			}
			return encoding;
		}

		/// <summary>This encoding will base on font encoding (FontSpecific encoding in Type 1 terminology)
		/// 	</summary>
		public static com.itextpdf.io.font.FontEncoding CreateFontSpecificEncoding()
		{
			com.itextpdf.io.font.FontEncoding encoding = new com.itextpdf.io.font.FontEncoding
				();
			encoding.fontSpecific = true;
			for (int ch = 0; ch < 256; ch++)
			{
				encoding.unicodeToCode.Put(ch, ch);
				encoding.codeToUnicode[ch] = ch;
				encoding.unicodeDifferences.Put(ch, ch);
			}
			return encoding;
		}

		public virtual String GetBaseEncoding()
		{
			return baseEncoding;
		}

		public virtual bool IsFontSpecific()
		{
			return fontSpecific;
		}

		public virtual bool AddSymbol(int code, int unicode)
		{
			if (code < 0 || code > 255)
			{
				return false;
			}
			String glyphName = AdobeGlyphList.UnicodeToName(unicode);
			if (glyphName != null)
			{
				unicodeToCode.Put(unicode, code);
				codeToUnicode[code] = unicode;
				differences[code] = glyphName;
				unicodeDifferences.Put(unicode, unicode);
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>Gets unicode value for corresponding font's char code.</summary>
		/// <param name="index">font's char code</param>
		/// <returns>-1, if the char code unsupported or valid unicode.</returns>
		public virtual int GetUnicode(int index)
		{
			return codeToUnicode[index];
		}

		public virtual int GetUnicodeDifference(int index)
		{
			return unicodeDifferences.Get(index);
		}

		public virtual bool HasDifferences()
		{
			return differences != null;
		}

		public virtual String GetDifference(int index)
		{
			return differences != null ? differences[index] : null;
		}

		/// <summary>
		/// Converts a
		/// <c>String</c>
		/// to a
		/// <c>byte</c>
		/// array according to the encoding.
		/// String could contain a unicode symbols or font specific codes.
		/// </summary>
		/// <param name="text">
		/// the
		/// <c>String</c>
		/// to be converted.
		/// </param>
		/// <returns>
		/// an array of
		/// <c>byte</c>
		/// representing the conversion according to the encoding
		/// </returns>
		public virtual byte[] ConvertToBytes(String text)
		{
			if (text == null || text.Length == 0)
			{
				return emptyBytes;
			}
			int ptr = 0;
			byte[] bytes = new byte[text.Length];
			for (int i = 0; i < text.Length; i++)
			{
				if (unicodeToCode.ContainsKey(text[i]))
				{
					bytes[ptr++] = unchecked((byte)ConvertToByte(text[i]));
				}
			}
			return ArrayUtil.ShortenArray(bytes, ptr);
		}

		/// <summary>
		/// Converts a unicode symbol or font specific code
		/// to
		/// <c>byte</c>
		/// according to the encoding.
		/// </summary>
		/// <param name="unicode">a unicode symbol or FontSpecif code to be converted.</param>
		/// <returns>
		/// a
		/// <c>byte</c>
		/// representing the conversion according to the encoding
		/// </returns>
		public virtual int ConvertToByte(int unicode)
		{
			return unicodeToCode.Get(unicode);
		}

		/// <summary>
		/// Check whether a unicode symbol or font specific code can be converted
		/// to
		/// <c>byte</c>
		/// according to the encoding.
		/// </summary>
		/// <param name="unicode">a unicode symbol or font specific code to be checked.</param>
		/// <returns>
		/// 
		/// <see langword="true"/>
		/// if
		/// <c>ch</c>
		/// could be encoded.
		/// </returns>
		public virtual bool CanEncode(int unicode)
		{
			return unicodeToCode.ContainsKey(unicode);
		}

		/// <summary>
		/// Check whether a
		/// <c>byte</c>
		/// code can be converted
		/// to unicode symbol according to the encoding.
		/// </summary>
		/// <param name="code">a byte code to be checked.</param>
		/// <returns>
		/// 
		/// <see langword="true"/>
		/// if
		/// <paramref name="code"/>
		/// could be decoded.
		/// </returns>
		public virtual bool CanDecode(int code)
		{
			return codeToUnicode[code] > -1;
		}

		protected internal virtual void FillCustomEncoding()
		{
			differences = new String[256];
			StringTokenizer tok = new StringTokenizer(baseEncoding.Substring(1), " ,\t\n\r\f"
				);
			if (tok.NextToken().Equals("full"))
			{
				while (tok.HasMoreTokens())
				{
					String order = tok.NextToken();
					String name = tok.NextToken();
					char uni = (char)System.Convert.ToInt32(tok.NextToken(), 16);
					int uniName = AdobeGlyphList.NameToUnicode(name);
					int orderK;
					if (order.StartsWith("'"))
					{
						orderK = order[1];
					}
					else
					{
						orderK = System.Convert.ToInt32(order);
					}
					orderK %= 256;
					unicodeToCode.Put(uni, orderK);
					codeToUnicode[orderK] = (int)uni;
					differences[orderK] = name;
					unicodeDifferences.Put(uni, uniName != null ? uniName : -1);
				}
			}
			else
			{
				int k = 0;
				if (tok.HasMoreTokens())
				{
					k = System.Convert.ToInt32(tok.NextToken());
				}
				while (tok.HasMoreTokens() && k < 256)
				{
					String hex = tok.NextToken();
					int uni = System.Convert.ToInt32(hex, 16) % 0x10000;
					String name = AdobeGlyphList.UnicodeToName(uni);
					if (name == null)
					{
						name = "uni" + hex;
					}
					unicodeToCode.Put(uni, k);
					codeToUnicode[k] = uni;
					differences[k] = name;
					unicodeDifferences.Put(uni, uni);
					k++;
				}
			}
			for (int k_1 = 0; k_1 < 256; k_1++)
			{
				if (differences[k_1] == null)
				{
					differences[k_1] = FontConstants.notdef;
				}
			}
		}

		protected internal virtual void FillNamedEncoding()
		{
			PdfEncodings.ConvertToBytes(" ", baseEncoding);
			// check if the encoding exists
			bool stdEncoding = PdfEncodings.WINANSI.Equals(baseEncoding) || PdfEncodings.MACROMAN
				.Equals(baseEncoding);
			if (!stdEncoding && differences == null)
			{
				differences = new String[256];
			}
			byte[] b = new byte[256];
			for (int k = 0; k < 256; ++k)
			{
				b[k] = unchecked((byte)k);
			}
			String str = PdfEncodings.ConvertToString(b, baseEncoding);
			char[] encoded = str.ToCharArray();
			for (int ch = 0; ch < 256; ++ch)
			{
				char uni = encoded[ch];
				String name = AdobeGlyphList.UnicodeToName(uni);
				if (name == null)
				{
					name = FontConstants.notdef;
				}
				else
				{
					unicodeToCode.Put(uni, ch);
					codeToUnicode[ch] = (int)uni;
					unicodeDifferences.Put(uni, uni);
				}
				if (differences != null)
				{
					differences[ch] = name;
				}
			}
		}

		protected internal virtual void FillStandardEncoding()
		{
			int[] encoded = PdfEncodings.standardEncoding;
			for (int ch = 0; ch < 256; ++ch)
			{
				int uni = encoded[ch];
				String name = AdobeGlyphList.UnicodeToName(uni);
				if (name == null)
				{
					name = FontConstants.notdef;
				}
				else
				{
					unicodeToCode.Put(uni, ch);
					codeToUnicode[ch] = uni;
					unicodeDifferences.Put(uni, uni);
				}
				if (differences != null)
				{
					differences[ch] = name;
				}
			}
		}

		/// <summary>Normalize the encoding names.</summary>
		/// <remarks>
		/// Normalize the encoding names. "winansi" is changed to "Cp1252" and
		/// "macroman" is changed to "MacRoman".
		/// </remarks>
		/// <param name="enc">the encoding to be normalized</param>
		/// <returns>the normalized encoding</returns>
		protected internal static String NormalizeEncoding(String enc)
		{
			String tmp = enc == null ? "" : enc.ToLower();
			switch (tmp)
			{
				case "":
				case "winansi":
				case "winansiencoding":
				{
					return PdfEncodings.WINANSI;
				}

				case "macroman":
				case "macromanencoding":
				{
					return PdfEncodings.MACROMAN;
				}

				default:
				{
					return enc;
				}
			}
		}
	}
}