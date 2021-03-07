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

        public bool HasNotStarted { get; set; } = true;
        public bool HasEnded { get; set; } = false;

        public bool ParrentPlayer { get; set; } = false;

        [TempData]
        public Card PlayerNowCard { get; set; }

        [TempData]
        public Card EnemyNowCard { get; set; }

        [TempData]
        public List<Card> PlayerDeck { get; set; }

        [TempData]
        public List<Card> EnemyDeck { get; set; }

        public string JudgedResult { get; set; }

        public string EnemyDeclaration { get; set; }

        public void OnGet()
        {

        }

        // ゲーム開始
        public IActionResult OnPostGameStart()
        {
            HasNotStarted = false;

            StartPlayer();
            DealCards();
            NextCardShow();

            return Page();
        }

        // 次のターンへ
        public IActionResult OnPostNextTurn()
        {

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

        }

        private void EnemyJudgment()
        {

        }
    }
}
