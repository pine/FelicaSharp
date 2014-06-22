using System;
using System.Text;
using System.Diagnostics;

namespace FelicaSharp
{
    /// <summary>
    /// FeliCa に対して提供される基本的なサービスのクラスです。
    /// </summary>
    public class FelicaBasicService : FelicaService
    {
        /// <summary>
        /// FeliCa の IDm (識別子) のバイト数です。
        /// </summary>
        public const int FelicaIdmLength = 8;

        /// <summary>
        /// FeliCa の PMm (製造パラメータ) のバイト数です。
        /// </summary>
        public const int FelicaPmmLength = 8;

        public FelicaBasicService(FelicaCard card) : base(card) { }

        /// <summary>
        /// カードの識別子を取得します。
        /// </summary>
        public byte[] GetIdm()
        {
            return this.GetDataWithSizeValidate(0x00, FelicaIdmLength);
        }

        /// <summary>
        /// カードの IDm (識別子) を文字列で取得します。
        /// </summary>
        /// <returns>
        /// 取得結果が返ります。
        /// IDm は 8 バイトであり、各バイトを 16 進数表記しハイフンで繋いだ形式です。
        /// </returns>
        public string GetIdmAsString()
        {
            return BitConverter.ToString(this.GetIdm());
        }

        /// <summary>
        /// カードの PMm (製造パラメータ) を取得します。
        /// </summary>
        public byte[] GetPmm()
        {
            return this.GetDataWithSizeValidate(0x01, FelicaPmmLength);
        }

        /// <summary>
        /// カードの PMm (製造パラメータ) を文字列で取得します。
        /// </summary>
        /// <returns>
        /// 取得結果が返ります。
        /// PMm は 8 バイトであり、各バイトを 16 進数表記しハイフンで繋いだ形式です。
        /// </returns>
        public string GetPmmAsString()
        {
            return BitConverter.ToString(this.GetPmm());
        }

        /// <summary>
        /// カードタイプ名を取得します。
        /// </summary>
        /// <returns>取得結果が返ります。</returns>
        public byte[] GetCardTypeName()
        {
            return this.GetData(0xF4);
        }

        /// <summary>
        /// カードタイプ名を文字列で返します。
        /// </summary>
        /// <returns>取得結果が返ります。</returns>
        public string GetCardTypeNameAsString()
        {
            var cardTypeName = this.GetCardTypeName();

            if (cardTypeName != null && cardTypeName.Length > 0)
            {
                return Encoding.ASCII.GetString(cardTypeName, 0, cardTypeName.Length - 1);
            }

            return null;
        }
    }
}
