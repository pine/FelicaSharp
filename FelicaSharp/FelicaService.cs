using System;
using System.Diagnostics;

namespace FelicaSharp
{
    /// <summary>
    /// <para>FeliCa カードを用いて提供されるサービスを表す親クラスです。</para>
    /// </summary>
    abstract public class FelicaService
    {
        /// <summary>
        /// FeliCa カードを表します。
        /// </summary>
        public FelicaCard Card { private set; get; }

        /// <summary>
        /// サービスの初期化を行います。
        /// 継承したクラスは、はじめに呼び出してください。
        /// </summary>
        public FelicaService(FelicaCard card)
        {
            if (card == null) { throw new ArgumentNullException("card"); }
            this.Card = card;
        }

        /// <summary>
        /// カードへコマンドを送信します。
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        protected byte[] SendCommand(byte[] command)
        {
            return this.Card.SendCommand(command);
        }

        /// <summary>
        /// カードに対して、Get Data コマンドを実行します。
        /// </summary>
        /// <param name="p1">取得するデータ種類を指定します。</param>
        /// <returns>取得したデータを返します。取得に失敗した場合は、例外を発生させます。</returns>
        protected byte[] GetData(byte p1)
        {
            return this.SendCommand(new byte[] { 0xFF, 0xCA, p1, 0x00, 0x00 });
        }

        /// <summary>
        /// <para>カードに対して、Get Data コマンドを実行します。</para>
        /// <para>予定したサイズと違う応答が帰ってきた場合、<value>null</value>を返します。</para>
        /// </summary>
        /// <param name="p1">取得するデータ種類を指定します。</param>
        /// <param name="receiveSize">予想されるレスポンスサイズを指定します。</param>
        /// <returns>取得したデータを返します。取得に失敗した場合は、例外を発生させます。</returns>
        protected byte[] GetDataWithSizeValidate(byte p1, int receiveSize)
        {
            var result = this.GetData(p1);
            return result.Length == receiveSize ? result : null;
        }
    }
}
