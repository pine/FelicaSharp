using System;
using System.Linq;
using System.Diagnostics;

namespace FelicaSharp
{
    using PCSC = PersonalComputerSmartCard;

    public class FelicaCard
    {
        /// <summary>
        /// <para>受信バッファのサイズです。</para>
        /// <para>値は「SDK for NCF Starter Kit Ver.2.0」のサンプルプログラムより拝借しました。</para>
        /// </summary>
        protected const int PCSC_RECV_BUFF_LEN = 262;

        public FelicaReader Reader { private set; get; }
        public int Protocol { private set; get; }
        internal IntPtr Card { set; get; }

        /// <summary>
        /// FeliCa カードを初期化します。
        /// </summary>
        /// <param name="reader">FeliCa リーダーを指定します。</param>
        /// <param name="card">FeliCa カードへのハンドルを指定します。</param>
        /// <param name="protocol">FeliCa カードとの通信に用いるプロトコルを指定します。</param>
        internal FelicaCard(FelicaReader reader, IntPtr card, int protocol)
        {
            if (reader == null) { throw new ArgumentNullException("reader"); }
            if (card == null) { throw new ArgumentNullException("card"); }
            if (protocol == PCSC.SCARD_PROTOCOL_UNDEFINED) { throw new ArgumentOutOfRangeException("protocol"); }

            this.Reader = reader;
            this.Card = card;
            this.Protocol = protocol;
        }

        /// <summary>
        /// FeliCa カードに対してコマンドを送信します。
        /// </summary>
        /// <param name="command">実行するコマンドを指定します。</param>
        /// <returns>コマンドの実行結果をステータスワードを除いて返します。</returns>
        /// <exception cref="FelicaException">
        ///     コマンドの送信が失敗した場合、コマンドの実行が正常に終了しなかった場合に発生します。</exception>
        public byte[] SendCommand(byte[] command)
        {
            if (command == null) { throw new ArgumentNullException("command"); }
            if (command.Length == 0) { throw new ArgumentException("コマンドはゼロバイト以上を指定してください。", "command"); }
            if (this.Card == IntPtr.Zero) { throw new InvalidOperationException("カードは既に無効です。"); }

            byte[] receiveBuffer = new byte[PCSC_RECV_BUFF_LEN];
            int receiveSize = receiveBuffer.Length;
            IntPtr pci = PCSC.CardProtocol2PCI(this.Protocol);

            // カードにコマンドを送信する
            int result = PCSC.SCardTransmit(
                this.Card,
                pci,
                command,
                command.Length,
                IntPtr.Zero,
                ref receiveBuffer[0],
                ref receiveSize
                );

            if (result != PCSC.SCARD_S_SUCCESS || receiveSize < 2)
            {
                throw new FelicaException("FeliCa へのコマンド送信に失敗しました。");
            }

            Debug.Assert(result == PCSC.SCARD_S_SUCCESS); // 成功

            // ステータスワード (コマンドが成功したかどうか)
            var statusWord = new[] {
                receiveBuffer[receiveSize - 2],
                receiveBuffer[receiveSize - 1]
            };

            // 正常終了な場合
            if (statusWord[0] == 0x90 && statusWord[1] == 0x00)
            {
                // ステータスワードを除いて返す
                return receiveBuffer.Take(receiveSize - 2).ToArray();
            }

            throw new FelicaException(
                string.Format("FeliCa でのコマンド実行が正常に終了しませんでした。ステータワード SW1 = 0x{0:X2}, SW2 = 0x{1:X2}", statusWord[0], statusWord[1])
            );
        }
    }
}
