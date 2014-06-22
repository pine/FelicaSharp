using System;

namespace FelicaSharp
{
    /// <summary>
    /// FeliCa リーダーが取り外された時に発生するイベントの引数です。
    /// </summary>
    public class FelicaReaderRemovedEventHandlerArgs : EventArgs
    {
        /// <summary>
        /// スマートカードリソースマネージャを表します。
        /// </summary>
        public SmartCardResourceManager ResourceManager { get; internal set; }

        /// <summary>
        /// 取り外された FeliCa リーダーを表します。
        /// </summary>
        public FelicaReader Reader { get; internal set; }
    }
}
