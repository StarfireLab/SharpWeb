using System;

namespace SharpWeb.Utilities
{
    class Enums
    {
        public enum OUTPUT_TYPE
        {
            SUCCESS = '+',
            VERBOSE = '*',
            ERROR = '!',
            Fail = '-',
            Normal = ' '
        };

        public enum VAULT_ELEMENT_TYPE : Int32
        {
            Undefined = -1,
            Boolean = 0,
            Short = 1,
            UnsignedShort = 2,
            Int = 3,
            UnsignedInt = 4,
            Double = 5,
            Guid = 6,
            String = 7,
            ByteArray = 8,
            TimeStamp = 9,
            ProtectedArray = 10,
            Attribute = 11,
            Sid = 12,
            Last = 13
        }

        public enum VAULT_SCHEMA_ELEMENT_ID : Int32
        {
            Illegal = 0,
            Resource = 1,
            Identity = 2,
            Authenticator = 3,
            Tag = 4,
            PackageSid = 5,
            AppStart = 100,
            AppEnd = 10000
        }
    }
}
