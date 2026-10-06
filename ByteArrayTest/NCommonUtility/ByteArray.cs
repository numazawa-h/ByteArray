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

        /// <summary>
        /// データの拡張
        /// </summary>
        /// <param name="size">拡張するバイト数</param>
        /// <param name="fill_value">拡張した部分を埋める値</param>
        /// <returns>更新後のthis</returns>
        public ByteArray Expand(int size, byte fill_value = 0)
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
        /// 対象範囲の分析
        /// </summary>
        /// <remarks>
        /// マイナスの引数や_datの長さを考慮して_datの対象範囲を返却する。
        /// </remarks>
        /// <param name="ofs">オフセット(マイナスなら末尾起点、ゼロでも長さがマイナスなら末尾起点)</param>
        /// <param name="len">長さ(マイナスなら ofsより前、ゼロなら末尾まで)</param>
        /// <returns>_datの対象範囲を(オフセット, 長さ)で返却</returns>
        private (int, int) analyzeRange(int ofs, int len)
        {
            // ofs がマイナスなら末尾からの位置に補正しておく
            if (ofs < 0)
            {
                ofs += _dat.Length;
            }
            // len が 0なら末尾までが対象
            if (len == 0)
            {
                len = _dat.Length - ofs;
            }
            // len がマイナスなら起点を終点にする(起点をcnt分前にずらす)
            if (len < 0)
            {
                // ofs がゼロなら末尾からの位置に補正しておく
                if (ofs == 0)
                {
                    ofs = _dat.Length;
                }
                len = -len;
                ofs = ofs - len;
            }
            // ofs がマイナスならゼロにして、 len を補正する
            if (ofs < 0)
            {
                len += ofs;
                ofs = 0;
                // 補正後の len がマイナスならゼロにする
                if (len < 0)
                {
                    len = 0;
                }
            }
            // 起点がデータ長を超えていたら補正する
            if (ofs > _dat.Length)
            {
                ofs = _dat.Length;
                len = 0;
            }
            // 終点がデータ長を超えていたら補正する
            if ((ofs + len) > _dat.Length)
            {
                len = _dat.Length - ofs;
            }

            return (ofs, len);
        }

        public void Clear()
        {
            Fill(0);
        }

        /// <summary>
        /// 特定の値で埋める
        /// </summary>
        /// <param name="val">埋める値</param>
        /// <param name="ofs">開始位置(マイナスの時は末尾からのオフセット)</param>
        /// <param name="len">長さ(ゼロの時は末尾までが対象)</param>
        /// <returns></returns>
        public ByteArray Fill(byte val = 0xff, int ofs = 0, int len = 0)
        {
            (ofs,len) = analyzeRange(ofs,len);

            // .NET FrameworkではArray.Fillを使えないのでループで対応
            for (int idx = ofs; len >0; idx++, len--)
            {
                _dat[idx] = val;
            }

            return this;
        }

        public ByteArray Take(int cnt = 1)
        {
            return Take(0, cnt);
        }

        public ByteArray Take(int ofs, int cnt)
        {
            int src_ofs = 0;    // データ取得のオフセット
            int len;            // データ長
            (src_ofs, len) = analyzeRange(ofs,cnt);
            if (len == 0)
            {
                return new ByteArray();
            }

            // 出力用dst[]の cntを算出
            int dst_ofs = 0;    // 出力用dst[]のオフセット(左ゼロ詰めの時)
            if (cnt < 0)
            {
                // マイナスなら cnt の絶対値が cnt
                cnt = -cnt;
                // データ長が小さければ左ゼロ詰め(データ長がcntより大きくなることはない)
                dst_ofs = cnt - len;
            }
            else
            {
                // cnt がゼロなら末尾までが対象
                if (cnt == 0)
                {
                    if (ofs < 0)
                    {
                        // ofs がマイナスなら末尾からの相対なので絶対値がそのまま cnt
                        cnt = -ofs;
                    }
                    else
                    {
                        // マイナスでなければ ofs から末尾までが cnt
                        cnt = _dat.Length - ofs;
                    }
                }
            }

            // 末尾からの相対でデータ長が足りなければ左ゼロ詰め
            if (ofs < 0)
            {
                ofs = -ofs;
                if( ofs > _dat.Length)
                {
                    dst_ofs = ofs - _dat.Length;
                }
            }

            byte[] dst = new byte[cnt];
            Buffer.BlockCopy(_dat, src_ofs, dst, dst_ofs, len);
            return new ByteArray(dst);
        }

        /// <summary>
        /// データをコピーする
        /// </summary>
        /// <param name="other">コピー元データ</param>
        /// <param name="ofs">コピー先位置（自身データのオフセット）</param>
        /// <param name="len">データ長</param>
        /// <returns>更新後のthis</returns>
        public ByteArray Copy(ByteArray other, int ofs = 0, int len = 0)
        {
            return Copy(other._dat, ofs, len);
        }
        public ByteArray Copy(byte[] other, int ofs = 0, int len = 0)
        {
            if (other == null || other.Length == 0)
            {
                // コピー元データがなければ更新なし
                return this;
            }
            if (len == 0)
            {
                // データ長の指定がなければコピー元データ長
                len = other.Length;
            }

            int dst_ofs;
            int cnt = len;      // 元のデータ長を覚えておく(ゼロ埋めや右寄せ処理に使う)
            (dst_ofs, len) = analyzeRange(ofs, len);
            if (len == 0)
            {
                // コピー先の対象範囲がなければ更新なし
                return this;
            }

            // 末端相対指定で自データ長を超える場合、コピー元のオフセットで調整
            int src_ofs = 0;
            if (ofs < 0)
            {
                if (cnt < 0)
                {
                    // cnt がマイナスなら ofs よりさらに右が開始位置
                    ofs += cnt;
                }
                ofs = -ofs;
                if (ofs > _dat.Length)
                {
                    src_ofs = ofs - _dat.Length;
                }
            }

            // 指定された長さがマイナスなら絶対値にして右寄せ
            bool right_align = false;
            if (cnt < 0)
            {
                cnt = -cnt;
                right_align = true;
            }

            byte[] src;
            if (cnt == other.Length)
            {
                // 指定された長さがコピー元データ長と同じならコピー元をそのままコピー
                src = other;
            }
            else
            {
                src = new byte[cnt];
                if (cnt > other.Length)
                {
                    if (right_align)
                    {
                        // 指定された長さがコピー元データより長ければ、右寄せ
                        Buffer.BlockCopy(other, 0, src, cnt - other.Length, other.Length);
                    }
                    else
                    {
                        // 指定された長さがコピー元データより長ければ、左寄せ
                        Buffer.BlockCopy(other, 0, src, 0, other.Length);
                    }
                }
                else
                {
                    if (right_align)
                    {
                        // 指定された長さがコピー元データより短ければ、右端取得
                        Buffer.BlockCopy(other, cnt - other.Length, src, 0, cnt);
                    }
                    else
                    {
                        // 指定された長さがコピー元データより短ければ、左端取得
                        Buffer.BlockCopy(other, 0, src, 0, cnt);
                    }
                }
            }


            // コピーすべきデータがあればコピーする
            if(src_ofs < src.Length)
            {
                Buffer.BlockCopy(src, src_ofs, _dat, dst_ofs, len);
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
