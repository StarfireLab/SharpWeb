using System;
using System.Collections.Generic;
using System.Text;

namespace SharpWeb.Browsers.Firefox
{
	//adapted from https://github.com/djhohnstein/SharpWeb/blob/master/Firefox/Cryptography/Asn1Der.cs#L8-L161
	public class Asn1Der
	{
		public Asn1DerObject Parse(byte[] dataToParse)
		{
			Asn1DerObject asn1DerObject = new Asn1DerObject();
			int i = 0;
			while (i < dataToParse.Length)
			{
				Asn1Der.Type type = (Asn1Der.Type)dataToParse[i];
				Asn1Der.Type type2 = type;
				switch (type2)
				{
					case Asn1Der.Type.Integer:
						{
							asn1DerObject.objects.Add(new Asn1DerObject
							{
								Type = Asn1Der.Type.Integer,
								Lenght = (int)dataToParse[i + 1]
							});
							byte[] array = new byte[(int)dataToParse[i + 1]];
							int length = (i + 2 + (int)dataToParse[i + 1] > dataToParse.Length) ? (dataToParse.Length - (i + 2)) : ((int)dataToParse[i + 1]);
							Array.Copy(dataToParse, i + 2, array, 0, length);
							Asn1DerObject[] array2 = asn1DerObject.objects.ToArray();
							asn1DerObject.objects[array2.Length - 1].Data = array;
							i = i + 1 + asn1DerObject.objects[array2.Length - 1].Lenght;
							break;
						}
					case Asn1Der.Type.BitString:
					case Asn1Der.Type.Null:
						break;
					case Asn1Der.Type.OctetString:
						{
							asn1DerObject.objects.Add(new Asn1DerObject
							{
								Type = Asn1Der.Type.OctetString,
								Lenght = (int)dataToParse[i + 1]
							});
							byte[] array = new byte[(int)dataToParse[i + 1]];
							int length = (i + 2 + (int)dataToParse[i + 1] > dataToParse.Length) ? (dataToParse.Length - (i + 2)) : ((int)dataToParse[i + 1]);
							Array.Copy(dataToParse, i + 2, array, 0, length);
							Asn1DerObject[] array3 = asn1DerObject.objects.ToArray();
							asn1DerObject.objects[array3.Length - 1].Data = array;
							i = i + 1 + asn1DerObject.objects[array3.Length - 1].Lenght;
							break;
						}
					case Asn1Der.Type.ObjectIdentifier:
						{
							asn1DerObject.objects.Add(new Asn1DerObject
							{
								Type = Asn1Der.Type.ObjectIdentifier,
								Lenght = (int)dataToParse[i + 1]
							});
							byte[] array = new byte[(int)dataToParse[i + 1]];
							int length = (i + 2 + (int)dataToParse[i + 1] > dataToParse.Length) ? (dataToParse.Length - (i + 2)) : ((int)dataToParse[i + 1]);
							Array.Copy(dataToParse, i + 2, array, 0, length);
							Asn1DerObject[] array4 = asn1DerObject.objects.ToArray();
							asn1DerObject.objects[array4.Length - 1].Data = array;
							i = i + 1 + asn1DerObject.objects[array4.Length - 1].Lenght;
							break;
						}
					default:
						if (type2 == Asn1Der.Type.Sequence)
						{
							bool flag = asn1DerObject.Lenght == 0;
							byte[] array;
							if (flag)
							{
								asn1DerObject.Type = Asn1Der.Type.Sequence;
								asn1DerObject.Lenght = dataToParse.Length - (i + 2);
								array = new byte[asn1DerObject.Lenght];
							}
							else
							{
								asn1DerObject.objects.Add(new Asn1DerObject
								{
									Type = Asn1Der.Type.Sequence,
									Lenght = (int)dataToParse[i + 1]
								});
								array = new byte[(int)dataToParse[i + 1]];
							}
							int length = (array.Length > dataToParse.Length - (i + 2)) ? (dataToParse.Length - (i + 2)) : array.Length;
							Array.Copy(dataToParse, i + 2, array, 0, length);
							asn1DerObject.objects.Add(this.Parse(array));
							i = i + 1 + (int)dataToParse[i + 1];
						}
						break;
				}
			IL_2D1:
				i++;
				continue;
				goto IL_2D1;
			}
			return asn1DerObject;
		}

		public static Dictionary<string, string> oidValues = new Dictionary<string, string>
		{
			{
				"2A864886F70D010C050103",
				"1.2.840.113549.1.12.5.1.3 pbeWithSha1AndTripleDES-CBC"
			},
			{
				"2A864886F70D0307",
				"1.2.840.113549.3.7 des-ede3-cbc"
			},
			{
				"2A864886F70D010101",
				"1.2.840.113549.1.1.1 pkcs-1"
			},
			{
				"2A864886F70D01050D",
				"1.2.840.113549.1.5.13 pkcs5 pbes2"
			},
			{
				"2A864886F70D01050C",
				"1.2.840.113549.1.5.12 pkcs5 PBKDF2"
			},
			{
				"2A864886F70D0209",
				"1.2.840.113549.2.9 hmacWithSHA256"
			},
			{
				"60864801650304012A",
				"2.16.840.1.101.3.4.1.42 aes256-CBC"
			}
		};

		public enum Type
		{
			Sequence = 0x30,
			Integer = 0x02,
			BitString = 0x03,
			OctetString = 0x04,
			Null = 0x05,
			ObjectIdentifier = 0x06
		}
	}

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
