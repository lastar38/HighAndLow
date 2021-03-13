using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace HighAndLow.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        [TempData]
        public bool HasNotStarted { get; set; } = true;
        [TempData]
        public bool HasEnded { get; set; } = false;

        [TempData]
        public bool ShowResult { get; set; } = false;

        [TempData]
        public bool ParrentPlayer { get; set; } = false;

        [TempData]
        public Card PlayerNowCard { get; set; }

        [TempData]
        public Card EnemyNowCard { get; set; }

        [TempData]
        public List<Card> PlayerDeck { get; set; }

        [TempData]
        public List<Card> EnemyDeck { get; set; }

        [TempData]
        public int PlayerVictry { get; set; } = 0;

        [TempData]
        public int EnemyVictry { get; set; } = 0;

        [TempData]
        public string JudgedResult { get; set; }

        [BindProperty]
        public HighLow HighOrLow { get; set; }
        public enum HighLow
        {
            High,
            Low,
        }

        [TempData]
        public string EnemyDeclaration { get; set; }

        [TempData]
        public string ResultMessage { get; set; }

        public void OnGet()
        {
            HasNotStarted = true;
        }

        // ゲーム開始
        public IActionResult OnPostGameStart()
        {
            HasNotStarted = false;

            StartPlayer();
            DealCards();
            NextCardShow();

            if (ParrentPlayer)
            {
                EnemyJudgment();
            }

            return Page();
        }

        // 次のターンへ
        public IActionResult OnPostNextTurn()
        {
            if (!ShowResult)
            {
                ShowResult = !ShowResult;
                if (!ParrentPlayer)
                {
                    PlayerJudgment();
                }
            }
            else
            {
                ShowResult = !ShowResult;
                ParrentPlayer = !ParrentPlayer;
                NextCardShow();
                if (ParrentPlayer)
                {
                    EnemyJudgment();
                }                
            }
            return Page();
        }

        // 最初の親を決める
        private void StartPlayer()
        {
            if (RandomNumberGenerator.GetInt32(0, 2) == 1)
            {
                ParrentPlayer = true;
            }
        }

        // カードを配る
        private void DealCards()
        {
            var Deck = new Cards();
            PlayerDeck = new List<Card>();
            EnemyDeck = new List<Card>();

            var pop = ParrentPlayer;
            foreach (var card in Deck)
            {
                if (pop)
                {
                    PlayerDeck.Add(card);
                    pop = false;
                }
                else
                {
                    EnemyDeck.Add(card);
                    pop = true;
                }
            }

            if (PlayerDeck.Count() != EnemyDeck.Count())
            {
                //例外処理
            }
        }

        // 次のカードを配る
        private void NextCardShow()
        {
            if (PlayerDeck.Count() <= 0)
            {
                HasEnded = true;

                if (PlayerVictry > EnemyVictry)
                {
                    ResultMessage = "勝利";
                }
                else if (PlayerVictry < EnemyVictry)
                {
                    ResultMessage = "敗北";
                }
                else
                {
                    ResultMessage = "引き分け";
                }
            }
            else
            {
                PlayerNowCard = PlayerDeck.First();
                PlayerDeck.RemoveAt(0);
                EnemyNowCard = EnemyDeck.First();
                EnemyDeck.RemoveAt(0);
            }
        }

        private void JudgmentShow()
        {

        }

        private void PlayerJudgment()
        {
            if (HighOrLow == HighLow.High)
            {
                if (PlayerNowCard.Number > EnemyNowCard.Number)
                {
                    JudgedResult = "当たり";
                    PlayerVictry += 2;
                }
                else if (PlayerNowCard.Number < EnemyNowCard.Number)
                {
                    JudgedResult = "はずれ";
                    EnemyVictry += 2;
                }
                else
                {
                    JudgedResult = "引き分け";
                    PlayerVictry++;
                    EnemyVictry++;
                }
            }
            else
            {
                if (PlayerNowCard.Number < EnemyNowCard.Number)
                {
                    JudgedResult = "当たり";
                    PlayerVictry += 2;
                }
                else if (PlayerNowCard.Number > EnemyNowCard.Number)
                {
                    JudgedResult = "はずれ";
                    EnemyVictry += 2;
                }
                else
                {
                    JudgedResult = "引き分け";
                    PlayerVictry++;
                    EnemyVictry++;
                }
            }
        }

        private void EnemyJudgment()
        {
            if (EnemyAI())
            {
                // 相手がハイを選択
                EnemyDeclaration = "ハイ";

                if (PlayerNowCard.Number < EnemyNowCard.Number)
                {
                    JudgedResult = "当たり";
                    EnemyVictry += 2;
                }
                else if (PlayerNowCard.Number > EnemyNowCard.Number)
                {
                    JudgedResult = "はずれ";
                    PlayerVictry += 2;
                }
                else
                {
                    JudgedResult = "引き分け";
                    PlayerVictry++;
                    EnemyVictry++;
                }
            }
            else
            {
                // 相手がローを選択
                EnemyDeclaration = "ロー";

                if (PlayerNowCard.Number > EnemyNowCard.Number)
                {
                    JudgedResult = "当たり";
                    EnemyVictry += 2;
                }
                else if (PlayerNowCard.Number < EnemyNowCard.Number)
                {
                    JudgedResult = "はずれ";
                    PlayerVictry += 2;
                }
                else
                {
                    JudgedResult = "引き分け";
                    PlayerVictry++;
                    EnemyVictry++;
                }
            }
        }

        public bool EnemyAI()
        {
            if(PlayerNowCard.Number >= 7)
            {
                return false;
            }
            else { return true; }
        }
    }
}
