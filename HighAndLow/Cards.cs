using Microsoft.AspNetCore.Html;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HighAndLow
{
    /// <summary>
    /// ランダムに並んだ52枚のトランプカードを表すクラスです。
    /// 新しくゲームを始めるにはコンストラクターで初期化する必要があります。（newキーワードを使用）
    /// </summary>
    public class Cards : IEnumerable<Card>
    {
        private static readonly List<Card> cardList;
        private readonly Stack<Card> shuffledCards;

        static Cards()
        {
            cardList = new List<Card>();
            AddCardAllKind();
        }

        public Cards()
        {
            shuffledCards = new Stack<Card>(cardList.OrderBy(x => Guid.NewGuid()));
        }

        private static void AddCardAllKind()
        {
            AddCardByMark(CardMark.Spade);
            AddCardByMark(CardMark.Heart);
            AddCardByMark(CardMark.Diamond);
            AddCardByMark(CardMark.Club);
        }

        private static void AddCardByMark(CardMark mark)
        {
            for (int i = 1; i <= 13; i++)
            {
                cardList.Add(new Card(mark, i));
            }
        }

        /// <summary>
        /// ランダムに並んだカードから先頭の1枚を取得します。
        /// </summary>
        /// <returns>1枚のカードを表すCardクラス</returns>
        public Card Pop()
        {
            return shuffledCards.Pop();
        }

        /// <summary>
        /// ランダムに並んだカードを先頭から順に取得します。
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Card> GetEnumerator()
        {
            while (shuffledCards.Count > 0)
            {
                yield return shuffledCards.Pop();
            }
        }

        /// <summary>
        /// ランダムに並んだカードを先頭から順に取得します。
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// 1枚のトランプカードを表すクラスです。
    /// このクラスおよびこのクラスを要素とするListは<c>TempData</c>に保存が可能です。
    /// </summary>
    public class Card : IJsonSerializable<Card>
    {
        private CardMark? mark;
        private int number = 0;
        private CardImage cardImage;

        /// <summary>
        /// トランプのマーク（スペード、ハート、ダイア、クラブ）を取得します。
        /// </summary>
        public CardMark Mark
        {
            get => mark.Value;
        }

        /// <summary>
        /// トランプの数字を取得します。
        /// </summary>
        public int Number
        {
            get => number;
        }

        /// <summary>
        /// トランプの表示用イメージを表す<seealso cref="HighAndLow.CardImage"/>クラスを取得します。
        /// </summary>
        public CardImage CardImage
        {
            get
            {
                if (cardImage == null)
                {
                    SetCardImage();
                }
                return cardImage;
            }
        }

        public Card(CardMark mark, int number)
        {
            this.mark = mark;
            this.number = number;
        }

        public Card()
        {
        }

        private void SetCardImage()
        {
            if (mark.HasValue && number != 0)
            {
                cardImage = new CardImage(Mark, Number);
            }
        }
    }

    public enum CardMark
    {
        Spade,
        Heart,
        Diamond,
        Club
    }

    /// <summary>
    /// トランプカードの表示用イメージを表すクラスです。
    /// <seealso cref="FrontImageToHtml"/>または<seealso cref="BackImageToHtml"/>でイメージ表示のためのHTML要素が出力されます。
    /// </summary>
    public class CardImage
    {
        private const int UNICODE_HEX_BACK = 0x1F0A0;
        private const string COLOR_BLACK = "black";
        private const string COLOR_RED = "red";

        private string frontImage;
        private string backImage;
        private string color;

        public CardImage(CardMark mark, int number)
        {
            const int UNICODE_HEX_BASE_SPADE = 0x1F0A1;
            const int UNICODE_HEX_BASE_HEART = 0x1F0B1;
            const int UNICODE_HEX_BASE_DIAMOND = 0x1F0C1;
            const int UNICODE_HEX_BASE_CLUB = 0x1F0D1;

            int differenceFromUnicodeHex;
            if (number < 12)
            {
                differenceFromUnicodeHex = number - 1;
            }
            else
            {
                differenceFromUnicodeHex = number;
            }

            if (mark == CardMark.Spade)
            {
                frontImage = GetStringFromHex(UNICODE_HEX_BASE_SPADE + differenceFromUnicodeHex);
                color = COLOR_BLACK;
            }
            else if (mark == CardMark.Heart)
            {
                frontImage = GetStringFromHex(UNICODE_HEX_BASE_HEART + differenceFromUnicodeHex);
                color = COLOR_RED;
            }
            else if (mark == CardMark.Diamond)
            {
                frontImage = GetStringFromHex(UNICODE_HEX_BASE_DIAMOND + differenceFromUnicodeHex);
                color = COLOR_RED;
            }
            else
            {
                frontImage = GetStringFromHex(UNICODE_HEX_BASE_CLUB + differenceFromUnicodeHex);
                color = COLOR_BLACK;
            }

            backImage = GetStringFromHex(UNICODE_HEX_BACK);
        }

        /// <summary>
        /// トランプカードの裏面イメージのHTML要素を出力します。
        /// </summary>
        /// <returns>HTML要素を表す文字列</returns>
        public HtmlString BackImageToHtml()
        {
            return BackImageToHtml(ImageSize.MIDDLE);
        }

        /// <inheritdoc cref="BackImageToHtml"/>
        /// <param name="size">イメージのサイズ指定。省略時はMIDDLEとなります。</param>
        public HtmlString BackImageToHtml(ImageSize size)
        {
            return CreateHtmlString(size, true);
        }

        /// <summary>
        /// トランプカードの表面イメージのHTML要素を出力します。
        /// </summary>
        /// <returns>HTML要素を表す文字列</returns>
        public HtmlString FrontImageToHtml()
        {
            return FrontImageToHtml(ImageSize.MIDDLE);
        }

        /// <inheritdoc cref="FrontImageToHtml"/>
        /// <param name="size">イメージのサイズ指定。省略時はMIDDLEとなります。</param>
        public HtmlString FrontImageToHtml(ImageSize size)
        {
            return CreateHtmlString(size);
        }

        private string GetStringFromHex(int hex)
        {
            return char.ConvertFromUtf32(hex);
        }

        private HtmlString CreateHtmlString(ImageSize size, bool isBackImage = false)
        {
            const string TAG_KIND = "span";

            string color;
            string imageString;

            if (isBackImage)
            {
                color = COLOR_BLACK;
                imageString = backImage;
            }
            else
            {
                color = this.color;
                imageString = frontImage;
            }

            return new HtmlString($"<{TAG_KIND} style=\"{GetSizeStyle(size)} {GetColorStyle(color)} {GetLineHeight()}\">{imageString}</{TAG_KIND}>");
        }

        private string GetSizeStyle(ImageSize size)
        {
            const string STYLE = "font-size";

            string sizeStyle = size switch
            {
                ImageSize.SMALL => "5rem",
                ImageSize.MIDDLE => "15rem",
                ImageSize.LARGE => "25rem",
                _ => "15rem",
            };
            return $"{STYLE}: {sizeStyle};";
        }

        private string GetColorStyle(String color)
        {
            const string STYLE = "color";

            return $"{STYLE}: {color};";
        }

        private string GetLineHeight()
        {
            const string STYLE = "line-height";

            return $"{STYLE}: 1em;";

        }

        public enum ImageSize
        {
            SMALL,
            MIDDLE,
            LARGE
        }
    }

}
