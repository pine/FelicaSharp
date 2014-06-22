using System;
using System.Diagnostics;

namespace FelicaSharp
{
    /// <summary>
    /// FeliCa リーダーを簡単に扱うための、ラッパクラスです。
    /// </summary>
    /// <seealso cref="SmartCardResourceManager"/>
    /// <seealso cref="FelicaReader"/>
    /// <seealso cref="FelicaCard"/>
    /// <seealso cref="FelicaBasicService"/>
    public class EasyFelicaReader : IDisposable
    {
        /// <summary>
        /// スマートカードリソースマネージャー
        /// </summary>
        public SmartCardResourceManager ResourceManager { private set; get; }

        /// <summary>
        /// 現在接続中の FeliCa リーダを表します。
        /// </summary>
        public FelicaReader Reader { private set; get; }

        /// <summary>
        /// カードが載せられた時のイベントを表します。
        /// </summary>
        public event EventHandler<EasyFelicaCardSetEventHandlerArgs> FelicaCardSet;

        /// <summary>
        /// カードリーダーが外された時のイベントを表します。
        /// </summary>
        public event EventHandler<FelicaReaderRemovedEventHandlerArgs> FelicaReaderRemoved;

        /// <summary>
        /// <para>FeliCa リーダーを初期化します。</para>
        /// <para>コンストラクタを呼び出した時点では、リーダーへは接続されません。</para>
        /// </summary>
        public EasyFelicaReader()
        {
            this.ResourceManager = new SmartCardResourceManager();
            this.Reader = null;
        }

        /// <summary>
        /// FeliCa リーダーへ接続します。
        /// </summary>
        public void Connect()
        {
            this.ThrowExceptionIfDisposed();

            // 前方一致で比較されるため、空文字は全リーダーを指定していることになる
            this.Connect("");
        }

        /// <summary>
        /// <para>FeliCa リーダーへ名前を指定して接続します。</para>
        /// <para>既に接続している場合は何もしません。</para>
        /// </summary>
        /// <param name="readerName">接続先の FeliCa リーダー名を指定します。
        ///     リーダー名は前方一致で比較されます。</param>
        /// <exception cref="FelicaException">
        ///     <para>スマートカードリソースマネージャーへの接続に失敗した時</para>
        ///     <para>FeliCa リーダーへの接続に失敗した時</para>
        /// </exception>
        public void Connect(string readerName)
        {
            this.ThrowExceptionIfDisposed();

            if (readerName == null) { throw new ArgumentNullException("readerName"); }

            // リーダーへまだ接続していない場合
            if (this.Reader == null)
            {
                // リソースマネージャへ接続していない場合は接続
                if (!this.ResourceManager.IsConnected)
                {
                    this.ResourceManager.Connect();
                }

                // スマートカードリソースマネージャーへ接続できているはず
                Debug.Assert(this.ResourceManager.IsConnected);

                // FeliCa リーダを取得
                this.Reader = this.GetReader(readerName);

                if (this.Reader == null)
                {
                    throw new FelicaException("接続する FeliCa リーダーが見つかりません。");
                }

                // イベントを設定
                this.Reader.FelicaCardSet += this.Reader_FelicaCardSet;
                this.Reader.FelicaReaderRemoved += this.Reader_FelicaReaderRemoved;

                // ポーリングを開始
                this.Reader.StartPolling();
            }
        }

        /// <summary>
        /// <para>現在接続している FeliCa リーダーから切断します。</para>
        /// <para>接続していない場合は、何もしません。</para>
        /// </summary>
        public void Disconnect()
        {
            this.ThrowExceptionIfDisposed();

            if (this.Reader != null)
            {
                // ポーリングを停止
                this.Reader.StopPolling();

                // 開放処理
                this.Reader.Dispose();
                this.Reader = null;
            }
        }

        /// <summary>
        /// リーダーへ接続されているかを返します。
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return this.ResourceManager != null && this.Reader != null;
            }
        }

        /// <summary>
        /// 名前を指定してリーダーを検索します。
        /// </summary>
        /// <param name="readerName">前方一致で検索する、リーダー名を指定します。</param>
        /// <returns>
        ///     <para>前方一致でマッチする最初のリーダーが返ります。</para>
        ///     <para>見つからなかった場合、<value>null</value>が返ります。</para>
        /// </returns>
        protected FelicaReader GetReader(string readerName)
        {
            // 引数・状態に関する例外処理
            this.ThrowExceptionIfDisposed();
            if (readerName == null) { throw new ArgumentNullException("readerName"); }
            if (!this.ResourceManager.IsConnected) { throw new InvalidOperationException("スマートカードリソースマネージャーへ接続されていません。"); }

            // リーダーを取得し、前方一致で検索する
            foreach (var reader in this.ResourceManager.Readers)
            {
                if (reader.ReaderName.StartsWith(readerName))
                {
                    return reader;
                }
            }

            return null;
        }

        /// <summary>
        /// カードが載せられた時に<see cref="FelicaReader"/>が発生させるイベントのリスナーです。
        /// </summary>
        private void Reader_FelicaCardSet(object sender, FelicaCardSetEventHandlerArgs e)
        {
            this.ThrowExceptionIfDisposed();

            // 自身にイベントが設定されている場合
            if (this.FelicaCardSet != null)
            {
                // Easy 版のイベントハンドラ引数を生成
                EasyFelicaCardSetEventHandlerArgs args = new EasyFelicaCardSetEventHandlerArgs(e);

                // FeliCa のベーシックサービスを生成
                var basic = new FelicaBasicService(e.Card);

                // カードタイプを取得
                var cardTypeName = basic.GetCardTypeNameAsString();

                // FeliCa である場合に処理
                if (cardTypeName == "FeliCa")
                {
                    // IDm と PMm を取得し、イベントハンドラ引数に代入
                    args.Idm = basic.GetIdmAsString();
                    args.Pmm = basic.GetPmmAsString();

                    // イベントを呼び出す
                    this.FelicaCardSet(this, args);
                }
            }
        }

        /// <summary>
        /// FeliCa リーダーが取り外された時に発生するイベントのリスナーです。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reader_FelicaReaderRemoved(object sender, FelicaReaderRemovedEventHandlerArgs e)
        {
            if (this.Reader != null)
            {
                this.ResourceManager.Disconnect();
                this.Reader = null;
            }

            if (this.FelicaReaderRemoved != null)
            {
                this.FelicaReaderRemoved(this, e);
            }
        }

        #region Dispose Finalize パターン

        private bool disposed = false;

        ~EasyFelicaReader()
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

                // リーダーから切断
                this.Disconnect();

                if (this.ResourceManager != null)
                {
                    this.ResourceManager.Dispose();
                }
            }

            // アンマネージドリソースの開放


            // 解放済みとしてマーク
            // ReleaseContext() の前で行うと、ObjectDisposedException になるので注意
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

