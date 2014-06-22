using System;

namespace FelicaSharp
{
    public class EasyFelicaCardSetEventHandlerArgs : FelicaCardSetEventHandlerArgs
    {
        /// <summary>
        /// FeliCa の製造 ID (IDm) を表します。
        /// </summary>
        public string Idm { internal set; get; }

        /// <summary>
        /// FeliCa の製造パラメータ (PMm) を表します。
        /// </summary>
        public string Pmm { internal set; get; }

        /// <summary>
        /// 親クラスのデータを用いて初期化を行うコンストラクタです。
        /// </summary>
        /// <param name="e">親クラスのオブジェクト</param>
        internal EasyFelicaCardSetEventHandlerArgs(FelicaCardSetEventHandlerArgs e) : base(e) { }
    }
}
