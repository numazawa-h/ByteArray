using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ByteArrayTest
{
    internal class Program
    {
                static void Main(string[] args)
        {
            Byte[] dat = new Byte[5];
            test(dat);
            _dat[0] = 0xff;
            if (dat[0] == 0)
            {
                Console.WriteLine("OK");
            }

        }

        static Byte[] _dat;
        static void test(Byte[] dat)
        {
            _dat = dat;
        }
    }
}
