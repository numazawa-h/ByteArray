using NCommonUtility;
using System.Collections;
using System.Text;

namespace ByteArrayTestProject
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public void TestMethod1()
        {
            ByteArray ba;

            // 空コンストラクタ
            ba = new ByteArray();
            Assert.AreEqual(0, ba.Length());

            // 初期値クリア
            ba = new ByteArray().Expand(5);
            Assert.AreEqual(5, ba.Length());
            Assert.AreEqual("0000000000", ba.to_hex());
            ba = new ByteArray().Expand(4, 0xff);
            Assert.AreEqual("FFFFFFFF", ba.to_hex());

            // 数値コンストラクタ
            ba = new ByteArray((Byte)12);
            Assert.AreEqual("0C", ba.to_hex());
            ba = new ByteArray((UInt16)0x1234);
            Assert.AreEqual("1234", ba.to_hex());
            ba = new ByteArray((UInt32)0x12345678);
            Assert.AreEqual("12345678", ba.to_hex());
            ba = new ByteArray((UInt64)0x1234567812345678);
            Assert.AreEqual("1234567812345678", ba.to_hex());

            // 日時コンストラクタ
            ba = new ByteArray(new DateTime(2021, 4, 15, 23, 59, 59, 999), "yyyyMMddHHmmss");
            Assert.AreEqual("20210415235959", ba.to_hex());

            // 文字列コンストラクタ
            ba = new ByteArray("テスト");
            Assert.AreEqual("テスト", ba.to_text());
            Assert.AreEqual("C630B930C830", ba.to_hex());
            ba = new ByteArray("1234abcdABCD", Encoding.UTF8);
            Assert.AreEqual("313233346162636441424344", ba.to_hex());

            // Byte[]コンストラクタ
            Byte[] dat = { 0x01, 0x23, 0x45, 0x67};
            ba = new ByteArray(dat);
            Assert.AreEqual("01234567", ba.to_hex());
            dat[1] = 0xfe;
            Assert.AreEqual("01FE4567", ba.to_hex());       // ByteArray(dat)は参照で生成しているのでByteArrayの中身も変わる
            ba = new ByteArray(dat).Clone();                // Clone()すればByteArrayの中身は変わらなくなる
            dat[1] = 0x23;
            Assert.AreEqual("01FE4567", ba.to_hex());
            ba = new ByteArray(dat, 0);                     // ByteArray(dat, 0)は引数をコピーしているのでClone()と同じになる
            Assert.AreEqual("01234567", ba.to_hex());
            dat[1] = 0xfe;
            Assert.AreEqual("01234567", ba.to_hex());
            ba = new ByteArray(dat, 2);
            Assert.AreEqual("01FE", ba.to_hex());
            ba = new ByteArray(dat, 4);
            Assert.AreEqual("01FE4567", ba.to_hex());
            ba = new ByteArray(dat, 6);
            Assert.AreEqual("01FE45670000", ba.to_hex());   // 元のデータより長ければ0x00で埋める
            Exception ex;
            ex = Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                // 長さがマイナスなら例外
                ba = new ByteArray(dat, -4);
            });
            ex = Assert.ThrowsException<NullReferenceException>(() =>
            {
                ba = new ByteArray(null, 0);
            });

            // コピーコンストラクタ
            ByteArray src = new ByteArray("1234abcdABCD", Encoding.UTF8);
            ba = new ByteArray(src);            // src.Clone() と同じ          
            Assert.AreEqual("313233346162636441424344", ba.to_hex());
            ba = new ByteArray(src, 8);         // オフセットのみなのでオフセット以降全部が対象
            Assert.AreEqual("41424344", ba.to_hex());
            ba = new ByteArray(src, 8, 16);     // オフセット8バイト目から16バイト(足りない部分を0x00埋め)
            Assert.AreEqual("41424344000000000000000000000000", ba.to_hex());
            ba = new ByteArray(src, 0, -16);    // 長さがマイナスでオフセット0なので、末尾から16バイト(足りない部分を0x00埋め)
            Assert.AreEqual("00000000313233346162636441424344", ba.to_hex());
            ba = new ByteArray(src, 0, -4);     // 長さがマイナスでオフセット0なので、末尾から4バイト
            Assert.AreEqual("41424344", ba.to_hex());
            ba = new ByteArray(src, 8, -4);     // 長さがマイナスでオフセットがプラスなので、オフセットから前の4バイト
            Assert.AreEqual("61626364", ba.to_hex());
            ba = new ByteArray(src, -2, -4);    // 長さがマイナスでオフセットもマイナスなので、末尾2バイトより前の4バイト
            Assert.AreEqual("63644142", ba.to_hex());
            ba = new ByteArray(src, -4, 8);     // オフセットがマイナスなので、先頭4バイトは0x00。全体で8バイト
            Assert.AreEqual("0000000031323334", ba.to_hex());

            // Copy()
            ByteArray ba00 = new ByteArray().Expand(5, 0xff);
            ByteArray ba01 = new ByteArray().Expand(2, 0x11);
            ba00.Copy(ba01, 5);         // コピー先オフセットが自身データを超えていれば更新なし
            Assert.AreEqual("FFFFFFFFFF", ba00.to_hex());
            ba00.Copy(ba01, 1);         // コピー先オフセットのみ指定なら、元データ全体をコピーする
            Assert.AreEqual("FF1111FFFF", ba00.to_hex());
            ba00.Copy(ba01, 4, 2);      // コピー先オフセット+コピー長が自身データを超えていれば、自身データの長さまでをコピーする
            Assert.AreEqual("FF1111FF11", ba00.to_hex());
            ba00.Copy(ba01, 0, 3);      // コピー長が元データより長ければ、足りない部分を0x00埋め
            Assert.AreEqual("111100FF11", ba00.to_hex());

            // ParseHex()
            ByteArray ba1 = ByteArray.ParseHex("[ 1234 5678 9ABC def0 ]");  // 16進文字([0-9,a-f,A-F])以外は無視する
            Assert.AreEqual("123456789ABCDEF0", ba1.to_hex());
            ByteArray ba2 = ByteArray.ParseHex("[  31323334 61626364 41424344]");
            Assert.AreEqual("1234abcdABCD", ba2.to_text_ascii());

            // 文字列コンストラクタ
            Encoding sjis = Encoding.GetEncoding("Shift_JIS");
            ByteArray ba3 = new ByteArray("あいうえお", sjis);
            Assert.AreEqual("82A082A282A482A682A8", ba3.to_hex());
            Assert.AreEqual("あいうえお", ba3.to_text_sjis());

            // Append()
            ba1.Append(ba1).Append(ba2).Append(ba3);
            Byte[] dat_append = [
                0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0,
                0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0,
                0x31, 0x32, 0x33, 0x34, 0x61, 0x62, 0x63, 0x64, 0x41, 0x42, 0x43, 0x44,
                0x82, 0xa0, 0x82, 0xa2, 0x82, 0xa4, 0x82, 0xa6, 0x82, 0xa8,
            ]; 
            CollectionAssert.AreEqual(dat_append, ba1.GetData());

            // 文字列への変換
            Assert.AreEqual("BC-DE-F0-12", ba1.to_hex(5, 4, "-"));
            Assert.AreEqual("1 2 3 4 a b c d A B C D", ba1.to_text_ascii(16, 12, " "));
            Assert.AreEqual("1234abcd", ba1.to_text_utf8(16,8));
            Assert.AreEqual("あいうえお", ba1.to_text_sjis(28,10));
            Assert.AreEqual("82A082A282A482A682A8", ba1.to_hex(28, 10));

            // StrToByte
            ba = ByteArray.StrToByte("0123456789abcdEF");
            Assert.AreEqual("0123456789ABCDEF", ba.to_hex());   //16進文字([0-9,a-f,A-F])のみの文字列ならParseHex()と同じ
            ba = ByteArray.StrToByte("test01");
            Assert.AreEqual("746573743031", ba.to_hex());   //16進文字以外があるとEncoding.ASCIIで変換
            ba = ByteArray.StrToByte("ac100");
            Assert.AreEqual("AC1000", ba.to_hex());     //ASCIIのつもりでも16進文字だけになることがあるので注意
            ba = ByteArray.StrToByte("ac100 ");
            Assert.AreEqual("6163313030", ba.to_hex()); //最後にスペースを追加するとASCIIになる(最後のスペースは含まれない)


            // Fill(), Clear()
            ba = new ByteArray().Expand(8).Fill(0xfe);
            Assert.AreEqual("FEFEFEFEFEFEFEFE", ba.to_hex());
            ba.Clear();
            Assert.AreEqual("0000000000000000", ba.to_hex());   // Fill(0) と同じ
            ba.Fill();
            Assert.AreEqual("FFFFFFFFFFFFFFFF", ba.to_hex());   // defaultは 0xFF

            // Take()
            ba = ByteArray.ParseHex("1234567890abcdef");
            Assert.AreEqual("12", ba.Take().to_hex());      // Take()で先頭1バイト取得
            Assert.AreEqual("1234", ba.Take(2).to_hex());   // Take(2)で先頭2バイト取得
            Assert.AreEqual("1234567890ABCDEF0000", ba.Take(10).to_hex());   // 長さが元データより長ければゼロ埋め
            Assert.AreEqual("CDEF", ba.Take(-2).to_hex());   // マイナスなら末尾バイト取得
            Assert.AreEqual("00001234567890ABCDEF", ba.Take(-10).to_hex());   // 長さが元データより長ければゼロ埋め
            Assert.AreEqual("", ba.Take(0).to_hex());       // Take(0)で0バイトを返還

            // Shift()

            // Read()
        }
    }
}
