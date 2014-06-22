using System;

namespace FelicaSharp
{
    /// <summary>
    /// カードが載せられた時のイベントハンドラの引数クラスです。
    /// </summary>
    public class FelicaCardSetEventHandlerArgs : EventArgs
    {
        internal FelicaCardSetEventHandlerArgs() { }
        internal FelicaCardSetEventHandlerArgs(FelicaCardSetEventHandlerArgs e)
        {
            this.ResourceManager = e.ResourceManager;
            this.Reader = e.Reader;
            this.Card = e.Card;
        }

        public SmartCardResourceManager ResourceManager { internal set; get; }
        public FelicaReader Reader { internal set; get; }
        public FelicaCard Card { internal set; get; }
    }
}
