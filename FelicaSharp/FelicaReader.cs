using System;
using System.Threading;
using System.Diagnostics;

namespace FelicaSharp
{
    using PCSC = PersonalComputerSmartCard; // 長いので別名を定義

    /// <summary>
    /// FeliCa リーダーを表すクラスです。
    /// リーダーへのポーリングにより、カード情報の習得が可能です。
    /// </summary>
    public class FelicaReader : IDisposable
    {
        /// <summary>
        /// カードが載せられた時に発生するイベントです。
        /// </summary>
        public event EventHandler<FelicaCardSetEventHandlerArgs> FelicaCardSet;
        public event EventHandler<FelicaReaderRemovedEventHandlerArgs> FelicaReaderRemoved;

        public SmartCardResourceManager ResourceManager { private set; get; }
        public string ReaderName { private set; get; }

        /// <summary>
        /// <para>カードリーダーへ接続しているかを返します。</para>
        /// <para>接続している場合<value>true</value>、切断している場合<value>false</value>を返します。</valuue></para>
        /// </summary>
        public bool IsConnected
        {
            get
            {
                bool connected = false;

                // ポーリングスレッドが動いているか取得する
                lock (this.pollingThreadLockObject)
                {
                    // スレッドが動作している
                    connected = this.pollingThread != null && this.pollingThread.IsAlive;
                }

                // カードリーダーが取り外されていない場合は true
                return connected && !this.IsRemoved;
            }
        }

        /// <summary>
        /// FeliCa リーダーが取り外されているかどうかを表します。
        /// </summary>
        public bool IsRemoved { get; private set; }

        /// <summary>
        /// FeliCa リーダーに対するポーリングを行うスレッドです。
        /// </summary>
        private Thread pollingThread = null;

        /// <summary>
        /// ポーリングスレッドで取得したカードを格納する変数です。
        /// </summary>
        private FelicaCard pollingCurrentCard = null;

        /// <summary>
        /// ポーリングスレッドへの操作を行う際に使用するロックオブジェクトです。
        /// </summary>
        private object pollingThreadLockObject = new object();

        public FelicaReader(
            SmartCardResourceManager mgr,
            string readerName
            )
        {
            if (mgr == null) { throw new ArgumentNullException("mgr"); }
            if (readerName == null) { throw new ArgumentNullException("readerName"); }

            this.ResourceManager = mgr;
            this.ReaderName = readerName;
            this.IsRemoved = false; // 初期状態では取り外されていない
        }

        /// <summary>
        /// <para>ポーリングが開始されていない場合、ポーリングを開始します。</para>
        /// <para>ポーリングが開始されている場合は、何もしません。</para>
        /// </summary>
        /// <seealso cref="StopPolling"/>
        public void StartPolling()
        {
            // ポーリングが開始されていなければ、開始する
            lock (this.pollingThreadLockObject)
            {
                if (this.pollingThread == null)
                {
                    this.pollingThread = new Thread(new ThreadStart(this.Polling));
                    this.pollingThread.SetApartmentState(ApartmentState.STA);
                    this.pollingThread.Start();
                }
            }
        }

        /// <summary>
        /// ポーリングが開始されていれば、ポーリングを停止します。
        /// </summary>
        /// <seealso cref="StartPolling"/>
        public void StopPolling()
        {
            // ポーリングが開始されている場合
            lock (this.pollingThreadLockObject)
            {
                if (this.pollingThread != null)
                {
                    this.pollingThread.Abort();
                    this.pollingThread.Join();

                    Debug.Assert(!this.pollingThread.IsAlive); // 停止しているはず

                    this.pollingThread = null;
                }
            }
        }

        /// <summary>
        /// カードへ接続する。
        /// </summary>
        /// <returns>カードへ接続できた場合は<see cref="FelicaCard"/>オブジェクト、
        ///     失敗した場合は<value>null</value>を返します。</returns>
        /// <seealso cref="DisconnectCard"/>
        protected FelicaCard ConnectCard()
        {
            IntPtr card;
            int protocol;
            
            // カードへ接続する
            var result = PCSC.SCardConnect(
                this.ResourceManager.Context,
                this.ReaderName,
                PCSC.SCARD_SHARE_SHARED,
                PCSC.SCARD_PROTOCOL_T0 | PCSC.SCARD_PROTOCOL_T1,
                out card,
                out protocol
                );

            // 失敗した場合
            if (result != PCSC.SCARD_S_SUCCESS)
            {
                return null;
            }

            Debug.Assert(card != IntPtr.Zero); // カードが取得できている
            Debug.Assert(protocol != PCSC.SCARD_PROTOCOL_UNDEFINED); // プロトコルが取得できている

            return new FelicaCard(this, card, protocol);
        }

        /// <summary>
        /// カードから切断します。
        /// </summary>
        /// <param name="card">切断するカードを指定します。
        ///     <value>null</value>が指定された場合、何もしません。</param>
        /// <seealso cref="ConnectCard"/>
        protected void DisconnectCard(FelicaCard card)
        {
            if (card != null)
            {
                PCSC.SCardDisconnect(card.Card, PCSC.SCARD_LEAVE_CARD);
                card.Card = IntPtr.Zero;
            }
        }

        /// <summary>
        /// カードへのポーリングを行います。
        /// </summary>
        protected void Polling()
        {
            var state = new PCSC.SCARD_READERSTATE[1];

            // 初回のステータス取得を行う
            while (true)
            {
                // 初回のステータスが取得できた場合、ループを抜ける
                if (this.GetStatus(state))
                {
                    // 既にカードが載せられているか調べる
                    // 載せられている場合、イベントを発生させる
                    if ((state[0].dwEventState & PCSC.SCARD_STATE_EMPTY) == 0)
                    {
                        this.pollingCurrentCard = this.ConnectCard();
                        this.DispatchEventCardSet(this.pollingCurrentCard);
                    }

                    break;
                }
            }

            // 二回目以降の状態変化を取得
            while (true)
            {

                if (this.GetStatusChange(state, 100))
                {
                    // リーダーが取り外された場合
                    if ((state[0].dwEventState & PCSC.SCARD_STATE_UNAVAILABLE) != 0)
                    {
                        this.DisconnectCard(this.pollingCurrentCard);
                        this.pollingCurrentCard = null;

                        this.IsRemoved = true;
                        this.DispatchEventReaderRemoved();
                        break;
                    }

                    // カードが載せられた場合
                    else if ((state[0].dwEventState & PCSC.SCARD_STATE_PRESENT) != 0)
                    {
                        // カード情報が取得できていない場合
                        if (this.pollingCurrentCard == null)
                        {
                            this.pollingCurrentCard = this.ConnectCard();
                            this.DispatchEventCardSet(this.pollingCurrentCard);
                        }
                    }

                    // カードが取り外された場合
                    else if ((state[0].dwEventState & PCSC.SCARD_STATE_EMPTY) != 0)
                    {
                        this.DisconnectCard(this.pollingCurrentCard);
                        this.pollingCurrentCard = null;
                    }
                }
            }
        }

        /// <summary>
        /// カードが載せられた際のイベントを発生させます。
        /// </summary>
        /// <param name="card">イベントを発生させるカードを指定します。</param>
        protected void DispatchEventCardSet(FelicaCard card)
        {
            if (card != null && this.FelicaCardSet != null)
            {
                this.FelicaCardSet(this, new FelicaCardSetEventHandlerArgs
                {
                    ResourceManager = this.ResourceManager,
                    Reader = this,
                    Card = card
                });
            }
        }

        /// <summary>
        /// リーダーが取り外された時のイベントを発生させます。
        /// </summary>
        protected void DispatchEventReaderRemoved()
        {
            if (this.IsRemoved && this.FelicaReaderRemoved != null)
            {
                this.FelicaReaderRemoved(this, new FelicaReaderRemovedEventHandlerArgs
                    {
                        ResourceManager = this.ResourceManager,
                        Reader = this
                    });
            }
        }

        /// <summary>
        /// リーダーの状態を取得する。状態取得は、即時行われる。
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool GetStatus(PCSC.SCARD_READERSTATE[] state)
        {
            if (state == null) { throw new ArgumentNullException("state"); }
            if (state.Length != 1) { throw new ArgumentException("state"); }

            state[0].szReader = this.ReaderName;
            state[0].dwCurrentState = PCSC.SCARD_STATE_UNAWARE;

            // タイムアウトを 0 ミリ秒に設定して呼び出す
            // (= 即時、現在の状態を返す)
            int result = PCSC.SCardGetStatusChange(
                this.ResourceManager.Context, 0, state, state.Length);

            return result == PCSC.SCARD_S_SUCCESS;
        }

        /// <summary>
        /// リーダーの状態変化を取得します。状態に変化が起こるかタイムアウトした場合に制御が戻ります。
        /// </summary>
        /// <param name="state">前回の状態を指定します。</param>
        /// <param name="timeout">タイムアウトをミリ秒で指定します。</param>
        /// <returns>状態変化の取得に成功した場合<value>true</value>、
        ///     失敗した場合<value>false</value>が返ります。</returns>
        private bool GetStatusChange(PCSC.SCARD_READERSTATE[] state, int timeout)
        {
            if (state == null) { throw new ArgumentNullException("state"); }
            if (state.Length != 1) { throw new ArgumentException("state"); }

            // 前回取得したステータスを現在のステータスとする
            state[0].dwCurrentState = state[0].dwEventState;

            // 現在のステータスからの変更を検出する
            // 変更が発生するまで、もしくはタイムアウトになるまで、ブロックする
            int result = PCSC.SCardGetStatusChange(
                this.ResourceManager.Context, timeout, state, state.Length);

            return result == PCSC.SCARD_S_SUCCESS;
        }

        #region Dispose Finalize パターン

        private bool disposed = false;

        ~FelicaReader()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        /// <summary>
        /// リソースの開放を行うメソッドです。
        /// </summary>
        /// <param name="disposing">マネージドリソースの開放を行う場合に真を指定します。</param>
        /// <exception cref="FelicaException">
        /// スマートカードリソースマネージャーへのコンテキストの開放に失敗した場合に発生します。</exception>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) { return; }

            if (disposing)
            {
                // マネージドリソースの開放

                // ポーリングを停止、タスクを解放
                this.StopPolling();
                
                // カードが取得済みな場合は解放
                if (this.pollingCurrentCard != null)
                {
                    this.DisconnectCard(this.pollingCurrentCard);
                }
            }

            // アンマネージドリソースの開放
            
            // 解放済みとしてマーク
            this.disposed = true;
        }

        /// <summary>
        /// オブジェクトが破棄されていた場合に、例外を発生させるメソッドです。
        /// </summary>
        /// <exception cref="System.ObjectDisposedException"/>
        protected void ThrowExceptionIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(this.GetType().ToString());
            }
        }

        #endregion
   }
}
