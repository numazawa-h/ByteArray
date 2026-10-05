using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NCommonUtility
{
    public class ByteArray
    {
        private byte[] _dat;

        public ByteArray()
        {
            _dat = System.Array.Empty<byte>();
        }
        public ByteArray(Byte num)
        {
            _dat = new Byte[1];
            _dat[0] = num;
        }
        public ByteArray(UInt16 num)
        {
            _dat = BitConverter.GetBytes(num);
            Array.Reverse(_dat);
        }
        public ByteArray(UInt32 num)
        {
            _dat = BitConverter.GetBytes(num);
            Array.Reverse(_dat);
        }
        public ByteArray(UInt64 num)
        {
            _dat = BitConverter.GetBytes(num);
            Array.Reverse(_dat);
        }

        public ByteArray(DateTime dt, string fmt)
        {
            string hex = dt.ToString(fmt);
            _dat = ByteArray.ParseHex(hex)._dat;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <remarks>
        /// 引数のdatはコピーされずにそのまま参照として格納されるので注意！
        /// 生成後、引数のdatを他で使用するのなら、Clone()を使用すること。
        /// 　new ByteArray(dat).Clone()
        /// </remarks>
        /// <param name="dat">元データ(null ならEmptyで生成)</param>
        public ByteArray(byte[] dat)
        {
            if (dat == null)
            {
                _dat = System.Array.Empty<byte>();
            }
            else
            {
                _dat = dat;
            }
        }

        /// <summary>
        /// 指定された文字コードで文字列を変換するコンストラクター
        /// </summary>
        public ByteArray(string text, Encoding enc=null )
        {
            if(enc==null)
            {
                // C#の文字列は、UTF16(リトルエンディアン)なので、デフォルトはUnicode
                enc = Encoding.Unicode;
            }
            _dat = enc.GetBytes(text);
        }

        public int Length()
        {
            return _dat.Length;
        }

        public ByteArray Clone()
        {
            Byte[] dst = new Byte[_dat.Length];
            Buffer.BlockCopy(_dat, 0, dst, 0, _dat.Length);
            return new ByteArray(dst);
        }

        public void Clear()
        {
            Fill(0);
        }

        public ByteArray Fill(byte data = 0xff)
        {
            for (int idx = 0; idx < _dat.Length; idx++)
            {
                _dat[idx] = data;
            }

            return this;
        }

        public ByteArray Take(int cnt = 1)
        {
            return Take(0, cnt);
        }

        public ByteArray Take(int ofs, int cnt)
        {
            // cnt が 0なら末尾までが対象
            if (cnt == 0)
            {
                cnt = _dat.Length - ofs;
                // ofs が末尾以降なら 0バイト
                if ( cnt <= 0)
                {
                    return new ByteArray();
                }
            }

            int src_ofs = ofs;
            int dst_ofs = 0;

            // cnt がマイナスなら起点を終点にする
            if (cnt < 0)
            {
                if (src_ofs <= 0)
                {
                    // src_ofsもマイナスなら起点を末尾から src_ofs分ずらした位置にする
                    src_ofs = _dat.Length + src_ofs;
                }
                // 起点を終点にする(起点をcnt分前にずらす)
                cnt = -cnt;
                src_ofs = src_ofs - cnt;
            }

            byte[] dst = new byte[cnt];

            // 起点がマイナスなら先頭ゼロ埋め
            if (src_ofs < 0)
            {
                dst_ofs = -src_ofs;
                src_ofs = 0;
                cnt -= dst_ofs;
            }
            // コピーする長さが元のデータを超えていたら元のデータ長にあわせる
            if ((src_ofs+cnt) > _dat.Length)
            {
                cnt = _dat.Length - src_ofs;
            }
            Buffer.BlockCopy(_dat, src_ofs, dst, dst_ofs, cnt);
            return new ByteArray(dst);
        }

        /// <summary>
        /// データの拡張
        /// </summary>
        /// <param name="size">拡張するバイト数</param>
        /// <param name="fill_value">拡張した部分を埋める値</param>
        /// <returns>更新後のthis</returns>
        public ByteArray Expand(int size, byte fill_value=0)
        {
            byte[] buf = new byte[_dat.Length + size];
            Buffer.BlockCopy(_dat, 0, buf, 0, _dat.Length);
            if (fill_value != 0)
            {
                // .NET FrameworkではArray.Fillを使えないのでループで対応
                for (int i = 0, ofs = _dat.Length; i < size; i++, ofs++)
                {
                    buf[ofs] = fill_value;
                }
            }
            _dat = buf;
            return this;
        }

        /// <summary>
        /// 末尾にデータを追加する
        /// </summary>
        /// <param name="other">追加するデータ</param>
        /// <returns>更新後のthis</returns>
        public ByteArray Append(string other)
        {
            Append(new ByteArray(other));
            return this;
        }
        public ByteArray Append(ByteArray other)
        {
            Append(other._dat);
            return this;
        }
        public ByteArray Append(byte[] other)
        {
            byte[] buf = new byte[_dat.Length + other.Length];
            Buffer.BlockCopy(_dat, 0, buf, 0, _dat.Length);
            Buffer.BlockCopy(other, 0, buf, _dat.Length, other.Length);
            _dat = buf;
            return this;
        }

        /// <summary>
        /// データをコピーする
        /// </summary>
        /// <param name="other">コピー元データ</param>
        /// <param name="ofs">コピー先位置（自身データのオフセット）</param>
        /// <param name="len">コピーする長さ</param>
        /// <returns>更新後のthis</returns>
        public ByteArray Copy(ByteArray other, int ofs = 0, int len = 0)
        {
            if(ofs >= _dat.Length)
            {
                // コピー先位置が自身データを超えていれば更新なし
                return this;
            }

            if (len == 0)
            {
                // コピーする長さが指定されなければ、コピー元データ全体をコピーする
                len = other.Length();
            }
            if((ofs + len) > _dat.Length)
            {
                // コピーする長さが自身データを超えていれば、自身データの長さまでをコピーする
                len = _dat.Length - ofs;
            }

            if (len > other.Length())
            {
                // コピーする長さがコピー元データを超えていれば、足りない部分を0x00で埋めてからコピー
                ByteArray ba = new ByteArray().Expand(len);
                Buffer.BlockCopy(other._dat, 0, ba._dat, 0, other.Length());
                Buffer.BlockCopy(ba._dat, 0, _dat, ofs, len);
            }
            else
            {
                // 通常コピー
                Buffer.BlockCopy(other._dat, 0, _dat, ofs, len);
            }
            return this;
        }

        public byte[] GetData()
        {
            if (_dat.Length == 0)
            {
                return System.Array.Empty<byte>();
            }
            byte[] buf = new byte[_dat.Length];
            Buffer.BlockCopy(_dat, 0, buf, 0, _dat.Length);

            return buf;
        }

        public override string ToString() 
        { 
            return to_hex();
        }

        public int to_int()
        {
            if (_dat.Length > 4 || _dat.Length < 1)
            {
                throw new Exception($"バイト長が{_dat.Length}なのでintに変換できません");
            }

            byte[] val = new byte[4];
            Buffer.BlockCopy(_dat, 0, val, 4 - _dat.Length, _dat.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(val);
            }

            return BitConverter.ToInt32(val, 0);
        }
        public long to_long()
        {
            if (_dat.Length > 8 || _dat.Length < 1)
            {
                throw new Exception($"バイト長が{_dat.Length}なのでlongに変換できません");
            }

            byte[] val = new byte[8];
            Buffer.BlockCopy(_dat, 0, val, 8 - _dat.Length, _dat.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(val);
            }

            return BitConverter.ToInt64(val, 0);
        }

        public DateTime to_dateTime()
        {
            DateTime val;
            string bcd = to_hex();
            switch (bcd.Length)
            {
                case 8:
                    val = DateTime.ParseExact(bcd, "yyyyMMdd", null);
                    break;
                case 12:
                    val = DateTime.ParseExact(bcd, "yyyyMMddHHmm", null);
                    break;
                case 14:
                    val = DateTime.ParseExact(bcd, "yyyyMMddHHmmss", null);
                    break;
                default:
                    val = DateTime.MinValue;
                    break;
            }

            return val;
        }
        public DateTime to_dateTime(string fmt)
        {
            string bcd = to_hex();
            return DateTime.ParseExact(bcd, fmt, null); ;
        }

        /// <summary>
        /// 16進文字列への変換
        /// </summary>
        /// <param name="sep">区切り文字(separator)</param>
        /// <param name="separate_size">区切り文字を挿入する間隔</param>
        /// <returns>16進文字列/<returns>
        public string to_hex(string sep=null, int separate_size=1)
        {
            StringBuilder sb = new StringBuilder();
            bool bFirst = true;
            int separate_cnt = 0;
            for (int i = 0; i < _dat.Length; i++)
            {
                if (sep != null)
                {
                    if (bFirst)
                    {
                        bFirst = false;
                    }
                    else
                    {
                        ++separate_cnt;
                        if(separate_cnt >= separate_size)
                        {
                            sb.Append(sep);
                            separate_cnt = 0;
                        }
                    }
                }
                sb.Append($"{_dat[i]:X2}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// ASCII文字列への変換
        /// </summary>
        /// <param name="sep">区切り文字(separator)</param>
        /// <param name="separate_size">区切り文字を挿入する間隔</param>
        /// <returns>表示できない文字はピリオド(".")に変換</returns>
        public string to_text_ascii(string sep = null, int separate_size = 1)
        {
            StringBuilder sb = new StringBuilder();
            bool bFirst = true;
            int separate_cnt = 0;
            for (int i = 0; i < _dat.Length; i++)
            {
                if (sep != null)
                {
                    if (bFirst)
                    {
                        bFirst = false;
                    }
                    else
                    {
                        ++separate_cnt;
                        if (separate_cnt >= separate_size)
                        {
                            sb.Append(sep);
                            separate_cnt = 0;
                        }
                    }
                }
                byte b = _dat[i];
                if (b < 0x20 || b>0x7e)
                {
                    sb.Append(".");
                }
                else
                {
                    sb.Append((char)b);
                }
            }

            return sb.ToString();
        }


        /// <summary>
        /// 指定された文字コードで文字列に変換
        /// </summary>
        /// <returns>文字列</returns>
        public string to_text_sjis()
        {
            return to_text(Encoding.GetEncoding("Shift_JIS"));
        }
        public string to_text_utf8()
        {
            return to_text(Encoding.UTF8);
        }
        public string to_text_unicode()
        {
            return to_text(Encoding.Unicode);
        }
        public string to_text()
        {
            // C#の文字列は、UTF16(リトルエンディアン)なので、デフォルトはUnicode
            return to_text(Encoding.Unicode);
        }

        /// <summary>
        /// 指定された文字コードで文字列に変換
        /// </summary>
        /// <param name="enc">encoding</param>
        /// <returns>文字列</returns>
        public string to_text(Encoding enc)
        {
            return enc.GetString(_dat);
        }

        //
        // static変換
        //
        static public ByteArray ParseHex(string hex)
        {
            // １６進表記の文字以外を削除（スペース等）
            Regex r = new Regex("[^0-9a-fA-F]");
            string h = r.Replace(hex, "");

            // 奇数文字なら後ろに"0"を追加
            if ((h.Length % 2) == 1)
            {
                h = h +"0";
            }

            int byte_size = h.Length / 2;
            byte[] buf = new byte[byte_size];

            int buf_idx = 0;
            for (int idx = 0; idx < h.Length; idx += 2)
            {
                string w = h.Substring(idx, 2);
                buf[buf_idx] = Convert.ToByte(w, 16);
                buf_idx++;
            }

            return new ByteArray(buf);
        }
        static public ByteArray StrToByte(string str)
        {
            Regex r = new Regex("[^0-9a-fA-F]");
            string hex = r.Replace(str, "");
            if (hex.Length == str.Length)
            {
                // １６進表記の文字のみなら16進データとする
                return ByteArray.ParseHex(str);
            }
            else
            {
                if(str.Length>1 && str.Substring(str.Length-1)==" ")
                {
                    // 文字列の最後がスペースなら削除する
                    // これにより例えば"abc"という文字列を指定したい時、"abc "と指定すればよい（16進データにならない）
                    str = str.Substring(0,str.Length-1);
                }
                return new ByteArray(Encoding.ASCII.GetBytes(str));
            }
        }
    }
}
