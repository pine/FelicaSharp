using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FelicaSharp
{
    /// <summary>
    /// <para>Microsoft による PC/SC (Personal Computer / Smart Card) の実装を、
    /// .NET から利用するための定義を行っているクラスです。</para>
    /// <seealso cref="SmartCardResourceManager"/>
    /// <seealso cref="FelicaReader"/>
    /// <seealso cref="FelicaCard"/>
    /// <seealso cref="DynamicLink"/>
    /// </summary>
    internal static class PersonalComputerSmartCard
    {
        internal const int SCARD_SCOPE_USER = 0;
        internal const int SCARD_SCOPE_TERMINAL = 1;
        internal const int SCARD_SCOPE_SYSTEM = 2;
        
        internal const int SCARD_S_SUCCESS = unchecked((int)0x00000000);
        internal const int SCARD_E_NO_SERVICE = unchecked((int)0x8010001D);

        internal const int SCARD_SHARE_SHARED = 0x0002;

        internal const int SCARD_PROTOCOL_T0 = 0x0001;
        internal const int SCARD_PROTOCOL_T1 = 0x0002;
        internal const int SCARD_PROTOCOL_RAW = 0x0004;
        internal const int SCARD_PROTOCOL_UNDEFINED = 0x0000;

        internal const int SCARD_STATE_UNAWARE = unchecked(0x0000);
        internal const int SCARD_STATE_EMPTY = unchecked(0x0010);
        internal const int SCARD_STATE_PRESENT = unchecked(0x0020);
        internal const int SCARD_STATE_UNAVAILABLE = unchecked(0x0008);
        internal const int SCARD_STATE_INUSE = unchecked(0x0100);

        internal const int SCARD_LEAVE_CARD = unchecked(0x0000);

        internal static readonly IntPtr SCARD_PCI_T0;
        internal static readonly IntPtr SCARD_PCI_T1;
        internal static readonly IntPtr SCARD_PCI_RAW;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct SCARD_READERSTATE
        {
            public string szReader;
            public IntPtr pvUserData;
            public UInt32 dwCurrentState;
            public UInt32 dwEventState;
            public UInt32 cbAtr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            public byte[] rgbAtr;
        }

        /// <summary>
        /// スタティックメンバの初期化を行います。
        /// </summary>
        static PersonalComputerSmartCard()
        {
            IntPtr handle = DynamicLink.LoadLibrary("Winscard.dll");

            SCARD_PCI_T0 = DynamicLink.GetProcAddress(handle, "g_rgSCardT0Pci");
            SCARD_PCI_T1 = DynamicLink.GetProcAddress(handle, "g_rgSCardT1Pci");
            SCARD_PCI_RAW = DynamicLink.GetProcAddress(handle, "g_rgSCardRawPci");

            DynamicLink.FreeLibrary(handle);
        }

        /// <summary>
        /// スマートカードマネージャーへ接続し、コンテキストを取得する Windows API 関数です。
        /// </summary>
        /// <param name="dwScope">スコープを指定します。
        /// <see cref="SCARD_SCOPE_USER"/>を指定してください。</param>
        /// <param name="pvReserved1">予約済みです。<see cref="IntPtr.Zero"/>を指定してください。</param>
        /// <param name="pvReserved2">予約済みです。<see cref="IntPtr.Zero"/>を指定してください。</param>
        /// <param name="phContext">スマートカードマネージャーへのコンテキストの出力先を指定します。</param>
        /// <returns>
        /// 関数が成功したかどうかが返ります。
        /// 成功した場合<see cref="SCARD_S_SUCCESS"/>が返ります。
        /// スマートカードサービスが起動していない場合は<see cref="SCARD_E_NO_SERVICE"/>が返ります。
        /// </returns>
        [DllImport("Winscard.dll")]
        internal static extern int SCardEstablishContext(
            uint dwScope,
            IntPtr pvReserved1,
            IntPtr pvReserved2,
            ref IntPtr phContext
            );

        /// <summary>
        /// <para>スマートカードマネージャーへのコンテキストを開放する Windows API 関数です。</para>
        /// <seealso cref="SCardEstablishContext"/>
        /// </summary>
        /// <param name="hContext">開放するスマートカードマネージャーへのコンテキストです。</param>
        /// <returns>関数が成功したかどうかが返ります。
        /// 成功した場合<see cref="SCARD_S_SUCCESS"/>が返ります。</returns>
        [DllImport("Winscard.dll")]
        internal static extern int SCardReleaseContext(
            IntPtr hContext
            );

        /// <summary>
        /// <para>スマートカードリソースマネージャーへ接続されているリーダー一覧を取得する Windows API 関数です。</para>
        /// <seealso cref="SCardConnect"/>
        /// </summary>
        /// <param name="hContext">スマートカードマネージャーへのコンテキストです。</param>
        /// <param name="mszGroups">取得するリーダーのグループを指定します。
        ///     接続されているすべてのリーダーを取得するには、<value>null</value>を指定します。</param>
        /// <param name="mszReaders">リーダの名前を受け取るバッファを指定します。
        ///     <value>null</value>を指定して呼び出した場合は、
        ///     <paramref name="pcchReaders"/>に要求されるバッファサイズが格納されます。</param>
        /// <param name="pcchReaders"><paramref name="mszReaders"/>で指定したバッファのサイズを指定します。</param>
        /// <returns>関数が成功したかどうかが返ります。
        /// 成功した場合<see cref="SCARD_S_SUCCESS"/>が返ります。</returns>
        [DllImport("Winscard.dll", CharSet = CharSet.Unicode, EntryPoint = "SCardListReadersW")]
        internal static extern int SCardListReaders(
            [In] IntPtr hContext,
            [In, Optional] string mszGroups,
            [Out] string mszReaders,
            [In, Out] ref int pcchReaders
            );

        /// <summary>
        /// スマートカードリーダーの状態変化を取得する関数です。
        /// </summary>
        /// <param name="hContext">スマートカードマネージャーへのコンテキストです。</param>
        /// <param name="dwTimeout">タイムアウト時間をミリ秒で指定します。
        ///     <value>0</value>を指定した場合は、現在の状態を取得します。</param>
        /// <param name="rgReaderStates">
        ///     状態変化を取得したいリーダー名や、取得した状態を格納する配列を指定します。</param>
        /// <param name="cReaders"><paramref name="rgReaderStates"/>の個数を指定します。</param>
        /// <returns>関数が成功したかどうかが返ります。
        ///     成功した場合<see cref="SCARD_S_SUCCESS"/>が返ります。</returns>
        [DllImport("Winscard.dll", CharSet = CharSet.Unicode, EntryPoint = "SCardGetStatusChangeW")]
        internal static extern int SCardGetStatusChange(
            IntPtr hContext,
            int dwTimeout,
            [In, Out] SCARD_READERSTATE[] rgReaderStates,
            int cReaders
            );

        /// <summary>
        /// スマートカードへ接続します。
        /// </summary>
        /// <param name="hContext">スマートカードリソースマネージャーへのコンテキストを指定します。</param>
        /// <param name="szReader">接続する FeliCa リーダーを指定します。</param>
        /// <param name="dwShareMode">共有モードを指定します。
        ///     通常は<see cref="SCARD_SHARE_SHARED"/>を指定してください。</param>
        /// <param name="dwPreferredProtocols">カードとの通信プロトコルを指定します。
        ///     <see cref="SCARD_PROTOCOL_T0"/>、<see cref="SCARD_PROTOCOL_T1"/>、
        ///     <see cref="SCARD_PROTOCOL_RAW"/>の何れか、もしくは論理和をとったものを指定します。</param>
        /// <param name="phCard">カードと接続したハンドルを受け取る、変数のアドレスを指定します。</param>
        /// <param name="pdwActiveProtocol">実際に用いられた通信プロトコルを取得します。</param>
        /// <returns>関数が成功したかどうかが返ります。
        /// 成功した場合<see cref="SCARD_S_SUCCESS"/>が返ります。</returns>
        [DllImport("Winscard.dll", CharSet = CharSet.Unicode, EntryPoint = "SCardConnectW")]
        internal static extern int SCardConnect(
            [In] IntPtr hContext,
            [In, MarshalAs(UnmanagedType.LPWStr)] string szReader,
            [In] uint dwShareMode,
            [In] uint dwPreferredProtocols,
            [Out] out IntPtr phCard,
            [Out] out int pdwActiveProtocol
            );

        /// <summary>
        /// スマートカードから切断します。
        /// </summary>
        /// <param name="hCard">切断するカードのハンドルを指定します。</param>
        /// <param name="dwDisposition"><see cref="SCARD_LEAVE_CARD"/>を指定します。</param>
        /// <returns>関数が成功したかどうかが返ります。
        ///     成功した場合<see cref="SCARD_S_SUCCESS"/>が返ります。</returns>
        [DllImport("Winscard.dll", EntryPoint = "SCardDisconnect")]
        internal static extern int SCardDisconnect(
            [In] IntPtr hCard,
            int dwDisposition
            );

        /// <summary>
        /// スマートカードへコマンドを送信します。
        /// </summary>
        /// <param name="hCard">送信するカードへのハンドルを指定します。</param>
        /// <param name="pioSendPci">送信に使用するプロトコルを指定します。</param>
        /// <param name="pbSendBuffer">送信したいコマンドを格納したバッファを指定します。</param>
        /// <param name="cbSendLength"><paramref name="pbSendBuffer"/>で指定したバッファサイズを指定します。</param>
        /// <param name="pioRecvPci"><see cref="IntPtr.Zero"/>を指定してください。</param>
        /// <param name="pbRecvBuffer">コマンドに対するレスポンスを受け取るバッファを指定します。</param>
        /// <param name="pcbRecvLength"><paramref name="pbRecvBuffer"/>で指定したバッファサイズを指定します。</param>
        /// <returns></returns>
        [DllImport("Winscard.dll")]
        internal static extern int SCardTransmit(
            [In] IntPtr hCard,
            [In] IntPtr pioSendPci,
            [In] byte[] pbSendBuffer,
            [In] int cbSendLength,
            [In, Out, Optional] IntPtr pioRecvPci,
            [In, Out] ref byte pbRecvBuffer,
            [In, Out] ref int pcbRecvLength
            );

        /// <summary>
        /// カードプロトコルを PC/SC で指定するデータに変換します。
        /// </summary>
        internal static IntPtr CardProtocol2PCI(int dwProtocol)
        {
            if (dwProtocol == SCARD_PROTOCOL_T0)
            {
                return SCARD_PCI_T0;
            }
            else if (dwProtocol == SCARD_PROTOCOL_T1)
            {
                return SCARD_PCI_T1;
            }
            else if (dwProtocol == SCARD_PROTOCOL_RAW)
            {
                return SCARD_PCI_RAW;
            }
            else if (dwProtocol == SCARD_PROTOCOL_UNDEFINED)
            {
                Debug.Assert(false);
                return IntPtr.Zero;
            }

            return SCARD_PCI_T1;
        }

    }
}
