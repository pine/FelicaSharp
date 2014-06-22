using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace FelicaSharp
{
    class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private EasyFelicaReader FelicaReader { get; set; }

        public MainWindowViewModel()
        {
            this.FelicaReader = new EasyFelicaReader();
            this.FelicaReader.FelicaCardSet += this.FelicaReader_FelicaCardSet;
            this.FelicaReader.FelicaReaderRemoved += this.FelicaReader_FelicaReaderRemoved;

            this.Logs = new ObservableCollection<string>();
            BindingOperations.EnableCollectionSynchronization(this.Logs, new object()); // マルチスレッド有効

            this.ConnectCommand = new DelegateCommand(this.Connect);
            this.DisconnectCommand = new DelegateCommand(this.Disconnect);
        }

        public bool IsConnected
        {
            get { return this.FelicaReader.IsConnected; }
        }

        private string connectButtonText = "接続";
        public string ConnectButtonText
        {
            get { return this.connectButtonText; }
            set
            {
                this.connectButtonText = value;
                this.OnPropertyChanged("ConnectButtonText");
            }
        }

        private ObservableCollection<string> logs = null;
        public ObservableCollection<string> Logs
        {
            get { return this.logs; }
            set
            {
                this.logs = value;
                this.OnPropertyChanged("Logs");
            }
        }

        public ICommand ConnectCommand { get; set; }
        private void Connect()
        {
            try
            {
                this.FelicaReader.Connect();
            }

            catch (FelicaException e)
            {
                this.AddLog(e.Message);
            }

            this.OnPropertyChanged("IsConnected");

            if (this.IsConnected)
            {
                this.AddLog("Connected");
            }
        }
        public ICommand DisconnectCommand { get; set; }
        private void Disconnect()
        {
            try
            {
                this.FelicaReader.Disconnect();
            }

            catch (FelicaException e)
            {
                this.AddLog(e.Message);
            }

            this.OnPropertyChanged("IsConnected");

            if (!this.IsConnected)
            {
                this.AddLog("Disconnected");
            }
        }

        private void AddLog(string line)
        {
            this.Logs.Insert(0, line);
        }

        private void FelicaReader_FelicaCardSet(object sender, EasyFelicaCardSetEventHandlerArgs e)
        {
            this.AddLog("CardSet: Idm = " + e.Idm + ", Pmm = " + e.Pmm);
        }

        private void FelicaReader_FelicaReaderRemoved(object sender, FelicaReaderRemovedEventHandlerArgs e)
        {
            this.AddLog("Reader Removed");
            this.OnPropertyChanged("IsConnected");
        }

        #region Dispose Finalize パターン
 
        /// <summary>
        /// 既にDisposeメソッドが呼び出されているかどうかを表します。
        /// </summary>
        private bool disposed = false;
 
        /// <summary>
        /// ConsoleApplication1.DisposableClass1 によって使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }
 
        /// <summary>
        /// ConsoleApplication1.DisposableClass1 クラスのインスタンスがGCに回収される時に呼び出されます。
        /// </summary>
        ~MainWindowViewModel()
        {
            this.Dispose(false);
        }
 
        /// <summary>
        /// ConsoleApplication1.DisposableClass1 によって使用されているアンマネージ リソースを解放し、オプションでマネージ リソースも解放します。
        /// </summary>
        /// <param name="disposing">マネージ リソースとアンマネージ リソースの両方を解放する場合は true。アンマネージ リソースだけを解放する場合は false。 </param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }
            this.disposed = true;
 
            if (disposing)
            {
                // マネージ リソースの解放処理をこの位置に記述します。
                this.FelicaReader.Dispose();
            }
            // アンマネージ リソースの解放処理をこの位置に記述します。
        }
 
        /// <summary>
        /// 既にDisposeメソッドが呼び出されている場合、例外をスローします。
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">既にDisposeメソッドが呼び出されています。</exception>
        protected void ThrowExceptionIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
 
        /// <summary>
        /// Dispose Finalize パターンに必要な初期化処理を行います。
        /// </summary>
        private void InitializeDisposeFinalizePattern()
        {
            this.disposed = false;
        }
 
        #endregion
    }
}
