using System;

namespace FelicaSharp
{
    using PCSC = PersonalComputerSmartCard; // 長いので別名定義

    /// <summary>
    /// <para>スマートカードマネージャーを表すクラスです。</para>
    /// <para>Windows API を呼び出しアンマネージドリソースを扱うため、
    /// <see cref="System.IDisposable"/>を実装しています。</para>
    /// </summary>
    public class SmartCardResourceManager : IDisposable
    {
        /// <summary>
        /// <para>コンピューターに接続されている FeliCa リーダー一覧を配列で返します。</para>
        /// <seealso cref="readers"/>
        /// </summary>
        /// <exception cref="FelicaException">
        /// FeliCa リーダの一覧の取得に失敗した場合に発生します。</exception>
        public FelicaReader[] Readers
        {
            get
            {
                this.ThrowExceptionIfDisposed();
                return this.GetReaders();
            }
        }

        /// <summary>
        /// スマートカードリソースマネージャーへ接続済かどうかを返す
        /// </summary>
        public bool IsConnected
        {
            get
            {
                this.ThrowExceptionIfDisposed();

                // コンテキストが取得できているかどうかを調べる
                return this.Context != IntPtr.Zero;
            }
        }

        /// <summary>
        /// <para>スマートカードリソースマネージャーへのコンテキストです。</para>
        /// <para>まだ取得していない場合、取得に失敗した場合は
        /// <see cref="System.IntPtr.Zero"/>が返ります。</para>
        /// </summary>
        internal IntPtr Context { private set; get; }

        /// <summary>
        /// スマートカードマネージャーの初期化を行います。
        /// </summary>
        public SmartCardResourceManager()
        {
            this.Context = IntPtr.Zero;
        }

        /// <summary>
        /// スマートカードリソースマネージャーへ接続します。
        /// </summary>
        /// <exception cref="FelicaException">
        /// スマートカードリソースマネージャーへの接続に失敗した場合に発生します。</exception>
        /// <exception cref="System.ObjectDisposedException"/>
        public void Connect()
        {
            this.ThrowExceptionIfDisposed();
            
            this.ReleaseContext(); // 既に取得済みのコンテキストを開放
            this.EstablishContext(); // コンテキストを新規に取得
        }

        /// <summary>
        /// スマートカードリソースマネージャーから切断します。
        /// </summary>
        public void Disconnect()
        {
            this.ReleaseContext();
        }

        /// <summary>
        /// スマートカードマネージャーへ接続し、コンテキストを取得します。
        /// </summary>
        /// <exception cref="FelicaException">
        /// スマートカードリソースマネージャーへのコンテキストの取得に失敗した場合に発生します。</exception>
        /// <exception cref="System.ObjectDisposedException"/>
        protected void EstablishContext()
        {
            this.ThrowExceptionIfDisposed();
            
            // スマートカードマネージャーへのコンテキストが未取得な場合
            if (this.Context == IntPtr.Zero)
            {
                IntPtr phContext = IntPtr.Zero;

                // スマートカードマネージャーへのコンテキストを取得する
                var result = PCSC.SCardEstablishContext(
                    PCSC.SCARD_SCOPE_USER,
                    IntPtr.Zero, IntPtr.Zero, ref phContext
                    );

                // 失敗した場合は例外を投げる
                if (result != PCSC.SCARD_S_SUCCESS)
                {
                    throw new FelicaException("スマートカードマネージャーへの接続に失敗しました。");
                }

                // 取得したコンテキストを保存
                this.Context = phContext;
            }
        }

        /// <summary>
        /// <para>スマートカードマネージャーへのコンテキストを開放します。</para>
        /// <seealso cref="SCardReleaseContext"/>
        /// </summary>
        /// <exception cref="FelicaException">
        /// コンテキストが取得済み、かつ開放に失敗した場合に発生します。</exception>
        /// <exception cref="System.ObjectDisposedException"/>
        protected void ReleaseContext()
        {
            this.ThrowExceptionIfDisposed();
            
            // コンテキストが取得済みな場合
            if (this.Context != IntPtr.Zero)
            {
                var result = PCSC.SCardReleaseContext(this.Context);

                // 失敗した場合は例外を投げる
                if (result != PCSC.SCARD_S_SUCCESS)
                {
                    throw new FelicaException("コンテキストの開放に失敗しました。");
                }

                // コンテキストを解放済みとしてマークする
                this.Context = IntPtr.Zero;
            }
        }

        /// <summary>
        /// <para>コンピューターに接続されている FeliCa リーダー名一覧を配列で返します。</para>
        /// <seealso cref="FelicaReader"/>
        /// <seealso cref="GetReadersAsString"/>
        /// </summary>
        /// <exception cref="System.ObjectDisposedException"/>
        /// <exception cref="FelicaException">FeliCa リーダ一覧の取得に失敗した場合に発生します。</exception>
        protected FelicaReader[] GetReaders()
        {
            this.ThrowExceptionIfDisposed();

            // リーダーの一覧を文字列の配列で取得
            string[] readersAsString = this.GetReadersAsString();

            // リーダーの一覧の配列を確保
            var readers = new FelicaReader[readersAsString.Length];

            // 文字列からオブジェクトへ変換
            for (int i = 0; i < readersAsString.Length; ++i)
            {
                readers[i] = new FelicaReader(this, readersAsString[i]);
            }

            return readers;
        }

        /// <summary>
        /// <para>コンピューターに接続されている FeliCa リーダー名一覧を文字列の配列で返します。</para>
        /// <seealso cref="GetReaders"/>
        /// </summary>
        /// <returns>コンピューターに接続されている FeliCa リーダ名一覧の文字列の配列です。</returns>
        /// <exception cref="System.ObjectDisposedException"/>
        /// <exception cref="FelicaException">FeliCa リーダ一覧の取得に失敗した場合に発生します。</exception>
        protected string[] GetReadersAsString()
        {
            this.ThrowExceptionIfDisposed();

            int result;
            int bufsize = 0;


            // 一覧取得に必要とされるバッファサイズを取得
            result = PCSC.SCardListReaders(this.Context, null, null, ref bufsize);

            // 失敗した場合
            if (result != PCSC.SCARD_S_SUCCESS)
            {
                throw new FelicaException("FeliCa リーダー一覧の取得に失敗しました。");
            }

            // バッファを確保
            string buffer = new string((char)0, bufsize);

            // 一覧を取得する
            result = PCSC.SCardListReaders(this.Context, null, buffer, ref bufsize);

            // 失敗した場合
            if (result != PCSC.SCARD_S_SUCCESS)
            {
                throw new FelicaException("FeliCa リーダー一覧の取得に失敗しました。");
            }

            // 文字列をヌル文字で分解して、文字列の配列にする
            string[] readers = buffer.Split(new char[] { (char)0 },
                StringSplitOptions.RemoveEmptyEntries);

            return readers;
        }

        #region Dispose Finalize パターン

        private bool disposed = false;

        ~SmartCardResourceManager()
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
            }

            // アンマネージドリソースの開放
            this.Disconnect();

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
