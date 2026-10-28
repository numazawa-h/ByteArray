using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NCommonUtility
{
    public class ByteArray
    {
        // Shift()の方向
        public enum Direction { Left = 0, Right = ~Left }

        // 保持データ
        protected byte[] _dat;

        // システム依存の改行コード
        static public readonly ByteArray NewLine = new ByteArray(Environment.NewLine);

        public ByteArray()
        {
            _dat = System.Array.Empty<byte>();
        }

        //
        // 整数系コンストラクタ(1バイトから8バイト)
        // マイナス値はキャストして使う(定数の時はキャストでエラーになるのでunchecked()で囲う)
        //

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
        public ByteArray(string text, Encoding enc = null)
        {
            if (enc == null)
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

        /// <summary>
        /// バイト列検索
        /// </summary>
        /// <param name="pattern">検索するパターン</param>
        /// <returns>見つからない時：-1、見つかった時：先頭のオフセット</returns>
        public int IndexOf(byte pattern)
        {
            return Array.IndexOf(_dat, pattern);
        }
        public int IndexOf(ByteArray pattern)
        {
            return IndexOf(pattern._dat);
        }
        public int IndexOf(byte[] pattern)
        {
            if (pattern == null) return -1;
            if (pattern.Length == 0) return 0;
            if (_dat.Length < pattern.Length) return -1;

            int offset = Array.IndexOf(_dat, pattern[0]);
            while (offset <= _dat.Length - pattern.Length)
            {
                if (offset < 0) break;
                bool isMatch = true;
                for (int i = 1; i < pattern.Length; i++)
                {
                    if (_dat[offset + i] != pattern[i])
                    {
                        isMatch = false;
                        break;
                    }
                }
                if (isMatch) return offset;
                offset = Array.IndexOf(_dat, pattern[0], offset + 1);
            }

            return -1;
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

        public ByteArray ShiftLeft(int cnt = 1) => Shift(cnt, Direction.Left);
        public ByteArray ShiftRight(int cnt = 1) => Shift(cnt, Direction.Right);
        /// <summary>
        /// データシフト(バイト単位)
        /// </summary>
        /// <param name="cnt">シフトするバイト数(マイナスなら逆方向にシフト)</param>
        /// <param name="direction">シフト方向</param>
        /// <returns>シフト後の自身</returns>
        public ByteArray Shift(int cnt, Direction direction)
        {
            if (cnt == 0)
            {
                // cntがゼロなら何もしない
                return this;
            }

            if (cnt < 0)
            {
                // cntがマイナスなら指定された逆方向にシフトする
                direction = ~direction;
                cnt = -cnt;
            }

            if (cnt >= _dat.Length)
            {
                // cntがデータ長以上なら全クリア
                Clear();
            }
            else
            {
                int copycnt = _dat.Length - cnt;
                if (direction == Direction.Left)
                {
                    // 左シフト
                    Array.Copy(_dat, cnt, _dat, 0, copycnt);
                    Array.Clear(_dat, copycnt, cnt);    // 末尾のcntバイトをクリア
                }
                else
                {
                    // 右シフト
                    Array.Copy(_dat, 0, _dat, cnt, copycnt);
                    Array.Clear(_dat, 0, cnt);      // 先頭のcntバイトをクリア
                }
            }

            return this;
        }

        public ByteArray HalfShiftLeft() => HalfShift(Direction.Left);
        public ByteArray HalfShiftRight() => HalfShift(Direction.Right);
        /// <summary>
        /// データシフト(4ビット単位)
        /// </summary>
        /// <param name="direction">シフト方向</param>
        /// <returns>シフト後の自身</returns>
        public ByteArray HalfShift(Direction direction)
        {
            if ((direction == Direction.Left))
            {
                for (int i = 0; i < _dat.Length; i++)
                {
                    int carry = 0;
                    if (i + 1 < _dat.Length)
                    {
                        carry = _dat[i + 1] >> 4;
                    }
                    _dat[i] = (byte)(_dat[i] << 4 | carry);
                }
            }
            else
            {
                for (int i = _dat.Length - 1; i >= 0; i--)
                {
                    int carry = 0;
                    if (i > 0)
                    {
                        carry = _dat[i - 1] << 4;
                    }
                    _dat[i] = (byte)(_dat[i] >> 4 | carry);
                }
            }

            return this;
        }

        /// <summary>
        /// 末尾にデータを追加する
        /// </summary>
        /// <param name="other">追加するデータ</param>
        /// <returns>更新後のthis</returns>
        public ByteArray Append(string other) => Append(new ByteArray(other));
        public ByteArray Append(ByteArray other) => Append(other._dat);
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
            Array.Clear(_dat, 0, _dat.Length);
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
            (ofs, len) = analyzeRange(ofs, len);

            if (val == 0)
            {
                Array.Clear(_dat, ofs, len);
            }
            else
            {
                // .NET FrameworkではArray.Fillを使えないのでループで対応
                for (int idx = ofs; len > 0; idx++, len--)
                {
                    _dat[idx] = val;
                }
            }

            return this;
        }

        public ByteArray Take(int cnt = 1)
        {
            return Take(0, cnt);
        }

        /// <summary>
        /// 部分の取得
        /// </summary>
        /// <param name="ofs">オフセット(マイナスの時は末尾からの相対)(cntがマイナスならofsが0の時も末尾から)</param>
        /// <param name="cnt">取得するバイト数(マイナスの時はオフセットより前の部分が対象)</param>
        /// <returns>指定された部分をByteArrayで返却</returns>
        public ByteArray Take(int ofs, int cnt)
        {
            int src_ofs = 0;    // データ取得のオフセット
            int len;            // データ長
            (src_ofs, len) = analyzeRange(ofs, cnt);
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
                if (ofs > _dat.Length)
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
                    // cnt もマイナスなら ofs よりさらに左が開始位置
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
                        Buffer.BlockCopy(other, other.Length - cnt, src, 0, cnt);
                    }
                    else
                    {
                        // 指定された長さがコピー元データより短ければ、左端取得
                        Buffer.BlockCopy(other, 0, src, 0, cnt);
                    }
                }
            }

            // コピーすべきデータがあればコピーする
            if (src_ofs < src.Length)
            {
                Buffer.BlockCopy(src, src_ofs, _dat, dst_ofs, len);
            }

            return this;
        }

        /// <summary>
        /// バイト配列の取得
        /// </summary>
        /// <remarks>
        /// 当メソッドはコピーを生成して返却する。
        /// 参照で直接変更したい時は、予め呼び出し元でバイト配列生成しておいて、それをコンストラクタに渡す。
        ///     Byte[] dat = { 0x01, 0x23, 0x45, 0x67 };
        ///     ba = new ByteArray(dat);
        ///     Assert.AreEqual("01234567", ba.to_hex());
        ///     dat[1] = 0xfe;
        ///     Assert.AreEqual("01FE4567", ba.to_hex());
        /// </remarks>
        /// <returns>コピーされたバイト配列</returns>
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
            if ((_dat[0] & 0x80) != 0)
            {
                // マイナス値なら val を 0xFF で埋める
                new ByteArray(val).Fill();
            }
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
            if ((_dat[0] & 0x80) != 0)
            {
                // マイナス値なら val を 0xFF で埋める
                new ByteArray(val).Fill();
            }
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
        public string to_hex(string sep = null, int separate_size = 1)
        {
            int capacity = _dat.Length * ((sep == null)? 2 :sep.Length + 2);  // separate_sizeは無視して大き目に設定
            StringBuilder sb = new StringBuilder(capacity);
            if (sep == null || separate_size == 0)
            {
                separate_size = -1;
            }

            int separate_cnt = separate_size;
            for (int i = 0; i < _dat.Length; i++)
            {
                if (separate_cnt == 0)
                {
                    sb.Append(sep);
                    separate_cnt = separate_size;
                }
                sb.Append(HEX_TABLE[_dat[i]]);
                --separate_cnt;
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
            int capacity = _dat.Length * ((sep == null) ? 1 : sep.Length + 1);  // separate_sizeは無視して大き目に設定
            StringBuilder sb = new StringBuilder(capacity);
            if (sep == null || separate_size == 0)
            {
                separate_size = -1;
            }

            int separate_cnt = separate_size;
            for (int i = 0; i < _dat.Length; i++)
            {
                if (separate_cnt == 0)
                {
                    sb.Append(sep);
                    separate_cnt = separate_size;
                }
                byte b = _dat[i];
                if (b < 0x20 || b > 0x7e)
                {
                    sb.Append(".");
                }
                else
                {
                    sb.Append((char)b);
                }
                --separate_cnt;
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

        /// <summary>
        /// ゾーン形式二進化十進をパック形式に変換する
        /// </summary>
        /// <returns></returns>
        public ByteArrayBcd UnZone()
        {
            return ByteArrayBcd.UnZone(this);
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
                h = h + "0";
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
                if (str.Length > 1 && str.Substring(str.Length - 1) == " ")
                {
                    // 文字列の最後がスペースなら削除する
                    // これにより例えば"abc"という文字列を指定したい時、"abc "と指定すればよい（16進データにならない）
                    str = str.Substring(0, str.Length - 1);
                }
                return new ByteArray(Encoding.ASCII.GetBytes(str));
            }
        }

        /// <summary>
        /// 二進化十進クラスを生成する
        /// </summary>
        /// <param name="digits">10進桁数</param>
        /// <returns></returns>
        static public ByteArrayBcd Bcd(int digits)
        {
            return new ByteArrayBcd(digits);
        }





        /// <summary>
        /// 二進化十進クラス
        /// </summary>
        public class ByteArrayBcd : ByteArray
        {
            bool _signed = false;       // マイナスの時、True
            int _bitcnt = 0;            // 対象のビット数

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="digits">10進桁数</param>
            /// <exception cref="ArgumentException">digits: 1から16まで</exception>
            public ByteArrayBcd(int digits) : base()
            {
                if ((digits <= 0) || (digits > 16))
                {
                    throw new ArgumentException("The argument 'digits' is invalid");
                }
                _bitcnt = digits * 4;
                int byte_len = (_bitcnt + 7) / 8;
                base.Expand(byte_len);
            }

            public ByteArrayBcd Pack(int num)
            {
                if (num < 0)
                {
                    _signed = true;
                    num = -num;
                }
                return Pack((UInt64)num);
            }

            public ByteArrayBcd Pack(UInt64 num)
            {
                if (NUM_MAX[_bitcnt / 4] < num)
                {
                    throw new ArgumentException("The argument 'num' is over");
                }
                UInt64 msk1 = (UInt64)1 << (_bitcnt - 1);
                UInt64 bcd = 0;
                for (int i = 0; i < _bitcnt; ++i)
                {
                    // 補正(全4ビットに対して「5以上なら3を足す」補正を実施する)
                    UInt64 msk3 = 3;
                    UInt64 msk4 = 4;
                    UInt64 msk8 = 8;
                    for (int j = 0; j < _bitcnt; j += 4)
                    {
                        if ((bcd & msk8) != 0)
                        {
                            bcd += msk3;
                        }
                        else if ((bcd & msk4) != 0)
                        {
                            if ((bcd & msk3) != 0)
                            {
                                bcd += msk3;
                            }
                        }
                        msk3 <<= 4;
                        msk4 <<= 4;
                        msk8 <<= 4;
                    }

                    // 1 ビットシフト
                    bcd <<= 1;
                    if ((num & msk1) != 0)
                    {
                        bcd |= 1;
                    }
                    msk1 >>= 1;
                }

                ByteArray ba = new ByteArray(bcd);
                Copy(ba, 0, -_dat.Length);
                return this;
            }

            public ByteArray Zone(byte zone_prefix = 0x30)
            {
                if((zone_prefix & 0x0F) != 0)
                {
                    throw new ArgumentException("The argument 'zone_prefix' is invalid");
                }
                int cnt = _bitcnt / 4;
                byte[] zone = new byte[cnt];

                int odd = (cnt % 2);
                int dat_idx = 0;
                for (int i = 0; i < cnt; i += 2, dat_idx++)
                {
                    byte dat = _dat[dat_idx];
                    if (i > 0 || odd == 0)
                    {
                        zone[i - odd] = (byte)(zone_prefix | (dat >> 4));
                    }
                    zone[i - odd + 1] = (byte)(zone_prefix | (dat & 0x0f));
                }

                // 符号部
                if (zone_prefix == 0x30)
                {
                    if (_signed)
                    {
                        // Zone()でASCII系(0x3)の時、マイナスの符号部は 0x7(プラスの時は 0x3 のまま)
                        zone[cnt - 1] |= 0x40;
                    }
                }
                else
                {
                    zone[cnt - 1] &= 0x0F;
                    if (_signed)
                    {
                        // EBCDIC系の時、マイナスの符号部は 0xD
                        zone[cnt - 1] |= 0xD0;
                    }
                    else
                    {
                        // EBCDIC系の時、プラスの符号部は 0xC
                        zone[cnt - 1] |= 0xC0;
                    }
                }

                return new ByteArray(zone);
            }

            /// <summary>
            /// パック10進数に符号部を付加する
            /// </summary>
            /// <remarks>マイナスの符号部は 0xD、プラスの符号部は 0xC</remarks>
            /// <returns>生成したByteArray</returns>
            public ByteArray Signed()
            {
                ByteArray ba = new ByteArray(this._dat).Clone();
                int cnt = _bitcnt / 4;
                if ((cnt % 2) == 0)
                {
                    ba.Expand(1);
                    // 右4ビットシフト
                    ba.HalfShiftRight();
                }
                else
                {
                    // 左4ビットシフト
                    ba.HalfShiftLeft();
                }

                if(_signed)
                {
                    ba._dat[ba.Length() - 1] |= 0x0D;
                }
                else
                {
                    ba._dat[ba.Length() - 1] |= 0x0C;
                }

                return ba;
            }

            public ByteArray UnPack()
            {
                ByteArray bcd = this.Clone();
                int digits = _bitcnt >> 2;      // 10進桁数
                if( (digits % 2) == 1)
                {
                    bcd.HalfShiftLeft();        // 奇数桁なら最初の4ビットは無視
                }
                UInt64 num = 0;
                for (int i = 0; i< digits; i++)
                {
                    num = (num << 3) + (num << 1);   // 10倍にする(8倍プラス2倍)
                    num += (UInt64)bcd._dat[0]>>4;
                    bcd.HalfShiftLeft();
                }
                if (_signed == true)
                {
                    num = ~num + 1;             // マイナス値にする
                }

                // 10進桁数に合わせてバイト長を決める
                ByteArray ba = null;
                switch ((digits + 1) / 2)
                {
                    case 1:
                        ba = new ByteArray((byte)num);
                        break;
                    case 2:
                        ba = new ByteArray((UInt16)num);
                        break;
                    case 4:
                        ba = new ByteArray((UInt32)num);
                        break;
                    case 8:
                        ba = new ByteArray(num);
                        break;
                }

                return ba;
            }

            /// <summary>
            /// ゾーン形式二進化十進をパック形式に変換する
            /// </summary>
            /// <returns></returns>
            static public ByteArrayBcd UnZone(ByteArray ba)
            {
                int cnt = ba.Length();
                ByteArrayBcd bcd = new ByteArrayBcd(cnt);
                switch(ba._dat[ba.Length() - 1] & 0xF0)
                {
                    case 0xD0:  // EBCDIC系の時、マイナスの符号部は 0xD
                    case 0x70:  // ASCII系の時、マイナスの符号部は 0x7
                        bcd._signed = true;
                        break;
                }

                int odd = (cnt % 2);
                if (odd == 1)
                {
                    // 奇数桁の時、先頭桁を先に処理
                    bcd._dat[0] = (byte)(ba._dat[0] & 0x0F);
                }
                int dat_idx = odd;
                for (int i = odd; i < cnt; i += 2, dat_idx++)
                {
                    bcd._dat[dat_idx] = (byte)((ba._dat[i] & 0x0F) <<4 | (ba._dat[i+1] & 0x0F));
                }

                return bcd;
            }
        }

        // Pack() の引数チェック用
        static readonly UInt64[] NUM_MAX = new UInt64[] {
                0,
                9,
                99,
                999,
                9999,
                9_9999,
                99_9999,
                999_9999,
                9999_9999,
                9_9999_9999,
                99_9999_9999,
                999_9999_9999,
                9999_9999_9999,
                9_9999_9999_9999,
                99_9999_9999_9999,
                999_9999_9999_9999,
                9999_9999_9999_9999,
        };

        // to_hex() の変換用
        static readonly string[] HEX_TABLE = new string[] {
            "00", "01", "02", "03", "04", "05", "06", "07", "08","09", "0A", "0B", "0C", "0D", "0E", "0F",
            "10", "11", "12", "13", "14", "15", "16", "17", "18","19", "1A", "1B", "1C", "1D", "1E", "1F",
            "20", "21", "22", "23", "24", "25", "26", "27", "28","29", "2A", "2B", "2C", "2D", "2E", "2F",
            "30", "31", "32", "33", "34", "35", "36", "37", "38","39", "3A", "3B", "3C", "3D", "3E", "3F",
            "40", "41", "42", "43", "44", "45", "46", "47", "48","49", "4A", "4B", "4C", "4D", "4E", "4F",
            "50", "51", "52", "53", "54", "55", "56", "57", "58","59", "5A", "5B", "5C", "5D", "5E", "5F",
            "60", "61", "62", "63", "64", "65", "66", "67", "68","69", "6A", "6B", "6C", "6D", "6E", "6F",
            "70", "71", "72", "73", "74", "75", "76", "77", "78","79", "7A", "7B", "7C", "7D", "7E", "7F",
            "80", "81", "82", "83", "84", "85", "86", "87", "88","89", "8A", "8B", "8C", "8D", "8E", "8F",
            "90", "91", "92", "93", "94", "95", "96", "97", "98","99", "9A", "9B", "9C", "9D", "9E", "9F",
            "A0", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8","A9", "AA", "AB", "AC", "AD", "AE", "AF",
            "B0", "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8","B9", "BA", "BB", "BC", "BD", "BE", "BF",
            "C0", "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8","C9", "CA", "CB", "CC", "CD", "CE", "CF",
            "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8","D9", "DA", "DB", "DC", "DD", "DE", "DF",
            "E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7", "E8","E9", "EA", "EB", "EC", "ED", "EE", "EF",
            "F0", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8","F9", "FA", "FB", "FC", "FD", "FE", "FF",
        };
    }
}
