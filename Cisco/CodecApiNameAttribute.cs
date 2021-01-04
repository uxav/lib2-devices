 
using System;

namespace UX.Lib2.Devices.Cisco
{
    public class CodecApiNameAttribute : Attribute
    {
        public CodecApiNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}