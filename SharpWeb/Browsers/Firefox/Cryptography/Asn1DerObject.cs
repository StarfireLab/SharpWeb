using System.Collections.Generic;
using System.Text;

namespace SharpWeb.Browsers.Firefox
{
	public class Asn1DerObject
	{
		public Asn1Der.Type Type { get; set; }

		public int Lenght { get; set; }

		public List<Asn1DerObject> objects { get; set; }

		public byte[] Data { get; set; }

		public Asn1DerObject()
		{
			this.objects = new List<Asn1DerObject>();
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			Asn1Der.Type type = this.Type;
			Asn1Der.Type type2 = type;
			switch (type2)
			{
				case Asn1Der.Type.Integer:
					{
						foreach (byte b in this.Data)
						{
							stringBuilder2.AppendFormat("{0:X2}", b);
						}
						StringBuilder stringBuilder3 = stringBuilder;
						string str = "\tINTEGER ";
						StringBuilder stringBuilder4 = stringBuilder2;
						stringBuilder3.AppendLine(str + ((stringBuilder4 != null) ? stringBuilder4.ToString() : null));
						stringBuilder2 = new StringBuilder();
						break;
					}
				case Asn1Der.Type.BitString:
				case Asn1Der.Type.Null:
					break;
				case Asn1Der.Type.OctetString:
					foreach (byte b2 in this.Data)
					{
						stringBuilder2.AppendFormat("{0:X2}", b2);
					}
					stringBuilder.AppendLine("\tOCTETSTRING " + stringBuilder2.ToString());
					stringBuilder2 = new StringBuilder();
					break;
				case Asn1Der.Type.ObjectIdentifier:
					foreach (byte b3 in this.Data)
					{
						stringBuilder2.AppendFormat("{0:X2}", b3);
					}
					foreach (KeyValuePair<string, string> keyValuePair in Asn1Der.oidValues)
					{
						bool flag = stringBuilder2.ToString().Equals(keyValuePair.Key);
						if (flag)
						{
							stringBuilder.AppendLine("\tOBJECTIDENTIFIER " + keyValuePair.Value);
						}
					}
					stringBuilder2 = new StringBuilder();
					break;
				default:
					if (type2 == Asn1Der.Type.Sequence)
					{
						stringBuilder.AppendLine("SEQUENCE {");
					}
					break;
			}
			foreach (Asn1DerObject asn1DerObject in this.objects)
			{
				stringBuilder.Append(asn1DerObject.ToString());
			}
			bool flag2 = this.Type.Equals(Asn1Der.Type.Sequence);
			if (flag2)
			{
				stringBuilder.AppendLine("\n}");
			}
			return stringBuilder.ToString();
		}
	}
}
