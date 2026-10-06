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
            Assert.AreEqual("0000000000", ba.to_hex());     // Expand()はゼロ埋め
            ba = new ByteArray().Expand(4).Fill();
            Assert.AreEqual("FFFFFFFF", ba.to_hex());       // Fill() は 0xFF埋め
            ba = new ByteArray().Expand(4).Fill(0xfe);
            Assert.AreEqual("FEFEFEFE", ba.to_hex());       // Fill() は 埋める値を指定できる
            ba = ba.Expand(2);
            Assert.AreEqual("FEFEFEFE0000", ba.to_hex());   // Expand()は元のデータは変更せずに拡張する
            ba = ba.Expand(1,0xaa);
            Assert.AreEqual("FEFEFEFE0000AA", ba.to_hex()); // Expand()も拡張部分を埋める値を指定できる

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
            ba = new ByteArray(null);
            Assert.AreEqual("", ba.to_hex());               // null なら 0バイト

            // Copy()
            ba = new ByteArray().Expand(5);
            ByteArray ba01 = new ByteArray().Expand(2).Fill(0x11);
            ba.Fill().Copy(null as byte[]);  // 元データがなければ更新なし
            Assert.AreEqual("FFFFFFFFFF", ba.to_hex());
            ba.Fill().Copy(new ByteArray()); // 元データがなければ更新なし
            Assert.AreEqual("FFFFFFFFFF", ba.to_hex());
            ba.Fill().Copy(ba01);           // 元データを先頭にコピー
            Assert.AreEqual("1111FFFFFF", ba.to_hex());
            ba.Fill().Copy(ba01, 1);        // オフセット指定
            Assert.AreEqual("FF1111FFFF", ba.to_hex());
            ba.Fill().Copy(ba01, 5);        // オフセットが自身データを超えていれば更新なし
            Assert.AreEqual("FFFFFFFFFF", ba.to_hex());

            ba.Fill().Copy(ba01, 1, 2);     // データ長指定(元データ長と同じなら指定しなくても同じ結果になる)
            Assert.AreEqual("FF1111FFFF", ba.to_hex());
            ba.Fill().Copy(ba01, 0, 3);     // データ長が元データより長ければ、足りない部分を0x00埋め
            Assert.AreEqual("111100FFFF", ba.to_hex());
            ba.Fill().Copy(ba01, 2, 3);     // データ長が元データより長ければ、足りない部分を0x00埋め
            Assert.AreEqual("FFFF111100", ba.to_hex());
            ba.Fill().Copy(ba01, 3, 3);     // 自身データを超えた部分は無視
            Assert.AreEqual("FFFFFF1111", ba.to_hex());
            ba.Fill().Copy(ba01, 4, 3);     // 自身データを超えた部分は無視
            Assert.AreEqual("FFFFFFFF11", ba.to_hex());
            ba.Fill().Copy(ba01, 5, 3);     // コピーすべきデータがなければ更新なし
            Assert.AreEqual("FFFFFFFFFF", ba.to_hex());

            ba.Fill().Copy(ba01, 5, -2);    // データ長がマイナスならオフセットより前が対象
            Assert.AreEqual("FFFFFF1111", ba.to_hex());
            ba.Fill().Copy(ba01, 4, -2);    // データ長がマイナスならオフセットより前が対象
            Assert.AreEqual("FFFF1111FF", ba.to_hex());
            ba.Fill().Copy(ba01, 3, -2);    // データ長がマイナスならオフセットより前が対象
            Assert.AreEqual("FF1111FFFF", ba.to_hex());
            ba.Fill().Copy(ba01, 2, -2);    // データ長がマイナスならオフセットより前が対象
            Assert.AreEqual("1111FFFFFF", ba.to_hex());
            ba.Fill().Copy(ba01, 1, -2);    // データ長がマイナスならオフセットより前が対象
            Assert.AreEqual("11FFFFFFFF", ba.to_hex());
            ba.Fill().Copy(ba01, 0, -2);    // ★オフセットがゼロでデータ長がマイナスなら末尾からの相対
            Assert.AreEqual("FFFFFF1111", ba.to_hex());

            ba.Fill().Copy(ba01, 5, -3);    // データ長がマイナスの時、元データより長ければ右寄せ
            Assert.AreEqual("FFFF001111", ba.to_hex());
            ba.Fill().Copy(ba01, 6, -3);    // データ長がマイナスの時、元データより長ければ右寄せ
            Assert.AreEqual("FFFFFF0011", ba.to_hex());
            ba.Fill().Copy(ba01, 7, -3);    // データ長がマイナスの時、元データより長ければ右寄せ
            Assert.AreEqual("FFFFFFFF00", ba.to_hex());
            ba.Fill().Copy(ba01, 8, -3);    // コピーすべきデータがなければ更新なし
            Assert.AreEqual("FFFFFFFFFF", ba.to_hex());

            ba.Fill().Copy(ba01, -2);       // オフセットがマイナスなら末尾からの相対
            Assert.AreEqual("FFFFFF1111", ba.to_hex());
            ba.Fill().Copy(ba01, -3, 3);    // データ長が元データより長ければ、左寄せ
            Assert.AreEqual("FFFF111100", ba.to_hex());

            ba.Fill().Copy(ba01, -1, -2);   // 両方がマイナスなら末尾相対のオフセットよりさらに前が対象
            Assert.AreEqual("FFFF1111FF", ba.to_hex());
            ba.Fill().Copy(ba01, -1, -3);   // データ長が元データより長ければ、右寄せ
            Assert.AreEqual("FF001111FF", ba.to_hex());

            ByteArray ba02 = ByteArray.ParseHex("1234");
            ba.Fill().Copy(ba02,-6, 3);     // 末尾相対が先頭より前ならはみ出る部分は無視
            Assert.AreEqual("3400FFFFFF", ba.to_hex());
            ba.Fill().Copy(ba02,-3,-3);     // 末尾相対とデータ長マイナスで先頭より前ならはみ出る部分は無視
            Assert.AreEqual("1234FFFFFF", ba.to_hex());
            ba.Fill().Copy(ba02,-5,-2);     // 末尾相対とデータ長マイナスでコピーすべきデータがなければ更新なし
            Assert.AreEqual("FFFFFFFFFF", ba.to_hex());



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
            Assert.AreEqual("BC-DE-F0-12", ba1.Take(5, 4).to_hex("-"));
            Assert.AreEqual("12345678 9ABCDEF0 12345678 9ABCDEF0", ba1.Take(0, 16).to_hex(" ", 4));
            Assert.AreEqual("1 2 3 4 a b c d A B C D", ba1.Take(16, 12).to_text_ascii(" "));
            Assert.AreEqual("1234,abcd,ABCD", ba1.Take(16, 12).to_text_ascii(",", 4));
            Assert.AreEqual("1234abcd", ba1.Take(16, 8).to_text_utf8());
            Assert.AreEqual("あいうえお", ba1.Take(28, 10).to_text_sjis());
            Assert.AreEqual("82A082A282A482A682A8", ba1.Take(28, 10).to_hex());

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
            ba.Fill(0x12, 4, 2);
            Assert.AreEqual("FFFFFFFF1212FFFF", ba.to_hex());   // オフセットと長さ指定
            ba.Fill(0x12, 4 );
            Assert.AreEqual("FFFFFFFF12121212", ba.to_hex());   // 長さを省略(0になる)したら末尾まで
            ba.Fill(0xaa, 0,-3);
            Assert.AreEqual("FFFFFFFF12AAAAAA", ba.to_hex());   // オフセット0で長さががマイナスなら末尾
            ba.Fill(0xaa, 3,-2);
            Assert.AreEqual("FFAAAAFF12AAAAAA", ba.to_hex());   // オフセットプラスで長さマイナスならオフセットより前の部分が対象
            ba.Fill(0x00,10,-3);
            Assert.AreEqual("FFAAAAFF12AAAA00", ba.to_hex());   // 範囲外は無視(エラーにはしない)
            ba.Fill(0x00,-4, 2);
            Assert.AreEqual("FFAAAAFF0000AA00", ba.to_hex());   // オフセットがマイナスなら末尾起点
            ba.Fill(0xBB,-2);
            Assert.AreEqual("FFAAAAFF0000BBBB", ba.to_hex());   // 長さを省略(0になる)したら末尾まで
            ba.Fill(0xFF,-3, 5);
            Assert.AreEqual("FFAAAAFF00FFFFFF", ba.to_hex());   // 範囲外は無視
            ba.Fill(0xDD, 8, 3);
            Assert.AreEqual("FFAAAAFF00FFFFFF", ba.to_hex());   // 全て範囲外なら変化なし(エラーにはしない)

            // Take()
            ba = ByteArray.ParseHex("1234567890abcdef");
            Assert.AreEqual("12", ba.Take().to_hex());              // Take()で先頭1バイト取得
            Assert.AreEqual("1234", ba.Take(2).to_hex());           // Take(2)で先頭2バイト取得
            Assert.AreEqual("1234567890ABCDEF0000", ba.Take(10).to_hex());  // 長さが元データより長ければゼロ埋め
            Assert.AreEqual("CDEF", ba.Take(-2).to_hex());          // マイナスなら末尾バイト取得
            Assert.AreEqual("00001234567890ABCDEF", ba.Take(-10).to_hex()); // 長さが元データより長ければゼロ埋め
            Assert.AreEqual("567890ABCD", ba.Take( 2, 5).to_hex()); // Take(ofs, len)でオフセットと長さ指定
            Assert.AreEqual("7890",       ba.Take( 5,-2).to_hex()); // 長さがマイナスなら、オフセットより前の部分を取得
            Assert.AreEqual("0000001234", ba.Take( 2,-5).to_hex()); // 長さが元データより長ければゼロ埋め
            Assert.AreEqual("ABCDEF0000", ba.Take( 5, 5).to_hex()); // 長さが元データより長ければゼロ埋め
            Assert.AreEqual("7890ABCDEF", ba.Take( 0,-5).to_hex()); // 長さがマイナスでオフセットが 0なら、末尾バイト取得
            Assert.AreEqual("ABCD",       ba.Take(-3, 2).to_hex()); // オフセットがマイナスなら、末尾からの相対
            Assert.AreEqual("34567890AB", ba.Take(-2,-5).to_hex()); // 両方マイナスなら、末尾2バイトより前の5バイトを取得
            Assert.AreEqual("90ABCDEF", ba.Take(4, 0).to_hex());            // 長さが 0なら末尾までが対象
            Assert.AreEqual("001234567890ABCDEF", ba.Take(-9, 0).to_hex()); // オフセットがマイナスなら末尾からの相対
            Assert.AreEqual("001234567890ABCDEF00", ba.Take(-9, 10).to_hex()); // 左右ゼロ埋め
            Assert.AreEqual("", ba.Take(8, 0).to_hex());                    // オフセットが末尾以降なら 0バイト
            Assert.AreEqual("1234567890ABCDEF", ba.Take(0, 0).to_hex());    // 両方 0なら完全コピー(Clone()と同じ)
            Assert.AreEqual("1234567890ABCDEF", ba.Take(0).to_hex());       // Take(0)はTake(0, 0)と同じ

            // Shift()
            ba = ByteArray.ParseHex("1234567890abcdef");
            Assert.AreEqual("1234567890ABCDEF", ba.ShiftLeft(0).to_hex());    // 引数ゼロなら変化なし
            Assert.AreEqual("34567890ABCDEF00", ba.ShiftLeft().to_hex());     // 引数なしなら1バイトシフト
            Assert.AreEqual("7890ABCDEF000000", ba.ShiftLeft(2).to_hex());    // 2バイトシフト
            Assert.AreEqual("0000007890ABCDEF", ba.ShiftLeft(-3).to_hex());   // マイナスなら逆方向にシフト
            Assert.AreEqual("0000000000000000", ba.ShiftLeft(8).to_hex());    // データ長以上は全クリア
            ba = ByteArray.ParseHex("1234567890abcdef");
            Assert.AreEqual("1234567890ABCDEF", ba.ShiftRight(0).to_hex());   // 引数ゼロなら変化なし
            Assert.AreEqual("001234567890ABCD", ba.ShiftRight().to_hex());    // 引数なしなら1バイトシフト
            Assert.AreEqual("0000001234567890", ba.ShiftRight(2).to_hex());   // 2バイトシフト
            Assert.AreEqual("1234567890000000", ba.ShiftRight(-3).to_hex());  // マイナスなら逆方向にシフト
            Assert.AreEqual("0000000000000000", ba.ShiftRight(8).to_hex());   // データ長以上は全クリア

        }
    }
}
